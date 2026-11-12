"""Prospective check of the capacity-minimum model: one bounded proposal per robust winner branch, two fractions each."""
from __future__ import annotations
from functools import lru_cache
import itertools
import json
import math
import runpy
import subprocess
import sys
import r3_h_protocol as P
import r3_h_minimax_lp as LP

ROOT=P.ROOT
R=runpy.run_path(str(ROOT/'eng/review-r3-repaired-capacity-minimum-model9.py'))
C,F,D,S,T=R['C'],R['F'],R['D'],R['S'],R['C']['T']
ID='r3-repaired-minimum-proposals10';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT,BINARY=C['PROJECT'],C['BINARY']
CAPACITIES,CHECKPOINTS=R['CAPACITIES'],R['CHECKPOINTS']
POLICY=dict(recipes=14,repeats=2,maximum_runtime_steps=44,branches=3,step_fractions=[.25,.5],
            model='capacity-minimum model 9, rebuilt from the frozen observation-8 records and required identical to its frozen analysis',
            branch_rule='all 125 winner patterns over three checkpoints are tried; a pattern is kept when its linear program is feasible '
                        'with every coefficient inside the sampled amplitude and the winner ahead by the branch margin at the full step',
            objective='max-absolute-weighted-81-residual inside the branch; secondary minimum normalized L1 step',
            branch_margin_kg_s=1e-3,executed_margin_kg_s=1e-4,secondary_objective_allowance=1e-8,numerical_cleanup_tolerance=1e-12,
            relative_prediction_tolerance=.1,winner_agreement_required=True,
            predictions_frozen_before_first_physical_call=True,rule_explored_offline_before_contract=True,
            baseline='identical to the frozen observation-8 baseline record',
            energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
            one_sided_directions_active=0,searches=0,production_changes=0,root_criterion_unchanged=True,
            previous_results_rewritten=False,r3_passed=False,m10_closed=False)


def affine(model,base,labels,amplitudes,pattern,row_checkpoint):
    """Inside one winner pattern every capacity and row is affine in the normalized step z (coefficient = z*amplitude)."""
    flows=[];capacities=[]
    for k,winner in enumerate(pattern):
        level=[]
        for c in range(len(CAPACITIES)):
            constant=base['cap'][k][c]-base['flow'][k]+math.fsum(model['eta'][k][c][m]*flows[m][0] for m in range(k))
            slope=[model['g'][label][k][c]*h+math.fsum(model['eta'][k][c][m]*flows[m][1][j] for m in range(k))
                   for j,(label,h) in enumerate(zip(labels,amplitudes))]
            level.append((constant,slope))
        capacities.append(level);flows.append(level[CAPACITIES.index(winner)])
    rows=[]
    for i,k in enumerate(row_checkpoint):
        constant=base['y'][i]+math.fsum(model['H'][i][m]*flows[m][0] for m in range(k+1))
        slope=[model['G'][label][i]*h+math.fsum(model['H'][i][m]*flows[m][1][j] for m in range(k+1))
               for j,(label,h) in enumerate(zip(labels,amplitudes))]
        rows.append((constant,slope))
    return capacities,rows


def solve_branch(model,base,labels,amplitudes,pattern,row_checkpoint):
    """Minimax over z in [-1,1]^n with the pattern's winner ahead by the branch margin; None when infeasible."""
    capacities,rows=affine(model,base,labels,amplitudes,pattern,row_checkpoint);n=len(labels)
    scale=max(1.,max(abs(c) for c,_ in rows));a=[];b=[]
    for constant,slope in rows:  # x = z + 1 >= 0
        shifted=(constant-math.fsum(slope))/scale;s=[v/scale for v in slope]
        a.append(s+[-1.]);b.append(-shifted);a.append([-v for v in s]+[-1.]);b.append(shifted)
    for k,winner in enumerate(pattern):
        w=CAPACITIES.index(winner);wc,ws=capacities[k][w]
        for c in range(len(CAPACITIES)):
            if c!=w:
                oc,os=capacities[k][c];d=[x-y for x,y in zip(ws,os)]
                a.append(d+[0.]);b.append(oc-wc-POLICY['branch_margin_kg_s']+math.fsum(d))
    for j in range(n):a.append([float(i==j) for i in range(n)]+[0.]);b.append(2.)
    try:primary=LP.linear_program(a,b,[0.]*n+[-1.])
    except ValueError:return None
    if primary['status']!='OPTIMAL-CERTIFIED':return None
    second=[row+[0.]*n for row in a];rhs=b[:]
    second.append([0.]*n+[1.]+[0.]*n);rhs.append(primary['primal'][n]+POLICY['secondary_objective_allowance'])
    for j in range(n):
        row=[0.]*(2*n+1);row[j]=1.;row[n+1+j]=-1.;second.append(row);rhs.append(1.)
        row=[0.]*(2*n+1);row[j]=-1.;row[n+1+j]=-1.;second.append(row);rhs.append(-1.)
    secondary=LP.linear_program(second,rhs,[0.]*(n+1)+[-1.]*n)
    P.require(secondary['status']=='OPTIMAL-CERTIFIED','Secondary LP not certified')
    normalized=[];cleanup=[]
    for j,value in enumerate(secondary['primal'][:n]):
        raw=value-1.;used=min(1.,max(-1.,raw))
        P.require(abs(used-raw)<=POLICY['numerical_cleanup_tolerance'],'Coefficient escaped box')
        if used!=raw:cleanup.append(dict(index=j,raw=raw,used=used))
        normalized.append(used)
    keys=('status','primal_violation','dual_violation','duality_gap','pivots')
    return normalized,dict(primary={k:primary[k] for k in keys},secondary={k:secondary[k] for k in keys},
                           numerical_cleanup=cleanup,linear_optimum=primary['primal'][n]*scale)


@lru_cache(maxsize=1)
def context():
    observation,records,_=R['inputs']();_,scales,names=S['layout']();basis,_=F['basis']()
    row_checkpoint=[CHECKPOINTS.index(n[0]) for n in names]+[i//2 for i in range(6)];obs=observation['observations']
    points={k:dict(y=S['weighted_vector'](r,scales,names),flow=[x['commanded_flow_kg_s'] for x in obs[k]],
                   cap=[[x['capacities_kg_s'][c] for c in CAPACITIES] for x in obs[k]]) for k,r in records.items()}
    directions=[(d['direction'],d['amplitude'],d['direction']+f"/plus/{d['amplitude']:g}",d['direction']+f"/minus/{d['amplitude']:g}")
                for d in observation['active_directions']]
    model=R['fit'](points,directions,81,row_checkpoint)
    parent=json.loads(S['frozen'](R['ID'])['01-analysis.json'])
    P.require(parent['status']=='PASS-REPAIRED-CAPACITY-MINIMUM-MODEL-EVIDENCE' and parent['structural_checks']['passed'],'Model 9 not accepted')
    P.require(model['H']==parent['model']['flow_response_by_row'] and model['eta']==parent['model']['capacity_flow_response']
              and model['G']==parent['model']['direction_response_by_row'] and model['g']==parent['model']['capacity_direction_response'],
              'Rebuilt model differs from the frozen model 9')
    labels=[d[0] for d in directions];descriptions=[basis['working_directions'][l] for l in labels]
    P.require(sum(d['coefficient_sign']=='nonnegative' for d in descriptions)==POLICY['one_sided_directions_active'],'One-sided direction became active')
    P.require(all(d['sampled_amplitude']==h for d,(_,h,_,_) in zip(descriptions,directions)),'Amplitude drift')
    return dict(model=model,base=points['base'],base_record=records['base'],labels=labels,amplitudes=[d[1] for d in directions],
                descriptions=descriptions,row_checkpoint=row_checkpoint,scales=scales,names=names)


@lru_cache(maxsize=1)
def proposal_plan():
    x=context();model,base,labels,h,rc=x['model'],x['base'],x['labels'],x['amplitudes'],x['row_checkpoint'];branches=[]
    for pattern in itertools.product(CAPACITIES,repeat=len(CHECKPOINTS)):
        solved=solve_branch(model,base,labels,h,pattern,rc)
        if solved is None:continue
        normalized,certificate=solved;coefficients=[z*a for z,a in zip(normalized,h)];executions=[]
        for fraction in POLICY['step_fractions']:
            step={l:fraction*c for l,c in zip(labels,coefficients)};p=R['predict'](model,base,step,rc)
            P.require(p['winners']==list(pattern) and min(p['margins'])>=POLICY['executed_margin_kg_s'],'Executed fraction leaves its declared branch')
            fluid=[math.fsum(s*d['coordinates'][i] for s,d in zip(step.values(),x['descriptions'])) for i in range(24)]
            operating=[math.fsum(s*d['operating'][i] for s,d in zip(step.values(),x['descriptions'])) for i in range(6)]
            P.require(all(abs(v)<=.02 for v in fluid+operating),'Aggregated coordinate outside inherited box')
            P.require(S['norm'](p['change'])>0,'Null predicted proposal')
            single=[S['norm'](R['predict'](model,base,{l:s},rc)['change']) for l,s in step.items()]
            executions.append(dict(step_fraction=fraction,coordinates=fluid,operating=operating,predicted_change_weighted_81=p['change'],
                                   predicted_capacities_kg_s=p['capacities'],predicted_winners=p['winners'],
                                   predicted_margin_to_next_kg_s=p['margins'],predicted_flow_change_kg_s=p['flow_change'],
                                   predicted_maximum_weighted_81=max(abs(a+b) for a,b in zip(base['y'],p['change'])),
                                   cancellation_ratio=S['norm'](p['change'])/math.fsum(single)))
        branches.append(dict(pattern=list(pattern),normalized_coefficients=normalized,coefficients=coefficients,
                             certificate=certificate,executions=executions))
    P.require(len(branches)==POLICY['branches'],'Branch count differs from the contracted budget')
    return dict(labels=labels,amplitudes=h,baseline_weighted_81=base['y'],baseline_maximum_weighted_81=max(map(abs,base['y'])),
                base_winners=[CAPACITIES[min(range(len(CAPACITIES)),key=lambda c:base['cap'][k][c])] for k in range(len(CHECKPOINTS))],
                branches=branches,full_steps_not_executed=True)


def label_of(branch,execution):
    return f"proposal/{'-'.join(branch['pattern'])}/{execution['step_fraction']:g}"


def requests():
    points=[dict(Id='base',Coordinates=[0.]*24,OperatingPointFractions=[0.]*6)]
    points+=[dict(Id=label_of(b,e),Coordinates=e['coordinates'],OperatingPointFractions=e['operating']) for b in proposal_plan()['branches'] for e in b['executions']]
    return [{**point,'Id':point['Id']+f'/repeat/{repeat}'} for point in points for repeat in (1,2)]


def fixtures():
    # Synthetic kink: rows depend on the inputs and on f=min(c1,c2). Each branch optimum must be exact when replayed on the truth.
    g1,g2,h0,y0=[2.,-1.],[.5,3.],[4.,-2.],[.3,-.2];cap=lambda v:[1.+v[0]-v[1],1.+2*v[1]]
    def point(v):
        f=min(cap(v));return dict(y=[y0[i]+g1[i]*v[0]+g2[i]*v[1]+h0[i]*(f-1.) for i in range(2)],flow=[f],cap=[cap(v)+[9.,9.,9.]])
    h=1e-3;pts=dict(base=point([0.,0.]),**{f'{n}/{t}':point([s*h if n=='a' else 0.,s*h if n=='b' else 0.]) for n in 'ab' for t,s in (('p',1),('m',-1))})
    model=R['fit'](pts,[('a',h,'a/p','a/m'),('b',h,'b/p','b/m')],2,[0,0],levels=1);found=[]
    for pattern in (('stop',),('control',),('admission',)):
        solved=solve_branch(model,pts['base'],['a','b'],[.1,.1],pattern,[0,0])
        if solved is None:continue
        z,certificate=solved;truth=point([.1*z[0],.1*z[1]]);caps=cap([.1*z[0],.1*z[1]])
        P.require(abs(max(map(abs,truth['y']))-certificate['linear_optimum'])<1e-7,'Branch optimum fixture')
        P.require(CAPACITIES[min(range(2),key=lambda c:caps[c])]==pattern[0] and abs(caps[0]-caps[1])>=POLICY['branch_margin_kg_s']-1e-9,'Branch margin fixture')
        found.append(pattern[0])
    P.require(found==['stop','control'],'Branch enumeration fixture')
    P.require(POLICY['recipes']==2*(1+POLICY['branches']*len(POLICY['step_fractions'])) and POLICY['maximum_runtime_steps']==2+3*POLICY['recipes'],'Budget fixture')
    return 'PASS-MINIMUM-PROPOSAL-FIXTURES'


def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for entry in c['implementation_files']+c['frozen_inputs']:
        P.require(P.digest((ROOT/entry['path']).read_bytes())==entry['sha256'],'Input drift: '+entry['path'])


def preexecution():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing acquisition retained')
    P.require((OUT/'00-build.log').exists() and BINARY.exists(),'Build the capacity observation console first')
    P.write(OUT,'01-model.json',proposal_plan());P.write(OUT,'01-requests.json',requests())
    P.write(OUT,'02-preexecution.json',dict(status='PASS-PREEXECUTION',contract_sha256=P.digest(CONTRACT.read_bytes()),
            requests_sha256=P.digest((OUT/'01-requests.json').read_bytes()),model_sha256=P.digest((OUT/'01-model.json').read_bytes()),
            fixtures=fixtures(),runtime_steps=0,assemblies=[dict(file=p.name,sha256=P.digest(p.read_bytes())) for p in sorted(BINARY.parent.glob('*.dll'))]))
    subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'--validate-only'],cwd=ROOT,check=True)
    return 'PASS-PREEXECUTION'


def analyze(records):
    expected=requests();P.require(len(records)==len(expected)==POLICY['recipes'],'Recipe budget drift')
    x=context();plan,scales,names=S['layout']();points={};energy_errors=[];torque_errors=[]
    for i in range(0,len(records),2):
        pair=records[i:i+2]
        for r,request in zip(pair,expected[i:i+2]):
            P.require(all(r[k]==request[k] for k in request),'Request/response drift')
            P.H3['H2']['verify_physics'](r,plan);energy_errors+=S['verify_extra'](r);torque_errors+=T['audit_mechanical'](r)
        P.require({k:v for k,v in pair[0].items() if k!='Id'}=={k:v for k,v in pair[1].items() if k!='Id'},'Repeatability drift')
        points[pair[0]['Id'].rsplit('/repeat/',1)[0]]=pair[0]
    base=points['base']
    P.require({k:v for k,v in base.items() if k!='Id'}=={k:v for k,v in x['base_record'].items() if k!='Id'},'Parent baseline drift')
    baseline=S['weighted_vector'](base,scales,names);base_flow=[r['commanded_flow_kg_s'] for r in C['reconstruct'](base)];model=proposal_plan();proposals=[]
    for branch in model['branches']:
        for e in branch['executions']:
            record=points[label_of(branch,e)];guards=D['reasons'](record,base);row=dict(record=label_of(branch,e),pattern=branch['pattern'],step_fraction=e['step_fraction'],guards=guards)
            if record['Residuals'] is None:
                row.update(prediction_validated=False,relative_prediction_error=None);proposals.append(row);continue
            observed=[a-b for a,b in zip(S['weighted_vector'](record,scales,names),baseline)];capacity=C['reconstruct'](record)
            winners=[r['winner'] for r in capacity];flow=[r['commanded_flow_kg_s']-b for r,b in zip(capacity,base_flow)]
            error=S['relative'](observed,e['predicted_change_weighted_81'])
            row.update(relative_prediction_error=error,predicted_winners=e['predicted_winners'],observed_winners=winners,winners_agree=winners==e['predicted_winners'],
                       predicted_margin_to_next_kg_s=e['predicted_margin_to_next_kg_s'],observed_margin_to_next_kg_s=[r['margin_to_next_kg_s'] for r in capacity],
                       predicted_flow_change_kg_s=e['predicted_flow_change_kg_s'],observed_flow_change_kg_s=flow,
                       maximum_flow_error_kg_s=max(abs(a-b) for a,b in zip(flow,e['predicted_flow_change_kg_s'])),
                       maximum_capacity_error_kg_s=max(abs(r['capacities_kg_s'][c]-p[c]) for r,p in zip(capacity,e['predicted_capacities_kg_s']) for c in CAPACITIES),
                       cancellation_ratio=e['cancellation_ratio'],
                       predicted_maximum_weighted_81=e['predicted_maximum_weighted_81'],
                       observed_maximum_weighted_81=max(map(abs,S['weighted_vector'](record,scales,names))),
                       observed_maximum_original_scaled_residual=max(map(abs,record['Residuals'])),
                       prediction_validated=guards['available'] and not guards['phase_changes'] and winners==e['predicted_winners']
                           and error<=POLICY['relative_prediction_tolerance'],
                       observed_change_weighted_81=observed)
            proposals.append(row)
    validated=[p['prediction_validated'] for p in proposals]
    return dict(status='PASS-REPAIRED-MINIMUM-PROPOSAL-EVIDENCE',fixtures=fixtures(),recipes=POLICY['recipes'],
                maximum_runtime_steps=POLICY['maximum_runtime_steps'],exact_repeats=True,baseline_matches_parent=True,
                baseline_maximum_weighted_81=max(map(abs,baseline)),baseline_maximum_original_scaled_residual=max(map(abs,base['Residuals'])),
                proposals=proposals,predictions_validated=sum(validated),predictions=len(validated),
                all_predictions_validated=all(validated),prospective=True,
                maximum_energy_ledger_error_w=max(map(abs,energy_errors)),maximum_torque_ledger_error_nm=max(map(abs,torque_errors)),
                model=model,search_started=False,family_selected=False,root_observed=False,r3_passed=False,m10_closed=False,
                limits=['Only the eight active trial-6 directions are used; the other 22 have no observed capacities',
                        'A branch is tested only where the model keeps its winner ahead by the declared margin; near-tie and mixed patterns are not tested',
                        'Full model steps are not executed: only their 0.25 and 0.5 fractions',
                        'A validated prediction is not a root, a search or a qualification of R3; no seed is installed'])


def adjudicate():
    authenticate();P.require(P.load(OUT/'01-model.json')==proposal_plan(),'Preexecution model drift')
    result=analyze([json.loads(s) for s in (OUT/'03-measurements.jsonl').read_text(encoding='utf-8').splitlines()])
    P.write(OUT,'04-analysis.json',result);return result['status']


def execute():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing acquisition retained')
    P.require(P.load(OUT/'01-model.json')==proposal_plan(),'Preexecution model drift')
    with (OUT/'execution.log').open('x',encoding='utf-8') as stream:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID],cwd=ROOT,stdout=stream,stderr=subprocess.STDOUT,check=True)
    return adjudicate()


def freeze():
    adjudicate();P.freeze(ID,['00-build.log','01-model.json','01-requests.json','02-preexecution.json','03-measurements.jsonl','04-analysis.json','execution.log'])
    return 'PASS-FROZEN-MINIMUM-PROPOSALS'


def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);P.require(json.loads(data['01-model.json'])==json.loads(json.dumps(proposal_plan())),'Frozen model drift')
    result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()])
    P.require(json.loads(json.dumps(result))==json.loads(data['04-analysis.json']),'Replay drift')
    return result['status']+' (offline; zero runtime steps)'


if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','plan','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    if sys.argv[1]=='plan':
        for b in proposal_plan()['branches']:
            print(b['pattern'],b['certificate']['linear_optimum'],[round(z,4) for z in b['normalized_coefficients']])
            for e in b['executions']:print('  ',e['step_fraction'],e['predicted_maximum_weighted_81'],min(e['predicted_margin_to_next_kg_s']),e['cancellation_ratio'])
    else:print(globals()[sys.argv[1]]())
