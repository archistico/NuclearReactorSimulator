"""Prospective check of capacity-minimum model 12: one many-direction proposal per feasible winner pattern, two fractions each."""
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
M=runpy.run_path(str(ROOT/'eng/review-r3-repaired-capacity-minimum-model12.py'))
X,R,C,D,S=M['X'],M['R'],M['C'],M['D'],M['S'];T=C['T']
ID='r3-repaired-minimum-proposals13';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT,BINARY=C['PROJECT'],C['BINARY']
CAPACITIES,CHECKPOINTS=R['CAPACITIES'],R['CHECKPOINTS']
POLICY=dict(recipes=18,repeats=2,maximum_runtime_steps=56,branches=4,step_fractions=[.25,.5],
            model='capacity-minimum model 12 on the 30 working directions, rebuilt from the frozen observations 8 and 11 and required identical to its frozen analysis',
            branch_rule='all 125 winner patterns over three checkpoints are tried; a pattern is kept when its linear program is feasible '
                        'with every coefficient inside the sampled amplitude, one-sided coefficients nonnegative, and the winner ahead by the branch margin at the full step',
            objective='max-absolute-weighted-81-residual inside the branch',
            step_choice='the certified primary vertex, without a sparsifying secondary program: the step deliberately moves many directions at once',
            branch_margin_kg_s=1e-3,executed_margin_kg_s=1e-4,numerical_cleanup_tolerance=1e-12,
            minimum_added_directions_moved=10,relative_prediction_tolerance=.1,winner_agreement_required=True,
            predictions_frozen_before_first_physical_call=True,rule_explored_offline_before_contract=True,
            baseline='identical to the frozen observation-8 baseline record',
            energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
            searches=0,production_changes=0,root_criterion_unchanged=True,
            previous_results_rewritten=False,r3_passed=False,m10_closed=False)


def solve_branch(model,base,labels,amplitudes,lower,pattern,row_checkpoint):
    """Minimax over lower <= z <= 1 with the pattern's winner ahead by the branch margin; None when infeasible."""
    capacities,rows=X['affine'](model,base,labels,amplitudes,pattern,row_checkpoint);n=len(labels)
    scale=max(1.,max(abs(c) for c,_ in rows));a=[];b=[]
    for constant,slope in rows:  # x = z - lower >= 0
        shifted=(constant+math.fsum(s*l for s,l in zip(slope,lower)))/scale;s=[v/scale for v in slope]
        a.append(s+[-1.]);b.append(-shifted);a.append([-v for v in s]+[-1.]);b.append(shifted)
    for k,winner in enumerate(pattern):
        w=CAPACITIES.index(winner);wc,ws=capacities[k][w]
        for c in range(len(CAPACITIES)):
            if c!=w:
                oc,os=capacities[k][c];d=[p-q for p,q in zip(ws,os)]
                a.append(d+[0.]);b.append(oc-wc-POLICY['branch_margin_kg_s']-math.fsum(v*l for v,l in zip(d,lower)))
    for j in range(n):a.append([float(i==j) for i in range(n)]+[0.]);b.append(1.-lower[j])
    try:primary=LP.linear_program(a,b,[0.]*n+[-1.])
    except ValueError:return None
    if primary['status']!='OPTIMAL-CERTIFIED':return None
    normalized=[];cleanup=[]
    for j,(value,l) in enumerate(zip(primary['primal'][:n],lower)):
        raw=value+l;used=min(1.,max(l,raw))
        P.require(abs(used-raw)<=POLICY['numerical_cleanup_tolerance'],'Coefficient escaped box')
        if used!=raw:cleanup.append(dict(index=j,raw=raw,used=used))
        normalized.append(used)
    keys=('status','primal_violation','dual_violation','duality_gap','pivots')
    return normalized,dict(primary={k:primary[k] for k in keys},numerical_cleanup=cleanup,linear_optimum=primary['primal'][n]*scale)


@lru_cache(maxsize=1)
def context():
    x=M['build']();parent=json.loads(S['frozen'](M['ID'])['01-analysis.json']);model=x['model']
    P.require(parent['status']=='PASS-REPAIRED-CAPACITY-MINIMUM-MODEL-12-EVIDENCE' and parent['retrospective_checks_passed'],'Model 12 not accepted')
    P.require(model['H']==parent['model']['flow_response_by_row'] and model['eta']==parent['model']['capacity_flow_response']
              and model['G']==parent['model']['direction_response_by_row'] and model['g']==parent['model']['capacity_direction_response'],
              'Rebuilt model differs from the frozen model 12')
    return x


@lru_cache(maxsize=1)
def proposal_plan():
    x=context();model,base,labels,h,rc,lower=x['model'],x['base'],x['labels'],x['amplitudes'],x['row_checkpoint'],x['lower']
    added=set(labels)-{d[0] for d in x['flow'][:8]};one_sided=set(x['doubled']);branches=[]
    for pattern in itertools.product(CAPACITIES,repeat=len(CHECKPOINTS)):
        solved=solve_branch(model,base,labels,h,lower,pattern,rc)
        if solved is None:continue
        normalized,certificate=solved;coefficients=[z*a for z,a in zip(normalized,h)];executions=[]
        moved=[l for l,z in zip(labels,normalized) if z!=0.]
        P.require(sum(l in added for l in moved)>=POLICY['minimum_added_directions_moved'],'Proposal does not exercise the added directions')
        for fraction in POLICY['step_fractions']:
            step={l:fraction*c for l,c in zip(labels,coefficients)};p=R['predict'](model,base,step,rc)
            P.require(p['winners']==list(pattern) and min(p['margins'])>=POLICY['executed_margin_kg_s'],'Executed fraction leaves its declared branch')
            fluid=[math.fsum(s*d['coordinates'][i] for s,d in zip(step.values(),x['descriptions'])) for i in range(24)]
            operating=[math.fsum(s*d['operating'][i] for s,d in zip(step.values(),x['descriptions'])) for i in range(6)]
            P.require(all(abs(v)<=.02 for v in fluid+operating),'Aggregated coordinate outside inherited box')
            P.require(S['norm'](p['change'])>0,'Null predicted proposal')
            single=[S['norm'](R['predict'](model,base,{l:s},rc)['change']) for l,s in step.items() if s!=0.]
            executions.append(dict(step_fraction=fraction,coordinates=fluid,operating=operating,predicted_change_weighted_81=p['change'],
                                   predicted_capacities_kg_s=p['capacities'],predicted_winners=p['winners'],
                                   predicted_margin_to_next_kg_s=p['margins'],predicted_flow_change_kg_s=p['flow_change'],
                                   predicted_maximum_weighted_81=max(abs(a+b) for a,b in zip(base['y'],p['change'])),
                                   cancellation_ratio=S['norm'](p['change'])/math.fsum(single)))
        branches.append(dict(pattern=list(pattern),normalized_coefficients=normalized,coefficients=coefficients,certificate=certificate,
                             directions_moved=len(moved),added_directions_moved=sorted(l for l in moved if l in added),
                             one_sided_directions_moved=sorted(l for l in moved if l in one_sided),
                             directions_at_bound=sum(abs(z)==1. for z in normalized),executions=executions))
    P.require(len(branches)==POLICY['branches'],'Branch count differs from the contracted budget')
    return dict(labels=labels,amplitudes=h,normalized_lower_bounds=lower,baseline_weighted_81=base['y'],baseline_maximum_weighted_81=max(map(abs,base['y'])),
                base_winners=[CAPACITIES[min(range(len(CAPACITIES)),key=lambda c:base['cap'][k][c])] for k in range(len(CHECKPOINTS))],
                branches=branches,full_steps_not_executed=True)


label_of=X['label_of']


def requests():
    points=[dict(Id='base',Coordinates=[0.]*24,OperatingPointFractions=[0.]*6)]
    points+=[dict(Id=label_of(b,e),Coordinates=e['coordinates'],OperatingPointFractions=e['operating']) for b in proposal_plan()['branches'] for e in b['executions']]
    return [{**point,'Id':point['Id']+f'/repeat/{repeat}'} for point in points for repeat in (1,2)]


def fixtures():
    # Synthetic kink with a one-sided third input: each branch optimum is exact on the truth and respects the sign.
    g1,g2,g3,h0,y0=[2.,-1.],[.5,3.],[1.,1.],[4.,-2.],[.3,-.2];cap=lambda v:[1.+v[0]-v[1],1.+2*v[1]]
    def point(v):
        f=min(cap(v));return dict(y=[y0[i]+g1[i]*v[0]+g2[i]*v[1]+g3[i]*v[2]+h0[i]*(f-1.) for i in range(2)],flow=[f],cap=[cap(v)+[9.,9.,9.]])
    h=1e-3;unit=lambda j,s:[s*h if i==j else 0. for i in range(3)]
    pts=dict(base=point([0.,0.,0.]),ap=point(unit(0,1)),am=point(unit(0,-1)),bp=point(unit(1,1)),bm=point(unit(1,-1)),cp=point(unit(2,1)))
    model=M['extend'](R['fit'](pts,[('a',h,'ap','am'),('b',h,'bp','bm')],2,[0,0],levels=1),pts,pts['base'],[0,0],[('c',h,'cp',None)]);found=[]
    for pattern in (('stop',),('control',),('admission',)):
        solved=solve_branch(model,pts['base'],['a','b','c'],[.1,.1,.1],[-1.,-1.,0.],pattern,[0,0])
        if solved is None:continue
        z,certificate=solved;v=[.1*q for q in z];truth=point(v);caps=cap(v)
        P.require(abs(max(map(abs,truth['y']))-certificate['linear_optimum'])<1e-7 and z[2]>=0.,'Branch optimum fixture')
        P.require(CAPACITIES[min(range(2),key=lambda c:caps[c])]==pattern[0] and abs(caps[0]-caps[1])>=POLICY['branch_margin_kg_s']-1e-9,'Branch margin fixture')
        found.append(pattern[0])
    P.require(found==['stop','control'],'Branch enumeration fixture')
    P.require(POLICY['recipes']==2*(1+POLICY['branches']*len(POLICY['step_fractions'])) and POLICY['maximum_runtime_steps']==2+3*POLICY['recipes'],'Budget fixture')
    return 'PASS-MINIMUM-PROPOSAL-13-FIXTURES'


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
            if r['Residuals'] is not None:
                P.H3['H2']['verify_physics'](r,plan);energy_errors+=S['verify_extra'](r);torque_errors+=T['audit_mechanical'](r)
        P.require({k:v for k,v in pair[0].items() if k!='Id'}=={k:v for k,v in pair[1].items() if k!='Id'},'Repeatability drift')
        points[pair[0]['Id'].rsplit('/repeat/',1)[0]]=pair[0]
    base=points['base']
    P.require({k:v for k,v in base.items() if k!='Id'}=={k:v for k,v in x['base_record'].items() if k!='Id'},'Parent baseline drift')
    baseline=S['weighted_vector'](base,scales,names);base_flow=[r['commanded_flow_kg_s'] for r in C['reconstruct'](base)];model=proposal_plan();proposals=[]
    for branch in model['branches']:
        for e in branch['executions']:
            record=points[label_of(branch,e)];guards=D['reasons'](record,base)
            row=dict(record=label_of(branch,e),pattern=branch['pattern'],step_fraction=e['step_fraction'],guards=guards,
                     cancellation_ratio=e['cancellation_ratio'],predicted_winners=e['predicted_winners'],
                     predicted_maximum_weighted_81=e['predicted_maximum_weighted_81'])
            capacity,failure=(None,record['Reason']) if record['Residuals'] is None else M['O']['observe'](record)
            if capacity is None:
                row.update(prediction_validated=False,relative_prediction_error=None,capacity_reconstruction_failure=failure);proposals.append(row);continue
            observed=[a-b for a,b in zip(S['weighted_vector'](record,scales,names),baseline)]
            winners=[r['winner'] for r in capacity];flow=[r['commanded_flow_kg_s']-b for r,b in zip(capacity,base_flow)]
            error=S['relative'](observed,e['predicted_change_weighted_81'])
            row.update(relative_prediction_error=error,observed_winners=winners,winners_agree=winners==e['predicted_winners'],
                       predicted_margin_to_next_kg_s=e['predicted_margin_to_next_kg_s'],observed_margin_to_next_kg_s=[r['margin_to_next_kg_s'] for r in capacity],
                       predicted_flow_change_kg_s=e['predicted_flow_change_kg_s'],observed_flow_change_kg_s=flow,
                       maximum_flow_error_kg_s=max(abs(a-b) for a,b in zip(flow,e['predicted_flow_change_kg_s'])),
                       maximum_capacity_error_kg_s=max(abs(r['capacities_kg_s'][c]-p[c]) for r,p in zip(capacity,e['predicted_capacities_kg_s']) for c in CAPACITIES),
                       observed_maximum_weighted_81=max(map(abs,S['weighted_vector'](record,scales,names))),
                       observed_maximum_original_scaled_residual=max(map(abs,record['Residuals'])),
                       prediction_validated=bool(guards['available'] and not guards['phase_changes'] and winners==e['predicted_winners']
                           and error<=POLICY['relative_prediction_tolerance']),
                       observed_change_weighted_81=observed)
            proposals.append(row)
    validated=[p['prediction_validated'] for p in proposals]
    return dict(status='PASS-REPAIRED-MINIMUM-PROPOSAL-13-EVIDENCE',fixtures=fixtures(),recipes=POLICY['recipes'],
                maximum_runtime_steps=POLICY['maximum_runtime_steps'],exact_repeats=True,baseline_matches_parent=True,
                baseline_maximum_weighted_81=max(map(abs,baseline)),baseline_maximum_original_scaled_residual=max(map(abs,base['Residuals'])),
                proposals=proposals,predictions_validated=sum(validated),predictions=len(validated),
                all_predictions_validated=all(validated),prospective=True,
                maximum_energy_ledger_error_w=max(map(abs,energy_errors)),maximum_torque_ledger_error_nm=max(map(abs,torque_errors)),
                model=model,search_started=False,family_selected=False,root_observed=False,r3_passed=False,m10_closed=False,
                limits=['One vertex step per feasible winner pattern: other combinations of the 30 directions are not measured',
                        'Patterns without a step at the declared margin are not tested',
                        'Full model steps are not executed: only their 0.25 and 0.5 fractions, inside the sampled amplitudes',
                        'A validated prediction is not a root, a search or a qualification of R3; no seed is installed'])


def adjudicate():
    authenticate();P.require(P.load(OUT/'01-model.json')==json.loads(json.dumps(proposal_plan())),'Preexecution model drift')
    result=analyze([json.loads(s) for s in (OUT/'03-measurements.jsonl').read_text(encoding='utf-8').splitlines()])
    P.write(OUT,'04-analysis.json',result);return result['status']


def execute():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing acquisition retained')
    P.require(P.load(OUT/'01-model.json')==json.loads(json.dumps(proposal_plan())),'Preexecution model drift')
    with (OUT/'execution.log').open('x',encoding='utf-8') as stream:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID],cwd=ROOT,stdout=stream,stderr=subprocess.STDOUT,check=True)
    return adjudicate()


def freeze():
    adjudicate();P.freeze(ID,['00-build.log','01-model.json','01-requests.json','02-preexecution.json','03-measurements.jsonl','04-analysis.json','execution.log'])
    return 'PASS-FROZEN-MINIMUM-PROPOSALS-13'


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
            print(b['pattern'],b['certificate']['linear_optimum'],b['directions_moved'],len(b['added_directions_moved']),b['one_sided_directions_moved'],b['directions_at_bound'])
            for e in b['executions']:print('  ',e['step_fraction'],e['predicted_maximum_weighted_81'],min(e['predicted_margin_to_next_kg_s']),e['cancellation_ratio'])
    else:print(globals()[sys.argv[1]]())
