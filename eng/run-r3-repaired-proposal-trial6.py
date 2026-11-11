"""Two small, repeated physical checks of a bounded directional linear prediction."""
from __future__ import annotations
from functools import lru_cache
import json
import math
import runpy
import subprocess
import sys
import r3_h_protocol as P
import r3_h_minimax_lp as LP

ROOT=P.ROOT
E=runpy.run_path(str(ROOT/'eng/run-r3-repaired-steam-directions5.py'))
D,B,T,S=E['D'],E['B'],E['T'],E['S']
ID='r3-repaired-proposal-trial6';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT,BINARY=E['PROJECT'],E['BINARY']
POLICY=dict(recipes=6,repeats=2,maximum_runtime_steps=20,step_fractions=[.25,.5],
            objective='max-absolute-weighted-81-residual; secondary minimum normalized L1 step',
            relative_prediction_tolerance=.1,secondary_objective_allowance=1e-9,numerical_cleanup_tolerance=1e-12,
            physical_weights=S['POLICY']['physical_weights'],energy_ledger_tolerance_w=.01,
            mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
            keep_one_sided_nonnegative=True,searches=0,production_changes=0,root_criterion_unchanged=True,r3_passed=False,m10_closed=False)

@lru_cache(maxsize=1)
def basis():
    data=S['frozen'](E['ID']);analysis=json.loads(data['04-analysis.json'])
    base=json.loads(data['03-measurements.jsonl'].splitlines()[0])
    P.require(analysis['selected_direction_count']==analysis['rank_diagnostic']['rank']==30,'Directional basis not complete')
    return analysis,base

def solve_box(matrix,residual,lower,upper):
    n=len(lower);scale=max(1.,max(map(abs,residual)))
    a=[[x/scale for x in row] for row in matrix]
    shifted=[r/scale+math.fsum(x*l for x,l in zip(row,lower)) for r,row in zip(residual,a)]
    constraints=[row+[-1.] for row in a]+[[-x for x in row]+[-1.] for row in a]
    rhs=[-x for x in shifted]+shifted
    for j in range(n):
        constraints.append([float(i==j) for i in range(n)]+[0.]);rhs.append(upper[j]-lower[j])
    primary=LP.linear_program(constraints,rhs,[0.]*n+[-1.])
    P.require(primary['status']=='OPTIMAL-CERTIFIED','Primary LP not certified')
    second=[row+[0.]*n for row in constraints];second_rhs=rhs[:]
    second.append([0.]*n+[1.]+[0.]*n);second_rhs.append(primary['primal'][n]+POLICY['secondary_objective_allowance'])
    for j in range(n):
        row=[0.]*(2*n+1);row[j]=1.;row[n+1+j]=-1.;second.append(row);second_rhs.append(-lower[j])
        row=[0.]*(2*n+1);row[j]=-1.;row[n+1+j]=-1.;second.append(row);second_rhs.append(lower[j])
    secondary=LP.linear_program(second,second_rhs,[0.]*(n+1)+[-1.]*n)
    P.require(secondary['status']=='OPTIMAL-CERTIFIED','Secondary LP not certified')
    coefficients=[];cleanup=[]
    for j,(value,l,u) in enumerate(zip(secondary['primal'][:n],lower,upper)):
        raw=l+value;used=min(u,max(l,raw))
        P.require(abs(used-raw)<=POLICY['numerical_cleanup_tolerance'],'Coefficient escaped box')
        if used!=raw:cleanup.append(dict(index=j,raw=raw,used=used))
        coefficients.append(used)
    return coefficients,dict(primary={k:primary[k] for k in ('status','primal_violation','dual_violation','duality_gap','pivots')},
                             secondary={k:secondary[k] for k in ('status','primal_violation','dual_violation','duality_gap','pivots')},
                             numerical_cleanup=cleanup,linear_optimum=primary['primal'][n]*scale)

@lru_cache(maxsize=1)
def proposal_plan():
    analysis,base=basis();_,scales,names=S['layout']();residual=S['weighted_vector'](base,scales,names)
    labels=sorted(analysis['working_directions']);descriptions=[analysis['working_directions'][name] for name in labels]
    bounds=[d['sampled_amplitude'] for d in descriptions]
    matrix=[[analysis['working_weighted_81_columns'][name][i]*h for name,h in zip(labels,bounds)] for i in range(81)]
    lower=[0. if d['coefficient_sign']=='nonnegative' else -1. for d in descriptions]
    normalized,certificate=solve_box(matrix,residual,lower,[1.]*len(labels))
    coefficients=[x*h for x,h in zip(normalized,bounds)]
    fluid=[math.fsum(c*d['coordinates'][i] for c,d in zip(coefficients,descriptions)) for i in range(24)]
    operating=[math.fsum(c*d['operating'][i] for c,d in zip(coefficients,descriptions)) for i in range(6)]
    P.require(all(abs(x)<=.02 for x in fluid+operating),'Aggregated coordinate outside inherited box')
    delta=[math.fsum(row[j]*normalized[j] for j in range(len(labels))) for row in matrix]
    P.require(S['norm'](delta)>0,'Null predicted proposal')
    return dict(labels=labels,coefficients=coefficients,normalized_coefficients=normalized,
                certificate=certificate,coordinates=fluid,operating=operating,predicted_change=delta,baseline_weighted_81=residual,
                linear_full_step_only=True)

def requests():
    plan=proposal_plan();points=[dict(Id='base',Coordinates=[0.]*24,OperatingPointFractions=[0.]*6)]
    for fraction in POLICY['step_fractions']:
        points.append(dict(Id=f'proposal/{fraction:g}',Coordinates=[fraction*x for x in plan['coordinates']],
                           OperatingPointFractions=[fraction*x for x in plan['operating']]))
    return [{**point,'Id':point['Id']+f'/repeat/{repeat}'} for point in points for repeat in (1,2)]

def fixtures():
    solution,_=solve_box([[1.]],[1.],[0.],[1.])
    P.require(abs(solution[0])<1e-8,'One-sided fixture admitted negative coefficient')
    solution,_=solve_box([[1.]],[1.],[-1.],[1.])
    P.require(abs(solution[0]+1.)<1e-8,'Free coefficient fixture')
    return 'PASS-PROPOSAL-TRIAL-FIXTURES'

def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for entry in c['implementation_files']+c['frozen_inputs']:
        P.require(P.digest((ROOT/entry['path']).read_bytes())==entry['sha256'],'Input drift: '+entry['path'])

def preexecution():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing acquisition retained')
    P.write(OUT,'01-model.json',proposal_plan());P.write(OUT,'01-requests.json',requests())
    P.require(BINARY.exists(),'Build runner first')
    P.write(OUT,'02-preexecution.json',dict(status='PASS-PREEXECUTION',contract_sha256=P.digest(CONTRACT.read_bytes()),
            requests_sha256=P.digest((OUT/'01-requests.json').read_bytes()),model_sha256=P.digest((OUT/'01-model.json').read_bytes()),
            fixtures=fixtures(),runtime_steps=0,assemblies=[dict(file=p.name,sha256=P.digest(p.read_bytes())) for p in sorted(BINARY.parent.glob('*.dll'))]))
    subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'--validate-only'],cwd=ROOT,check=True)
    return 'PASS-PREEXECUTION'

def analyze(records):
    expected=requests();P.require(len(records)==len(expected)==6,'Recipe budget drift')
    plan,scales,names=S['layout']();points={};energy_errors=[];torque_errors=[]
    for i in range(0,len(records),2):
        pair=records[i:i+2]
        for r,request in zip(pair,expected[i:i+2]):
            P.require(all(r[k]==request[k] for k in request),'Request/response drift')
            P.H3['H2']['verify_physics'](r,plan);energy_errors+=S['verify_extra'](r);torque_errors+=T['audit_mechanical'](r)
        P.require({k:v for k,v in pair[0].items() if k!='Id'}=={k:v for k,v in pair[1].items() if k!='Id'},'Repeatability drift')
        points[pair[0]['Id'].rsplit('/repeat/',1)[0]]=pair[0]
    base=points['base'];_,prior=basis()
    for field in ('Raw','Residuals','Phases'):P.require(base[field]==prior[field],'Parent baseline drift')
    P.require([o['StateSha256'] for o in base['Observations']]==[o['StateSha256'] for o in prior['Observations']],'Parent full-state drift')
    model=proposal_plan();baseline=S['weighted_vector'](base,scales,names);proposals=[]
    for fraction in POLICY['step_fractions']:
        record=points[f'proposal/{fraction:g}'];predicted=[fraction*x for x in model['predicted_change']]
        guards=D['reasons'](record,base)
        observed=[a-b for a,b in zip(S['weighted_vector'](record,scales,names),baseline)] if record['Residuals'] else None
        error=S['relative'](observed,predicted) if observed is not None else None
        proposals.append(dict(step_fraction=fraction,guards=guards,relative_prediction_error=error,
                              prediction_validated=guards['available'] and error<=POLICY['relative_prediction_tolerance'],
                              predicted_change_weighted_81=predicted,observed_change_weighted_81=observed,
                              observed_maximum_original_scaled_residual=max(map(abs,record['Residuals'])) if record['Residuals'] else None,
                              observed_maximum_weighted_81=max(map(abs,S['weighted_vector'](record,scales,names))) if record['Residuals'] else None))
    return dict(status='PASS-REPAIRED-PROPOSAL-TRIAL-EVIDENCE',fixtures=fixtures(),recipes=6,maximum_runtime_steps=20,
                exact_repeats=True,baseline_matches_parent=True,proposals=proposals,
                both_predictions_validated=all(p['prediction_validated'] for p in proposals),
                maximum_energy_ledger_error_w=max(map(abs,energy_errors)),maximum_torque_ledger_error_nm=max(map(abs,torque_errors)),
                model=proposal_plan(),search_started=False,family_selected=False,root_observed=False,r3_passed=False,m10_closed=False,
                limits=['Prediction agreement is required for the full 81-row change and all existing guards',
                        'A model-optimal step is not a physical root; this trial does not install a seed or launch a search'])

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
    return 'PASS-FROZEN-PROPOSAL-TRIAL'

def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);P.require(json.loads(data['01-model.json'])==proposal_plan(),'Frozen model drift')
    result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()]);P.require(result==json.loads(data['04-analysis.json']),'Replay drift')
    return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
