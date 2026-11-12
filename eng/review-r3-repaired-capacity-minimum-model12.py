"""Offline re-estimation of the capacity-minimum model on all 30 working directions, compared with frozen probes and proposals."""
from __future__ import annotations
from functools import lru_cache
import json
import math
import runpy
import sys
import r3_h_protocol as P

ROOT=P.ROOT
X=runpy.run_path(str(ROOT/'eng/run-r3-repaired-minimum-proposals10.py'))
O=runpy.run_path(str(ROOT/'eng/run-r3-repaired-capacity-observation11.py'))
R,C,F,D,S=X['R'],X['C'],X['F'],X['D'],X['S']
ID='r3-repaired-capacity-minimum-model12';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
CAPACITIES,CHECKPOINTS=R['CAPACITIES'],R['CHECKPOINTS']
POLICY=dict(runtime_steps=0,directions=30,flow_directions=13,direct_directions=17,one_sided_directions=4,
            parents='frozen records of capacity observations 8 and 11 (first repeat; repeats verified identical there)',
            model='capacity-minimum model 9, same form: per checkpoint each capacity linear in the directions and in the commanded stage flows '
                  'of earlier checkpoints; stage flow = minimum; each of the 81 rows linear in the directions and in the flows up to its checkpoint',
            fit='flow coefficients by least squares on the plus+minus probe sums of the 13 directions whose probes change the commanded flow; '
                'their direction coefficients by the antisymmetric difference after removing the flow terms; '
                'the 13 two-sided directions that leave the flow unchanged by the plain central difference; '
                'the four one-sided directions by the forward difference at the sampled amplitude',
            proposals_used_in_fit=False,one_sided_doubled_probe_used_in_fit=False,
            minimum_relative_singular_value=1e-3,relative_prediction_tolerance=.1,winner_agreement_required=True,
            structural_torque_relative_tolerance=1e-3,structural_inventory_tolerance=1e-2,
            retrospective=True,model_form_explored_before_contract=True,
            proposals=0,searches=0,production_changes=0,root_criterion_unchanged=True,previous_results_rewritten=False,
            r3_passed=False,m10_closed=False)


def subtract(a,b):return [x-y for x,y in zip(a,b)]


def first_records(protocol):
    data=S['frozen'](protocol);records={}
    for line in data['03-measurements.jsonl'].splitlines():
        r=json.loads(line);records.setdefault(r['Id'].rsplit('/repeat/',1)[0],r)
    return json.loads(data['04-analysis.json']),records


def extend(model,points,base,row_checkpoint,direct):
    """Add directions without flow terms: central difference, or forward difference when only the positive probe is given."""
    for label,h,plus,minus in direct:
        low=points[minus] if minus else base;width=2*h if minus else h
        model['G'][label]=[(points[plus]['y'][i]-low['y'][i])/width for i in range(len(row_checkpoint))]
        model['g'][label]=[[(points[plus]['cap'][k][c]-low['cap'][k][c])/width for c in range(len(CAPACITIES))] for k in range(len(model['eta']))]
    return model


@lru_cache(maxsize=1)
def build():
    """Model on the 30 directions plus everything a later protocol needs to use it."""
    observation8,records8,trial6=R['inputs']();observation11,records11=first_records(O['ID'])
    P.require(observation11['status']=='PASS-REPAIRED-CAPACITY-OBSERVATION-11-EVIDENCE' and not observation11['reconstruction_unavailable'],'Observation 11 not accepted')
    _,scales,names=S['layout']();basis,_=F['basis']()
    row_checkpoint=[CHECKPOINTS.index(n[0]) for n in names]+[i//2 for i in range(6)]
    point=lambda r,o:dict(y=S['weighted_vector'](r,scales,names),flow=[x['commanded_flow_kg_s'] for x in o],
                          cap=[[x['capacities_kg_s'][c] for c in CAPACITIES] for x in o])
    points={k:point(r,observation8['observations'][k]) for k,r in records8.items()}
    P.require({k:v for k,v in records11['base'].items() if k!='Id'}=={k:v for k,v in records8['base'].items() if k!='Id'},'Baseline drift between observations')
    points.update({k:point(r,observation11['observations'][k]) for k,r in records11.items() if k!='base'})
    records={**records11,**records8};base_record=records8['base']
    guards={k:D['reasons'](r,base_record) for k,r in records.items() if k!='base'}
    P.require(all(g['available'] and not g['phase_changes'] for g in guards.values()),'Phase or regime change inside the model scope')
    flow=[(d['direction'],d['amplitude'],d['direction']+f"/plus/{d['amplitude']:g}",d['direction']+f"/minus/{d['amplitude']:g}") for d in observation8['active_directions']]
    moving=set(observation11['summary']['directions_changing_flow']);direct=[];doubled={}
    for d in observation11['directions']:
        if d['direction'] in moving:flow.append((d['direction'],d['amplitude'],d['records'][0],d['records'][1]))
        elif d['one_sided']:direct.append((d['direction'],d['amplitude'],d['records'][0],None));doubled[d['direction']]=d['records'][1]
        else:direct.append((d['direction'],d['amplitude'],d['records'][0],d['records'][1]))
    base=points['base']
    for label,h,plus,minus in direct:
        P.require(all(points[k]['flow']==base['flow'] for k in (plus,minus) if k),'Direct direction changes the commanded flow')
    P.require(len(flow)==POLICY['flow_directions'] and len(direct)==POLICY['direct_directions'] and len(doubled)==POLICY['one_sided_directions'],'Direction partition drift')
    model=extend(R['fit'](points,flow,81,row_checkpoint),points,base,row_checkpoint,direct)
    labels=sorted(model['G']);descriptions=[basis['working_directions'][l] for l in labels]
    P.require(labels==sorted(basis['working_directions']) and len(labels)==POLICY['directions'],'Working direction drift')
    amplitudes={d[0]:d[1] for d in flow+direct}
    P.require(all(basis['working_directions'][l]['sampled_amplitude']==amplitudes[l] for l in labels),'Amplitude drift')
    P.require(sorted(doubled)==sorted(l for l,d in zip(labels,descriptions) if d['coefficient_sign']=='nonnegative'),'One-sided direction drift')
    return dict(model=model,points=points,base=base,base_record=base_record,row_checkpoint=row_checkpoint,scales=scales,names=names,
                labels=labels,amplitudes=[amplitudes[l] for l in labels],descriptions=descriptions,
                lower=[0. if l in doubled else -1. for l in labels],flow=flow,direct=direct,doubled=doubled,
                observation8=observation8,trial6=trial6)


def fixtures():
    # A direction that never moves the minimum needs no flow term; a one-sided one is a forward difference.
    cap=lambda v:[1.+v[0],1.-v[0]+0.*v[1]]
    def point(v):
        f=min(cap(v));return dict(y=[2.*v[0]+3.*v[1]+5.*v[2]+4.*(f-1.)],flow=[f],cap=[cap(v)+[9.,9.,9.]])
    h=1e-3;unit=lambda j,s:[s*h if i==j else 0. for i in range(3)]
    pts=dict(base=point([0.,0.,0.]),ap=point(unit(0,1)),am=point(unit(0,-1)),bp=point(unit(1,1)),bm=point(unit(1,-1)),cp=point(unit(2,1)))
    model=extend(R['fit'](pts,[('a',h,'ap','am')],1,[0],levels=1),pts,pts['base'],[0],[('b',h,'bp','bm'),('c',h,'cp',None)])
    step=dict(a=.2,b=-.1,c=.3);truth=point([.2,-.1,.3])
    P.require(abs(R['predict'](model,pts['base'],step,[0])['change'][0]-truth['y'][0])<1e-9,'Extended decomposition fixture')
    P.require(abs(model['G']['b'][0]-3.)<1e-9 and abs(model['G']['c'][0]-5.)<1e-9,'Direct response fixture')
    return R['fixtures']()+'; PASS-CAPACITY-MINIMUM-MODEL-12-FIXTURES'


def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for entry in c['implementation_files']+c['frozen_inputs']:
        P.require(P.digest((ROOT/entry['path']).read_bytes())==entry['sha256'],'Input drift: '+entry['path'])


def analyze():
    x=build();model,points,base,rc=x['model'],x['points'],x['base'],x['row_checkpoint']
    identifiability={str(n):R['singular_values'](model['sums'],n) for n in (1,2,3)}
    relative_singular={n:v[0]/v[-1] for n,v in identifiability.items()}
    P.require(min(relative_singular.values())>=POLICY['minimum_relative_singular_value'],'Flow terms not identifiable from the probe sums')
    obs=x['observation8']['observations'];names=x['names']
    torque_rows=[i for i,n in enumerate(names) if n[1:]==('rotor','torque')];structural=[]
    for k,i in enumerate(torque_rows):
        m=x['base_record']['Observations'][k]['Extra']['Mechanical'];q=obs['base'][k]['inlet_quality']
        expected=q*m['StageIdealWorkJKg']*m['StageEfficiency']/max(m['InitialOmega'],m['RatedOmega'])
        structural.append(dict(checkpoint=CHECKPOINTS[k],fitted_nm_per_kg_s=model['H'][i][k],mechanical_nm_per_kg_s=expected,
                               relative_difference=(model['H'][i][k]-expected)/expected))
    inventory=[dict(checkpoint=CHECKPOINTS[k],earlier=CHECKPOINTS[m],fitted=model['eta'][k][CAPACITIES.index('inventory')][m],expected=-.5)
               for k in range(3) for m in range(k)]
    structural_ok=all(abs(s['relative_difference'])<=POLICY['structural_torque_relative_tolerance'] for s in structural) \
        and all(abs(i['fitted']-i['expected'])<=POLICY['structural_inventory_tolerance'] for i in inventory)

    def compare(record,step,observed,winners,flow):
        p=R['predict'](model,base,step,rc);error=S['relative'](observed,p['change']) if S['norm'](p['change'])>0 else None
        return dict(record=record,predicted_winners=p['winners'],observed_winners=winners,winners_agree=p['winners']==winners,
                    maximum_flow_error_kg_s=max(abs(a-b) for a,b in zip(p['flow_change'],flow)),predicted_margin_to_next_kg_s=p['margins'],
                    relative_error=error,passed=p['winners']==winners and error is not None and error<=POLICY['relative_prediction_tolerance'])

    def of_point(record,step):
        q=points[record];winners=[CAPACITIES[min(range(len(CAPACITIES)),key=lambda c:q['cap'][k][c])] for k in range(len(CHECKPOINTS))]
        return compare(record,step,subtract(q['y'],base['y']),winners,subtract(q['flow'],base['flow']))

    # In-sample: the probes that estimate the flow terms. They test the winners and the kink, not the smooth slopes.
    flow_probes=[of_point(p,{label:sign*h}) for label,h,plus,minus in x['flow'] for p,sign in ((plus,1.),(minus,-1.))]
    # Central directions without flow terms: the even part of the two probes measures what a linear slope cannot represent.
    symmetry=[]
    for label,h,plus,minus in x['direct']:
        if minus:
            even=[a+b-2*c for a,b,c in zip(points[plus]['y'],points[minus]['y'],base['y'])];odd=subtract(points[plus]['y'],points[minus]['y'])
            symmetry.append(dict(direction=label,odd_norm=S['norm'](odd),even_to_odd=S['norm'](even)/S['norm'](odd)))
    # Held out: the doubled positive probe of each one-sided direction.
    doubled=[dict(direction=label,**of_point(record,{label:2*x['amplitudes'][x['labels'].index(label)]})) for label,record in sorted(x['doubled'].items())]
    # Held out: the frozen proposals of trials 6 and 10 (eight directions only).
    coefficients=dict(zip(x['trial6']['model']['labels'],x['trial6']['model']['coefficients']));old=[d[0] for d in x['flow'][:8]]
    previous9={p['step_fraction']:p['relative_error'] for p in json.loads(S['frozen'](R['ID'])['01-analysis.json'])['proposals']}
    proposals6=[dict(step_fraction=f,model9_relative_error=previous9[f],**of_point(f'proposal/{f:g}',{l:f*coefficients[l] for l in old})) for f in F['POLICY']['step_fractions']]
    trial10=json.loads(S['frozen'](X['ID'])['04-analysis.json']);branches={tuple(b['pattern']):b for b in trial10['model']['branches']};proposals10=[]
    for p in trial10['proposals']:
        b=branches[tuple(p['pattern'])];step={l:p['step_fraction']*c for l,c in zip(trial10['model']['labels'],b['coefficients'])}
        proposals10.append(dict(step_fraction=p['step_fraction'],model9_relative_error=p['relative_prediction_error'],
                                **compare(p['record'],step,p['observed_change_weighted_81'],p['observed_winners'],p['observed_flow_change_kg_s'])))
    held_out=doubled+proposals6+proposals10
    return dict(status='PASS-REPAIRED-CAPACITY-MINIMUM-MODEL-12-EVIDENCE',fixtures=fixtures(),runtime_steps=0,
                scope=dict(points=len(points),directions=x['labels'],flow_directions=[d[0] for d in x['flow']],
                           direct_directions=[d[0] for d in x['direct']],one_sided_directions=sorted(x['doubled']),
                           amplitudes=x['amplitudes'],normalized_lower_bounds=x['lower'],
                           base_winners=[r['winner'] for r in obs['base']],base_margin_to_next_kg_s=[r['margin_to_next_kg_s'] for r in obs['base']]),
                identifiability=dict(singular_values=identifiability,relative_minimum=relative_singular,
                                     probe_flow_sums_kg_s={d[0]:s for d,s in zip(x['flow'],model['sums'])}),
                model=dict(flow_response_by_row=model['H'],capacity_flow_response=model['eta'],
                           direction_response_by_row=model['G'],capacity_direction_response=model['g'],
                           maximum_row_fit_residual=model['maximum_row_fit_residual'],
                           maximum_capacity_fit_residual_kg_s=model['maximum_capacity_fit_residual']),
                structural_checks=dict(torque=structural,inventory=inventory,passed=structural_ok),
                flow_probes=flow_probes,direct_direction_symmetry=symmetry,one_sided_doubled_probes=doubled,
                trial6_proposals=proposals6,trial10_proposals=proposals10,
                summary=dict(flow_probe_winner_agreement=sum(p['winners_agree'] for p in flow_probes),flow_probes=len(flow_probes),
                             maximum_flow_probe_relative_error=max(p['relative_error'] for p in flow_probes),
                             maximum_flow_probe_flow_error_kg_s=max(p['maximum_flow_error_kg_s'] for p in flow_probes),
                             maximum_direct_even_to_odd=max(s['even_to_odd'] for s in symmetry),
                             maximum_one_sided_doubled_relative_error=max(p['relative_error'] for p in doubled),
                             trial6_relative_errors=[p['relative_error'] for p in proposals6],
                             trial10_relative_errors=[p['relative_error'] for p in proposals10],
                             maximum_held_out_relative_error=max(p['relative_error'] for p in held_out),
                             held_out_passed=sum(p['passed'] for p in held_out),held_out=len(held_out)),
                retrospective_checks_passed=structural_ok and all(p['passed'] for p in held_out) and all(p['winners_agree'] for p in flow_probes),
                retrospective_only=True,prospective_validation=False,
                proposal_executed=False,search_started=False,family_selected=False,root_observed=False,r3_passed=False,m10_closed=False,
                limits=['Retrospective: every record was frozen before this contract and the form was explored on them; no proposal enters the fit',
                        'The held-out proposals move only the eight directions of trial 6: no combined step along the 22 added directions has been measured',
                        'Seventeen directions carry no flow term because their probes leave the commanded flow unchanged at the sampled amplitudes; a larger or combined step may still move the minimum through the capacities, which the model tracks only linearly',
                        'One-sided directions are modelled on the positive side only, by a forward difference',
                        'Flow responses are near-collinear across checkpoints: individual cross coefficients are not physical claims',
                        'A validated prediction is not a root, a search or a qualification of R3'])


def execute():
    authenticate();P.require(not (OUT/'01-analysis.json').exists(),'Existing review retained')
    result=analyze();P.write(OUT,'01-analysis.json',result);P.freeze(ID,['01-analysis.json']);return result['status']


def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);P.require(json.loads(json.dumps(analyze()))==json.loads(data['01-analysis.json']),'Replay drift')
    return json.loads(data['01-analysis.json'])['status']+' (offline; zero runtime steps)'


if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','execute','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
