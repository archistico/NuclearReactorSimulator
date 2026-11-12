"""Prospective original-75 minimax proposals with separate thermal and capacity constraints."""
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
X=runpy.run_path(str(ROOT/'eng/run-r3-repaired-local-proposals22.py'))
R,L,Q,S,D,C=X['R'],X['L'],X['Q'],X['S'],X['D'],X['C']
ID='r3-repaired-root-objective23';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT,BINARY=L['PROJECT'],L['BINARY']
POLICY=dict(maximum_recipes=8,maximum_batches=1,maximum_batch_recipes=8,maximum_runtime_steps=26,repeats=2,
    step_fractions=[.25,.5],objective='maximum absolute original 75 scaled residuals, unchanged scales; secondary minimum normalized L1 step',
    box='sampled amplitudes from observations20-21; suction coefficients nonnegative',
    thermal_constraint='each of six predicted thermal rates bounded by max(abs(center rate),1 W); measured excess allowance 1 W',
    thermal_floor_w=1.,thermal_measurement_allowance_w=1.,
    capacity_constraint='all predicted expansion-to-other gaps retain at least half the center margin at each checkpoint',
    secondary_objective_allowance=1e-9,numerical_cleanup_tolerance=1e-12,relative_prediction_tolerance=.1,
    capacity_relative_tolerance=.1,capacity_prediction_norm_floor_kg_s=1e-9,
    winner_agreement_required=True,measured_minimum_margin_fraction=.5,original_maximum_must_decrease=True,
    neighbourhood_absolute_fraction_radius=.0002,binary64_domain_tolerance=1e-15,
    energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
    searches=0,production_changes=0,old_domains_unchanged=True,root_criterion_unchanged=True,
    seed_installation=False,r3_passed=False,m10_closed=False)

def constrained_minimax(matrix,residual,lower,upper,auxiliary):
    """Signed box, original minimax objective, independent linear inequalities and an L1 tie-break."""
    n=len(lower);scale=max(1.,max(map(abs,residual)))
    a=[[v/scale for v in row] for row in matrix]
    shifted=[v/scale+math.fsum(c*l for c,l in zip(row,lower)) for row,v in zip(a,residual)]
    constraints=[row+[-1.] for row in a]+[[-v for v in row]+[-1.] for row in a];rhs=[-v for v in shifted]+shifted
    for row,bound in auxiliary:
        row_scale=max([abs(bound)]+list(map(abs,row))+[1e-30])
        normalized=[v/row_scale for v in row]
        constraints.append(normalized+[0.]);rhs.append(bound/row_scale-math.fsum(v*l for v,l in zip(normalized,lower)))
    for j in range(n):constraints.append([float(i==j) for i in range(n)]+[0.]);rhs.append(upper[j]-lower[j])
    primary=LP.linear_program(constraints,rhs,[0.]*n+[-1.]);P.require(primary['status']=='OPTIMAL-CERTIFIED','Primary constrained LP not certified')
    second=[row+[0.]*n for row in constraints];second_rhs=rhs[:]
    second.append([0.]*n+[1.]+[0.]*n);second_rhs.append(primary['primal'][n]+POLICY['secondary_objective_allowance'])
    for j in range(n):
        row=[0.]*(2*n+1);row[j]=1.;row[n+1+j]=-1.;second.append(row);second_rhs.append(-lower[j])
        row=[0.]*(2*n+1);row[j]=-1.;row[n+1+j]=-1.;second.append(row);second_rhs.append(lower[j])
    secondary=LP.linear_program(second,second_rhs,[0.]*(n+1)+[-1.]*n);P.require(secondary['status']=='OPTIMAL-CERTIFIED','Secondary constrained LP not certified')
    coefficients=[];cleanup=[]
    for j,(v,lo,hi) in enumerate(zip(secondary['primal'][:n],lower,upper)):
        raw=lo+v;used=min(hi,max(lo,raw));P.require(abs(used-raw)<=1e-12,'Coefficient escaped box')
        if used!=raw:cleanup.append(dict(index=j,raw=raw,used=used))
        coefficients.append(used)
    audit=lambda result:{k:result[k] for k in ('status','primal_violation','dual_violation','duality_gap','pivots')}
    return coefficients,dict(primary=audit(primary),secondary=audit(secondary),numerical_cleanup=cleanup,
        original_linear_optimum=primary['primal'][n]*scale,objective_scale=scale)

@lru_cache(maxsize=1)
def model():
    data=S['frozen'](R['ID']);basis=json.loads(data['04-analysis.json']);prior,points,_=R['lineage']()
    center=points['center'];historical=points['historical'];_,scales,names=S['layout']();weights=S['POLICY']['physical_weights']
    P.require(basis['selected_direction_count']==basis['rank_diagnostic']['rank']==30,'Remeasured basis incomplete')
    labels=sorted(basis['working_directions']);descriptions=[basis['working_directions'][k] for k in labels];h=[d['sampled_amplitude'] for d in descriptions]
    weighted=S['weighted_vector'](center,scales,names)
    factors=[weights[{'mass':'mass_kg_s','energy':'energy_w','torque':'torque_nm'}[name[2]]]/s for name,s in zip(names,scales)]
    original=[[basis['working_weighted_81_columns'][k][i]*amp*factors[i] for k,amp in zip(labels,h)] for i in range(75)]
    wm=[[basis['working_weighted_81_columns'][k][i]*amp for k,amp in zip(labels,h)] for i in range(81)]
    thermal_base=[v*weights['thermal_w'] for v in weighted[75:]]
    thermal_matrix=[[v*weights['thermal_w'] for v in row] for row in wm[75:]]
    limits=[max(abs(v),1.) for v in thermal_base]
    cap0=L['capacity_vector'](prior['observations']['center'])
    cm=[[basis['working_capacity_columns'][k][i]*amp for k,amp in zip(labels,h)] for i in range(15)]
    auxiliary=[];constraints=[]
    for i,(row,value,bound) in enumerate(zip(thermal_matrix,thermal_base,limits)):
        for sign in (1.,-1.):
            auxiliary.append(([sign*v for v in row],bound-sign*value));constraints.append(dict(kind='thermal',row=i,sign=sign,limit_w=bound))
    e=C['CAPACITIES'].index('expansion')
    for checkpoint in range(3):
        for k in range(5):
            if k==e:continue
            a,b=5*checkpoint+e,5*checkpoint+k;gap=cap0[b]-cap0[a];P.require(gap>0.,'Center expansion branch lacks strict margin')
            auxiliary.append(([u-v for u,v in zip(cm[a],cm[b])],.5*gap))
            constraints.append(dict(kind='capacity_margin',checkpoint=checkpoint,competitor=C['CAPACITIES'][k],minimum_gap_kg_s=.5*gap))
    lower=[0. if d['coefficient_sign']=='nonnegative' else -1. for d in descriptions]
    normalized,certificate=constrained_minimax(original,center['Residuals'],lower,[1.]*30,auxiliary)
    coefficient=[v*amp for v,amp in zip(normalized,h)]
    multiply=lambda matrix:[math.fsum(v*c for v,c in zip(row,normalized)) for row in matrix]
    fluid=[math.fsum(v*d['coordinates'][i] for v,d in zip(coefficient,descriptions)) for i in range(24)]
    operating=[math.fsum(v*d['operating'][i] for v,d in zip(coefficient,descriptions)) for i in range(6)]
    P.require(all(abs(v)<=.0002+1e-15 for v in fluid+operating),'Aggregate outside diagnostic neighbourhood')
    delta=multiply(original);P.require(max(map(abs,[a+b for a,b in zip(center['Residuals'],delta)]))<max(map(abs,center['Residuals'])),'No predicted reduction of original maximum')
    return dict(labels=labels,normalized_coefficients=normalized,coefficients=coefficient,certificate=certificate,
        center=center,historical=historical,baseline_original_75=center['Residuals'],baseline_weighted_81=weighted,
        baseline_thermal_w=thermal_base,thermal_limits_w=limits,baseline_capacities=cap0,
        predicted_original_change=delta,predicted_weighted_change=multiply(wm),predicted_thermal_change_w=multiply(thermal_matrix),predicted_capacity_change=multiply(cm),
        coordinates=fluid,operating=operating,constraints=constraints,
        auxiliary_slacks=[bound-math.fsum(v*x for v,x in zip(row,normalized)) for row,bound in auxiliary])

def predictions():
    m=model();return [{k:v for k,v in m.items() if k not in ('center','historical')},
        [dict(step_fraction=f,original_change=[f*v for v in m['predicted_original_change']],weighted_change=[f*v for v in m['predicted_weighted_change']],
            thermal_change_w=[f*v for v in m['predicted_thermal_change_w']],capacity_change=[f*v for v in m['predicted_capacity_change']]) for f in POLICY['step_fractions']]]

def requests():
    m=model();points=[dict(Id=k,Coordinates=m[k]['Coordinates'],OperatingPointFractions=m[k]['OperatingPointFractions']) for k in ('historical','center')]
    for f in POLICY['step_fractions']:
        points.append(dict(Id=f'proposal/{f:g}',Coordinates=[a+f*v for a,v in zip(m['center']['Coordinates'],m['coordinates'])],
            OperatingPointFractions=[a+f*v for a,v in zip(m['center']['OperatingPointFractions'],m['operating'])]))
    return [{**p,'Id':p['Id']+f'/repeat/{repeat}'} for p in points for repeat in (1,2)]

def fixtures():
    x,cert=constrained_minimax([[1.]],[1.],[-1.],[1.],[([-1.],.25)])
    P.require(abs(x[0]+.25)<2e-9 and abs(cert['original_linear_optimum']-.75)<1e-9,'Constrained original-objective fixture')
    x,_=constrained_minimax([[1.]],[1.],[0.],[1.],[]);P.require(abs(x[0])<1e-8,'One-sided fixture')
    x,_=constrained_minimax([[1.,0.]],[1.],[-1.,-1.],[1.,1.],[])
    P.require(abs(x[0]+1.)<1e-8 and abs(x[1])<1e-8,'L1 tie-break fixture')
    m=model();_,scales,names=S['layout']();w=S['POLICY']['physical_weights']
    for v,weighted,s,name in zip(m['baseline_original_75'],m['baseline_weighted_81'],scales,names):
        recovered=weighted*w[{'mass':'mass_kg_s','energy':'energy_w','torque':'torque_nm'}[name[2]]]/s
        P.require(abs(v-recovered)<=1e-8*max(1.,abs(v)),'Original-scale conversion fixture')
    P.require(len(requests())==len({r['Id'] for r in requests()})==8 and 2+3*8==26,'Budget fixture')
    return 'PASS-ROOT-OBJECTIVE-23-FIXTURES'

def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for e in c['implementation_files']+c['frozen_inputs']:P.require(P.digest((ROOT/e['path']).read_bytes())==e['sha256'],'Input drift: '+e['path'])

def preexecution():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists() and not (OUT/'batch-001').exists(),'Existing execution retained')
    P.require(BINARY.exists(),'Build the local-basis console first')
    with (OUT/'00-domain-fixtures.log').open('w',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),'--self-test'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    P.write(OUT,'01-predictions.json',predictions())
    token=dict(status='PASS-PREEXECUTION',contract_sha256=P.digest(CONTRACT.read_bytes()),fixtures=fixtures(),runtime_steps=0,recipes=8,
        predictions_sha256=P.digest((OUT/'01-predictions.json').read_bytes()),assemblies=[dict(file=x.name,sha256=P.digest(x.read_bytes())) for x in sorted(BINARY.parent.glob('*.dll'))])
    for folder in (OUT,OUT/'batch-001'):
        P.write(folder,'01-requests.json',requests());P.write(folder,'02-preexecution.json',dict(**token,requests_sha256=P.digest((folder/'01-requests.json').read_bytes())))
    with (OUT/'preflight.log').open('w',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'1','--validate-only'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    return 'PASS-PREEXECUTION'

def physical_residuals(record):
    _,scales,names=S['layout']()
    return [dict(checkpoint=c,node=n,kind=k,scaled=v,physical=v*s) for (c,n,k),v,s in zip(names,record['Residuals'],scales)]

def analyze(records):
    first=Q['audit_pairs'](records,requests());points={r['Id'].rsplit('/repeat/',1)[0]:r for r in first};m=model();_,scales,names=S['layout']()
    for k in ('historical','center'):
        P.require({p:v for p,v in points[k].items() if p!='Id'}=={p:v for p,v in m[k].items() if p!='Id'},'Parent full-record drift')
    observed=[];base_max=max(map(abs,m['baseline_original_75']))
    for p in predictions()[1]:
        f=p['step_fraction'];r=points[f'proposal/{f:g}'];guards=D['reasons'](r,points['center'])
        result=dict(step_fraction=f,guards=guards,prediction_conforming=False,reduces_original_maximum=False,eligible_for_later_review=False)
        if r['Residuals'] is not None:
            original_delta=[a-b for a,b in zip(r['Residuals'],m['baseline_original_75'])]
            weighted=S['weighted_vector'](r,scales,names);wd=[a-b for a,b in zip(weighted,m['baseline_weighted_81'])]
            thermal=[v*S['POLICY']['physical_weights']['thermal_w'] for v in weighted[75:]]
            error=L['deviation'](original_delta,p['original_change']);we=L['deviation'](wd,p['weighted_change'])
            thermal_ok=all(abs(v)<=bound+1. for v,bound in zip(thermal,m['thermal_limits_w']))
            maximum=max(map(abs,r['Residuals']));predicted=max(abs(a+b) for a,b in zip(m['baseline_original_75'],p['original_change']))
            result.update(relative_prediction_error_original_75=error,relative_prediction_error_weighted_81=we,
                observed_maximum_original=maximum,predicted_maximum_original=predicted,observed_original_change=original_delta,
                observed_thermal_w=thermal,thermal_constraints_conforming=thermal_ok,
                observed_maximum_weighted_81=max(map(abs,weighted)),reduces_original_maximum=maximum<base_max,
                physical_residuals=physical_residuals(r),cycle_powers=[o['CyclePower'] for o in r['Observations']])
            try:
                capacities=C['reconstruct'](r);cap=L['capacity_vector'](capacities);capdelta=[a-b for a,b in zip(cap,m['baseline_capacities'])]
                caperror=L['deviation'](capdelta,p['capacity_change'],1e-9);winners=[v['winner'] for v in capacities]
                e=C['CAPACITIES'].index('expansion')
                retained=min((cap[5*i+j]-cap[5*i+e])/(m['baseline_capacities'][5*i+j]-m['baseline_capacities'][5*i+e]) for i in range(3) for j in range(5) if j!=e)
                result.update(relative_capacity_prediction_error=caperror,observed_winners=winners,minimum_retained_capacity_margin_fraction=retained)
                result['prediction_conforming']=guards['available'] and error<=.1 and we<=.1 and caperror<=.1 and winners==['expansion']*3 and retained>=.5
                result['eligible_for_later_review']=result['prediction_conforming'] and thermal_ok and maximum<base_max
            except Exception as e:result['capacity_failure']=str(e)
        observed.append(result)
    return dict(status='PASS-REPAIRED-ROOT-OBJECTIVE-23-EVIDENCE',fixtures=fixtures(),recipes=8,maximum_runtime_steps=26,
        exact_repeats=True,parent_full_records_identical=True,model=predictions()[0],baseline_maximum_original=base_max,proposals=observed,
        both_predictions_conforming=all(r['prediction_conforming'] for r in observed),both_proposals_eligible_for_later_review=all(r['eligible_for_later_review'] for r in observed),
        maximum_global_energy_ledger_error_w=max(abs(e) for r in records if r['Residuals'] is not None for e in Q['audit_power'](r)),
        integrated_root_observed=any(r['Residuals'] is not None and max(map(abs,r['Residuals']))<=1 for r in first),
        integrated_search_started=False,seed_installed=False,r3_passed=False,m10_closed=False,
        limits=['Two prospective original-objective proposals only, with a new composition of directions',
                'Separate thermal and capacity constraints do not change the original root scales or tolerance',
                'No proposal is installed or accepted as an integrated-search update; no hundred-step confirmation or R3 qualification'])

def execute():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing execution retained')
    P.require(P.load(OUT/'01-predictions.json')==predictions(),'Prospective prediction drift')
    with (OUT/'execution.log').open('x',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'1'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    (OUT/'03-measurements.jsonl').write_bytes((OUT/'batch-001/03-measurements.jsonl').read_bytes())
    return adjudicate()

def adjudicate():
    authenticate();P.require(P.load(OUT/'01-requests.json')==requests() and P.load(OUT/'01-predictions.json')==predictions(),'Prospective input drift')
    result=analyze([json.loads(s) for s in (OUT/'03-measurements.jsonl').read_text(encoding='utf-8').splitlines()]);P.write(OUT,'04-analysis.json',result);return result['status']

def freeze():
    adjudicate();P.freeze(ID,['00-domain-fixtures.log','01-predictions.json','01-requests.json','02-preexecution.json','03-measurements.jsonl','04-analysis.json','execution.log'])
    return 'PASS-FROZEN-ROOT-OBJECTIVE-23'

def review():
    authenticate();P.require(P.load(ROOT/'eng'/(ID+'-returned.json'))['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);P.require(json.loads(data['01-requests.json'])==requests() and json.loads(data['01-predictions.json'])==predictions(),'Frozen prospective input drift')
    result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()]);P.require(result==json.loads(data['04-analysis.json']),'Frozen analysis drift')
    return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
