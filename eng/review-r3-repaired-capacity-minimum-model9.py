"""Offline model of the stage-flow capacity minimum, compared with trial 6 and the frozen active-direction probes."""
from __future__ import annotations
from functools import lru_cache
import json
import math
import runpy
import sys
import r3_h_protocol as P

ROOT=P.ROOT
C=runpy.run_path(str(ROOT/'eng/run-r3-repaired-capacity-observation8.py'))
M,F,D,S=C['M'],C['F'],C['D'],C['S']
ID='r3-repaired-capacity-minimum-model9';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
CAPACITIES=C['CAPACITIES'];CHECKPOINTS=C['CHECKPOINTS']
POLICY=dict(runtime_steps=0,parents='the 38 frozen records of capacity observation 8 (first repeat; repeats verified identical there)',
            model='per checkpoint, each capacity linear in the active directions and in the commanded stage flows of earlier checkpoints; '
                  'stage flow = minimum of the linearized capacities, evaluated checkpoint by checkpoint; '
                  'each of the 81 rows linear in the active directions and in the commanded stage flows up to its own checkpoint',
            fit='flow coefficients by least squares on the plus+minus probe sums of the active directions; '
                'direction coefficients by the antisymmetric probe difference after removing the flow terms',
            proposals_used_in_fit=False,active_coefficient_threshold=1e-13,
            minimum_relative_singular_value=1e-3,relative_prediction_tolerance=.1,winner_agreement_required=True,
            structural_torque_relative_tolerance=1e-3,structural_inventory_tolerance=1e-2,leave_one_direction_out=True,
            one_sided_directions_active=0,phases_and_regimes_unchanged=True,
            retrospective=True,model_form_explored_before_contract=True,
            proposals=0,searches=0,production_changes=0,root_criterion_unchanged=True,previous_results_rewritten=False,
            r3_passed=False,m10_closed=False)


def subtract(a,b):return [x-y for x,y in zip(a,b)]


def lstsq(rows,rhs):
    """Householder least squares; returns solution and residual vector (pure Python, deterministic)."""
    n=len(rows[0]) if rows else 0
    if n==0:return [],rhs[:]
    a=[r[:] for r in rows];b=rhs[:];m=len(a)
    P.require(m>=n,'Underdetermined least squares')
    for j in range(n):
        norm=math.sqrt(math.fsum(a[i][j]**2 for i in range(j,m)))
        P.require(norm>0,'Rank-deficient least squares')
        alpha=-norm if a[j][j]>=0 else norm
        v=[0.]*m;v[j]=a[j][j]-alpha
        for i in range(j+1,m):v[i]=a[i][j]
        vv=math.fsum(x*x for x in v[j:])
        for k in range(j,n):
            t=2*math.fsum(v[i]*a[i][k] for i in range(j,m))/vv
            for i in range(j,m):a[i][k]-=t*v[i]
        t=2*math.fsum(v[i]*b[i] for i in range(j,m))/vv
        for i in range(j,m):b[i]-=t*v[i]
    x=[0.]*n
    for i in reversed(range(n)):x[i]=(b[i]-math.fsum(a[i][k]*x[k] for k in range(i+1,n)))/a[i][i]
    return x,[r-math.fsum(c*y for c,y in zip(row,x)) for r,row in zip(rhs,rows)]


def singular_values(rows,n):
    """Singular values of the first n columns by cyclic Jacobi on the Gram matrix."""
    g=[[math.fsum(r[i]*r[j] for r in rows) for j in range(n)] for i in range(n)]
    for _ in range(200):
        off=[(abs(g[i][j]),i,j) for i in range(n) for j in range(i+1,n)]
        if not off or max(off)[0]<=1e-300:break
        _,i,j=max(off);theta=.5*math.atan2(2*g[i][j],g[j][j]-g[i][i]);c,s=math.cos(theta),math.sin(theta)
        for k in range(n):g[k][i],g[k][j]=c*g[k][i]-s*g[k][j],s*g[k][i]+c*g[k][j]
        for k in range(n):g[i][k],g[j][k]=c*g[i][k]-s*g[j][k],s*g[i][k]+c*g[j][k]
    return sorted(math.sqrt(max(0.,g[i][i])) for i in range(n))


def fit(points,directions,rows,row_checkpoint,levels=3):
    """points: label -> dict(y=rows, flow=[3], cap=[3][5]); directions: list of (label,h,plus,minus)."""
    base=points['base'];dF={k:subtract(v['flow'],base['flow']) for k,v in points.items()}
    sums=[[dF[p][m]+dF[q][m] for m in range(levels)] for _,_,p,q in directions]
    H=[];row_fit=[]
    for i in range(rows):
        k=row_checkpoint[i]
        x,res=lstsq([s[:k+1] for s in sums],[points[p]['y'][i]+points[q]['y'][i]-2*base['y'][i] for _,_,p,q in directions])
        H.append(x);row_fit.append(max(map(abs,res)))
    eta=[];capacity_fit=[]
    for k in range(levels):
        level=[]
        for c in range(len(CAPACITIES)):
            x,res=lstsq([s[:k] for s in sums],[points[p]['cap'][k][c]+points[q]['cap'][k][c]-2*base['cap'][k][c] for _,_,p,q in directions])
            level.append(x);capacity_fit.append(max(map(abs,res)))
        eta.append(level)
    G={};g={}
    for label,h,p,q in directions:
        G[label]=[((points[p]['y'][i]-points[q]['y'][i])-math.fsum(H[i][m]*(dF[p][m]-dF[q][m]) for m in range(row_checkpoint[i]+1)))/(2*h) for i in range(rows)]
        g[label]=[[((points[p]['cap'][k][c]-points[q]['cap'][k][c])-math.fsum(eta[k][c][m]*(dF[p][m]-dF[q][m]) for m in range(k)))/(2*h)
                   for c in range(len(CAPACITIES))] for k in range(levels)]
    return dict(H=H,eta=eta,G=G,g=g,sums=sums,maximum_row_fit_residual=max(row_fit),maximum_capacity_fit_residual=max(capacity_fit))


def predict(model,base,step,row_checkpoint):
    """step: label -> coordinate multiplier of that direction's unit vector."""
    flows=[];winners=[];margins=[];capacities=[]
    for k in range(len(model['eta'])):
        caps=[base['cap'][k][c]+math.fsum(s*model['g'][label][k][c] for label,s in step.items())
              +math.fsum(model['eta'][k][c][m]*(flows[m]-base['flow'][m]) for m in range(k)) for c in range(len(CAPACITIES))]
        order=sorted(range(len(CAPACITIES)),key=lambda c:caps[c])
        flows.append(caps[order[0]]);winners.append(CAPACITIES[order[0]]);margins.append(caps[order[1]]-caps[order[0]])
        capacities.append(dict(zip(CAPACITIES,caps)))
    dF=subtract(flows,base['flow'])
    dy=[math.fsum(s*model['G'][label][i] for label,s in step.items())+math.fsum(model['H'][i][m]*dF[m] for m in range(row_checkpoint[i]+1))
        for i in range(len(row_checkpoint))]
    return dict(change=dy,flow_change=dF,winners=winners,margins=margins,capacities=capacities)


def fixtures():
    # Synthetic minimum: rows depend on the inputs and on f=min(c1,c2); the decomposition recovers the joint step.
    G1,G2,H0=[2.,-1.],[.5,3.],[4.,-2.]
    cap=lambda x:[1.+x[0]-x[1],1.+2*x[1]]
    def point(x):
        f=min(cap(x));return dict(y=[G1[i]*x[0]+G2[i]*x[1]+H0[i]*(f-1.) for i in range(2)],flow=[f],cap=[cap(x)+[9.,9.,9.]])
    h=1e-3;pts=dict(base=point([0.,0.]),**{f'{n}/{t}':point([s*h if n=='a' else 0.,s*h if n=='b' else 0.]) for n in 'ab' for t,s in (('p',1),('m',-1))})
    dirs=[('a',h,'a/p','a/m'),('b',h,'b/p','b/m')]
    model=fit(pts,dirs,2,[0,0],levels=1)
    step={'a':.3,'b':.2};truth=point([.3,.2])
    pred=predict(model,pts['base'],step,[0,0])
    P.require(max(abs(a-b) for a,b in zip(pred['change'],truth['y']))<1e-9,'Minimum decomposition fixture')
    affine=[(pts['a/p']['y'][i]-pts['a/m']['y'][i])/(2*h)*.3+(pts['b/p']['y'][i]-pts['b/m']['y'][i])/(2*h)*.2 for i in range(2)]
    P.require(max(abs(a-b) for a,b in zip(affine,truth['y']))>1e-2,'Affine secant fixture should fail across the kink')
    x,res=lstsq([[1.,0.],[0.,1.],[1.,1.]],[1.,2.,3.]);P.require(max(abs(a-b) for a,b in zip(x,[1.,2.]))<1e-14 and max(map(abs,res))<1e-14,'Least-squares fixture')
    P.require(max(abs(a-b) for a,b in zip(singular_values([[3.,0.],[0.,4.]],2),[3.,4.]))<1e-12,'Singular-value fixture')
    return 'PASS-CAPACITY-MINIMUM-MODEL-FIXTURES'


def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for entry in c['implementation_files']+c['frozen_inputs']:
        P.require(P.digest((ROOT/entry['path']).read_bytes())==entry['sha256'],'Input drift: '+entry['path'])


@lru_cache(maxsize=1)
def inputs():
    data=S['frozen'](C['ID']);observation=json.loads(data['04-analysis.json'])
    P.require(observation['status']=='PASS-REPAIRED-CAPACITY-OBSERVATION-EVIDENCE','Capacity observation not accepted')
    records={}
    for line in data['03-measurements.jsonl'].splitlines():
        r=json.loads(line);records.setdefault(r['Id'].rsplit('/repeat/',1)[0],r)
    trial=json.loads(S['frozen'](F['ID'])['04-analysis.json'])
    return observation,records,trial


def analyze():
    observation,records,trial=inputs();_,scales,names=S['layout']();basis,_=F['basis']()
    row_checkpoint=[CHECKPOINTS.index(n[0]) for n in names]+[i//2 for i in range(6)]
    obs=observation['observations'];base_record=records['base']
    P.require(len(S['thermal_vector'](base_record))==6,'Thermal row layout drift')
    points={k:dict(y=S['weighted_vector'](r,scales,names),flow=[x['commanded_flow_kg_s'] for x in obs[k]],
                   cap=[[x['capacities_kg_s'][c] for c in CAPACITIES] for x in obs[k]]) for k,r in records.items()}
    guards={k:D['reasons'](r,base_record) for k,r in records.items() if k!='base'}
    P.require(all(g['available'] and not g['phase_changes'] for g in guards.values()),'Phase or regime change inside the model scope')
    model_t6=trial['model'];coefficients=dict(zip(model_t6['labels'],model_t6['coefficients']))
    active=[d for d in observation['active_directions']]
    P.require([d['direction'] for d in active]==[l for l in model_t6['labels'] if abs(coefficients[l])>POLICY['active_coefficient_threshold']],'Active direction drift')
    omitted=max(abs(coefficients[l]) for l in model_t6['labels'] if abs(coefficients[l])<=POLICY['active_coefficient_threshold'])
    signs={d['direction']:basis['working_directions'][d['direction']]['coefficient_sign'] for d in active}
    P.require(sum(s=='nonnegative' for s in signs.values())==POLICY['one_sided_directions_active'],'One-sided direction became active')
    directions=[(d['direction'],d['amplitude'],d['direction']+f"/plus/{d['amplitude']:g}",d['direction']+f"/minus/{d['amplitude']:g}") for d in active]
    model=fit(points,directions,81,row_checkpoint)
    identifiability={str(n):singular_values(model['sums'],n) for n in (1,2,3)}
    relative_singular={n:v[0]/v[-1] for n,v in identifiability.items()}
    P.require(min(relative_singular.values())>=POLICY['minimum_relative_singular_value'],'Flow terms not identifiable from the probe sums')
    base=points['base']

    # Structural diagnostics: same-checkpoint torque per commanded flow, and inventory capacity versus earlier flows.
    torque_rows=[i for i,n in enumerate(names) if n[1:]==('rotor','torque')]
    structural=[]
    for k,i in enumerate(torque_rows):
        m=base_record['Observations'][k]['Extra']['Mechanical'];q=obs['base'][k]['inlet_quality']
        expected=q*m['StageIdealWorkJKg']*m['StageEfficiency']/max(m['InitialOmega'],m['RatedOmega'])
        structural.append(dict(checkpoint=CHECKPOINTS[k],fitted_nm_per_kg_s=model['H'][i][k],mechanical_nm_per_kg_s=expected,
                               relative_difference=(model['H'][i][k]-expected)/expected))
    inventory=[dict(checkpoint=CHECKPOINTS[k],earlier=CHECKPOINTS[m],fitted=model['eta'][k][CAPACITIES.index('inventory')][m],expected=-.5)
               for k in range(3) for m in range(k)]
    structural_ok=all(abs(s['relative_difference'])<=POLICY['structural_torque_relative_tolerance'] for s in structural) \
        and all(abs(x['fitted']-x['expected'])<=POLICY['structural_inventory_tolerance'] for x in inventory)

    def compare(label,step):
        p=predict(model,base,step,row_checkpoint);observed=subtract(points[label]['y'],base['y'])
        winners=[x['winner'] for x in obs[label]]
        return dict(record=label,predicted_winners=p['winners'],observed_winners=winners,winners_agree=p['winners']==winners,
                    predicted_flow_change_kg_s=p['flow_change'],observed_flow_change_kg_s=subtract(points[label]['flow'],base['flow']),
                    maximum_flow_error_kg_s=max(abs(a-b) for a,b in zip(p['flow_change'],subtract(points[label]['flow'],base['flow']))),
                    predicted_margin_to_next_kg_s=p['margins'],
                    relative_error=S['relative'](observed,p['change']) if S['norm'](p['change'])>0 else None,
                    predicted_change_weighted_81=p['change'],observed_change_weighted_81=observed)

    probes=[compare(p,{label:sign*h}) for label,h,plus,minus in directions for p,sign in ((plus,1.),(minus,-1.))]
    proposals=[];previous={p['step_fraction']:p for p in trial['proposals']}
    for fraction in F['POLICY']['step_fractions']:
        step={d[0]:fraction*coefficients[d[0]] for d in directions}
        row=compare(f'proposal/{fraction:g}',step)
        contributions=[S['norm'](predict(model,base,{label:s},row_checkpoint)['change']) for label,s in step.items()]
        row.update(step_fraction=fraction,guards=guards[f'proposal/{fraction:g}'],
                   prediction_validated=guards[f'proposal/{fraction:g}']['available'] and row['winners_agree']
                       and row['relative_error']<=POLICY['relative_prediction_tolerance'],
                   previous_affine_relative_error=previous[fraction]['relative_prediction_error'],
                   previous_affine_prediction_validated=previous[fraction]['prediction_validated'],
                   cancellation_ratio=S['norm'](row['predicted_change_weighted_81'])/math.fsum(contributions))
        proposals.append(row)

    loo=[]
    for excluded in directions:
        others=[d for d in directions if d is not excluded];sub=fit(points,others,81,row_checkpoint)
        label,h,plus,minus=excluded;dF={k:subtract(points[k]['flow'],base['flow']) for k in (plus,minus)}
        sub['G'][label]=[((points[plus]['y'][i]-points[minus]['y'][i])-math.fsum(sub['H'][i][m]*(dF[plus][m]-dF[minus][m]) for m in range(row_checkpoint[i]+1)))/(2*h) for i in range(81)]
        sub['g'][label]=[[((points[plus]['cap'][k][c]-points[minus]['cap'][k][c])-math.fsum(sub['eta'][k][c][m]*(dF[plus][m]-dF[minus][m]) for m in range(k)))/(2*h)
                          for c in range(len(CAPACITIES))] for k in range(3)]
        for fraction in F['POLICY']['step_fractions']:
            p=predict(sub,base,{d[0]:fraction*coefficients[d[0]] for d in directions},row_checkpoint)
            loo.append(dict(excluded_direction=label,step_fraction=fraction,winners=p['winners'],
                            relative_error=S['relative'](subtract(points[f'proposal/{fraction:g}']['y'],base['y']),p['change'])))

    validated=all(p['prediction_validated'] for p in proposals)
    return dict(status='PASS-REPAIRED-CAPACITY-MINIMUM-MODEL-EVIDENCE',fixtures=fixtures(),runtime_steps=0,
                scope=dict(points=len(points),active_directions=[d[0] for d in directions],omitted_maximum_coefficient=omitted,
                           one_sided_active=0,phases_and_regimes_unchanged=True,base_winners=[x['winner'] for x in obs['base']],
                           base_margin_to_next_kg_s=[x['margin_to_next_kg_s'] for x in obs['base']]),
                identifiability=dict(singular_values=identifiability,relative_minimum=relative_singular,
                                     probe_flow_sums_kg_s={d[0]:s for d,s in zip(directions,model['sums'])}),
                model=dict(flow_response_by_row=model['H'],capacity_flow_response=model['eta'],
                           direction_response_by_row=model['G'],capacity_direction_response=model['g'],
                           maximum_row_fit_residual=model['maximum_row_fit_residual'],
                           maximum_capacity_fit_residual_kg_s=model['maximum_capacity_fit_residual']),
                structural_checks=dict(torque=structural,inventory=inventory,passed=structural_ok),
                probes=probes,proposals=proposals,leave_one_direction_out=loo,
                summary=dict(probe_winner_agreement=sum(p['winners_agree'] for p in probes),probes=len(probes),
                             maximum_probe_relative_error=max(p['relative_error'] for p in probes),
                             maximum_probe_flow_error_kg_s=max(p['maximum_flow_error_kg_s'] for p in probes),
                             proposal_relative_errors=[p['relative_error'] for p in proposals],
                             previous_affine_relative_errors=[p['previous_affine_relative_error'] for p in proposals],
                             maximum_proposal_flow_error_kg_s=max(p['maximum_flow_error_kg_s'] for p in proposals),
                             maximum_leave_one_out_relative_error=max(x['relative_error'] for x in loo),
                             leave_one_out_winners_admission=all(x['winners']==['admission']*3 for x in loo)),
                proposal_predictions_validated=validated,retrospective_only=True,prospective_validation=False,
                proposal_executed=False,search_started=False,family_selected=False,root_observed=False,r3_passed=False,m10_closed=False,
                limits=['Retrospective: the model form was explored on these frozen records before the contract; no parameter uses the proposal records',
                        'Only the eight active trial-6 directions are modelled; the other 22 directions have no observed capacities',
                        'Flow responses near-collinear across checkpoints: individual cross coefficients are not physical claims',
                        'Second-order smooth terms are neglected; valid only inside the same phases, regimes and amplitudes',
                        'A validated prediction is not a root, a search or a qualification of R3'])


ORIGINAL=['00-original-review.py','00-original-spec.md','00-original-contract.json','00-original-failure.log',
          '00-original-analysis.json','00-original-manifest.json','00-original-returned.json']


def verify_original(files,result):
    """The first freeze failed only its replay: integer dictionary keys became strings in JSON. Its bytes stay conserved."""
    correction=P.load(CONTRACT)['review_correction'];original=json.loads(files['00-original-contract.json'])
    P.require(P.digest(files['00-original-contract.json'])==correction['previous_contract_sha256'],'Original contract identity drift')
    P.require(original['policy']==POLICY,'Correction changed the policy')
    P.require({e['path']:e['sha256'] for e in original['implementation_files']}['eng/review-'+ID+'.py']==P.digest(files['00-original-review.py']),'Original script identity drift')
    P.require(json.loads(files['00-original-returned.json'])['manifest_sha256']==P.digest(files['00-original-manifest.json']),'Original receipt drift')
    stored={e['file']:e['sha256'] for e in json.loads(files['00-original-manifest.json'])['files']}
    P.require(stored=={'01-analysis.json':P.digest(files['00-original-analysis.json'])},'Original analysis identity drift')
    P.require(b'Replay drift' in files['00-original-failure.log'],'Original failure not conserved')
    P.require(json.loads(files['00-original-analysis.json'])==json.loads(json.dumps(result)),'Correction changed the analysis content')


def execute():
    authenticate();P.require(not (OUT/'01-analysis.json').exists(),'Existing review retained')
    result=analyze();verify_original({name:(OUT/name).read_bytes() for name in ORIGINAL},result)
    P.write(OUT,'01-analysis.json',result);P.freeze(ID,ORIGINAL+['01-analysis.json']);return result['status']


def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);result=analyze();verify_original(data,result)
    P.require(result==json.loads(data['01-analysis.json']),'Replay drift')
    return result['status']+' (offline; zero runtime steps)'


if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','execute','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
