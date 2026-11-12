"""Original-75 minimax with measured operational-envelope derivatives and thermal headroom."""
from __future__ import annotations
from functools import lru_cache
import json
import math
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
U=runpy.run_path(str(ROOT/'eng/run-r3-repaired-root-objective23.py'))
R,L,Q,S,D,C=U['R'],U['L'],U['Q'],U['S'],U['D'],U['C']
ID='r3-repaired-envelope-objective24';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT,BINARY=L['PROJECT'],L['BINARY']
ENVELOPE=dict(ElectricalMwe=[4.99,5.01],PumpKgS=[99.9,100.1],DrumLevel=[.49,.51],Governor=[29.27,29.30])
POLICY=dict(maximum_recipes=8,maximum_batches=1,maximum_batch_recipes=8,maximum_runtime_steps=26,repeats=2,
    step_fractions=[.5,1.],objective='maximum absolute original 75 scaled residuals with unchanged scales; secondary normalized L1',
    operational_envelope=ENVELOPE,predicted_envelope_buffer_fraction=.1,
    envelope_derivatives='same selected stencils of observations20-21; physical derivatives normalized by envelope width for the comparison only',
    envelope_derivative_relative_tolerance=.1,envelope_derivative_norm_floor=1.,
    unavailable_envelope_derivative='freeze that direction coefficient at zero; do not fabricate a column',
    thermal_constraint='each of six predicted rates within max(abs(center rate)-10 W,1 W); measured original limit max(abs(center rate),1 W) plus 1 W',
    thermal_headroom_w=10.,thermal_floor_w=1.,thermal_measurement_allowance_w=1.,
    capacity_constraint='each predicted and measured expansion-to-other gap retains at least half its center margin',
    secondary_objective_allowance=1e-9,numerical_cleanup_tolerance=1e-12,relative_prediction_tolerance=.1,
    capacity_relative_tolerance=.1,capacity_prediction_norm_floor_kg_s=1e-9,
    envelope_prediction_absolute_tolerance_in_range_units=.01,
    measured_minimum_margin_fraction=.5,original_maximum_must_decrease=True,
    neighbourhood_absolute_fraction_radius=.0002,binary64_domain_tolerance=1e-15,
    energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
    searches=0,production_changes=0,old_domains_unchanged=True,root_criterion_unchanged=True,
    seed_installation=False,r3_passed=False,m10_closed=False)

def envelope_vector(record):return [o[k] for o in record['Observations'] for k in ENVELOPE]

@lru_cache(maxsize=1)
def envelope_basis():
    a20,points20,_=R['lineage']();data21=S['frozen'](R['ID']);a21=json.loads(data21['04-analysis.json'])
    points21={r['Id'].rsplit('/repeat/',1)[0]:r for r in map(json.loads,data21['03-measurements.jsonl'].splitlines()) if r['Id'].endswith('/repeat/1')}
    selected20={d['direction']:d['selected'] for d in a20['directions'] if d['selected']}
    selected21={d['direction']:d['selected'] for d in a21['directions'] if d['selected']}
    widths=[hi-lo for _ in range(3) for lo,hi in ENVELOPE.values()];columns={};checks={}
    for label,d in sorted(a21['working_directions'].items()):
        source=points21 if label in selected21 else points20
        selected=(selected21 if label in selected21 else selected20)[label]
        high=d['sampled_amplitude'];low=selected['reference_amplitude'] if label in selected21 else .5*high
        stencil=selected.get('stencil','central');sign=selected.get('orientation',1);derivatives=[]
        for h in (low,high):
            tag='plus' if sign==1 else 'minus'
            keys=[f'{label}/plus/{h:g}',f'{label}/minus/{h:g}'] if stencil=='central' else [f'{label}/{tag}/{h:g}',f'{label}/{tag}/{2*h:g}']
            P.require(all(k in source for k in keys),'Missing selected operational-envelope stencil')
            derivatives.append(L['derivative'](envelope_vector(source['center']),*[envelope_vector(source[k]) for k in keys],h,stencil))
        error=L['deviation']([v/w for v,w in zip(derivatives[1],widths)],[v/w for v,w in zip(derivatives[0],widths)],1.)
        checks[label]=dict(relative_deviation=error,available=error<=.1,reference_amplitude=low,sampled_amplitude=high)
        if error<=.1:columns[label]=derivatives[1]
    return columns,checks

@lru_cache(maxsize=1)
def model():
    data=S['frozen'](R['ID']);basis=json.loads(data['04-analysis.json']);prior,points,_=R['lineage']()
    center=points['center'];historical=points['historical'];_,scales,names=S['layout']();weights=S['POLICY']['physical_weights']
    labels=sorted(basis['working_directions']);descriptions=[basis['working_directions'][k] for k in labels];h=[d['sampled_amplitude'] for d in descriptions]
    weighted=S['weighted_vector'](center,scales,names)
    factors=[weights[{'mass':'mass_kg_s','energy':'energy_w','torque':'torque_nm'}[name[2]]]/s for name,s in zip(names,scales)]
    wm=[[basis['working_weighted_81_columns'][k][i]*amp for k,amp in zip(labels,h)] for i in range(81)]
    original=[[v*factors[i] for v in row] for i,row in enumerate(wm[:75])]
    thermal=[v*weights['thermal_w'] for v in weighted[75:]];tm=[[v*weights['thermal_w'] for v in row] for row in wm[75:]]
    limits=[max(abs(v)-10.,1.) for v in thermal]
    cap0=L['capacity_vector'](prior['observations']['center']);cm=[[basis['working_capacity_columns'][k][i]*amp for k,amp in zip(labels,h)] for i in range(15)]
    env_columns,checks=envelope_basis();env0=envelope_vector(center)
    em=[[env_columns[k][i]*amp if k in env_columns else 0. for k,amp in zip(labels,h)] for i in range(12)]
    auxiliary=[]
    for row,value,bound in zip(tm,thermal,limits):
        auxiliary += [(row,bound-value),([-v for v in row],bound+value)]
    e=C['CAPACITIES'].index('expansion')
    for i in range(3):
        for j in range(5):
            if j!=e:
                a,b=5*i+e,5*i+j;gap=cap0[b]-cap0[a];P.require(gap>0,'Center lacks strict expansion margin')
                auxiliary.append(([u-v for u,v in zip(cm[a],cm[b])],.5*gap))
    env_bounds=[[lo+.1*(hi-lo),hi-.1*(hi-lo)] for _ in range(3) for lo,hi in ENVELOPE.values()]
    for row,value,(lo,hi) in zip(em,env0,env_bounds):auxiliary += [(row,hi-value),([-v for v in row],value-lo)]
    lower=[(0. if d['coefficient_sign']=='nonnegative' else -1.) if k in env_columns else 0. for k,d in zip(labels,descriptions)]
    upper=[1. if k in env_columns else 0. for k in labels]
    normalized,certificate=U['constrained_minimax'](original,center['Residuals'],lower,upper,auxiliary)
    coefficients=[v*amp for v,amp in zip(normalized,h)]
    multiply=lambda m:[math.fsum(v*c for v,c in zip(row,normalized)) for row in m]
    fluid=[math.fsum(v*d['coordinates'][i] for v,d in zip(coefficients,descriptions)) for i in range(24)]
    operating=[math.fsum(v*d['operating'][i] for v,d in zip(coefficients,descriptions)) for i in range(6)]
    P.require(all(abs(v)<=.0002+1e-15 for v in fluid+operating),'Aggregate outside diagnostic domain')
    delta=multiply(original);P.require(max(abs(a+b) for a,b in zip(center['Residuals'],delta))<max(map(abs,center['Residuals'])),'No predicted original-criterion reduction')
    return dict(labels=labels,normalized_coefficients=normalized,coefficients=coefficients,certificate=certificate,
        center=center,historical=historical,baseline_original_75=center['Residuals'],baseline_weighted_81=weighted,
        baseline_thermal_w=thermal,thermal_limits_w=[max(abs(v),1.) for v in thermal],predicted_thermal_limits_w=limits,
        baseline_capacities=cap0,baseline_envelope=env0,predicted_envelope_bounds=env_bounds,envelope_derivative_checks=checks,
        predicted_original_change=delta,predicted_weighted_change=multiply(wm),predicted_thermal_change_w=multiply(tm),
        predicted_capacity_change=multiply(cm),predicted_envelope_change=multiply(em),coordinates=fluid,operating=operating,
        auxiliary_slacks=[bound-math.fsum(v*x for v,x in zip(row,normalized)) for row,bound in auxiliary])

def predictions():
    m=model();return [{k:v for k,v in m.items() if k not in ('center','historical')},
        [dict(step_fraction=f,original_change=[f*v for v in m['predicted_original_change']],weighted_change=[f*v for v in m['predicted_weighted_change']],
            thermal_change_w=[f*v for v in m['predicted_thermal_change_w']],capacity_change=[f*v for v in m['predicted_capacity_change']],
            envelope_change=[f*v for v in m['predicted_envelope_change']]) for f in POLICY['step_fractions']]]

def requests():
    m=model();points=[dict(Id=k,Coordinates=m[k]['Coordinates'],OperatingPointFractions=m[k]['OperatingPointFractions']) for k in ('historical','center')]
    for f in POLICY['step_fractions']:
        points.append(dict(Id=f'proposal/{f:g}',Coordinates=[a+f*v for a,v in zip(m['center']['Coordinates'],m['coordinates'])],
            OperatingPointFractions=[a+f*v for a,v in zip(m['center']['OperatingPointFractions'],m['operating'])]))
    return [{**p,'Id':p['Id']+f'/repeat/{repeat}'} for p in points for repeat in (1,2)]

def fixtures():
    x,_=U['constrained_minimax']([[1.]],[1.],[-1.],[1.],[([-1.],.1)])
    P.require(abs(x[0]+.1)<2e-9,'Operational bound fixture')
    m=model();P.require(len(requests())==len({r['Id'] for r in requests()})==8,'Budget fixture')
    for value,change,(lo,hi) in zip(m['baseline_envelope'],m['predicted_envelope_change'],m['predicted_envelope_bounds']):
        P.require(lo-1e-8<=value+change<=hi+1e-8,'Buffered prediction fixture')
    return 'PASS-ENVELOPE-OBJECTIVE-24-FIXTURES'

def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for k in ('src','tests'):P.require(P.tree(k)==c['unchanged_baseline'][k],'Runtime/test tree drift')
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

def analyze(records):
    first=Q['audit_pairs'](records,requests());points={r['Id'].rsplit('/repeat/',1)[0]:r for r in first};m=model();_,scales,names=S['layout']()
    for k in ('historical','center'):P.require({p:v for p,v in points[k].items() if p!='Id'}=={p:v for p,v in m[k].items() if p!='Id'},'Parent full-record drift')
    observed=[];base_max=max(map(abs,m['baseline_original_75']))
    widths=[hi-lo for _ in range(3) for lo,hi in ENVELOPE.values()]
    for p in predictions()[1]:
        f=p['step_fraction'];r=points[f'proposal/{f:g}'];guards=D['reasons'](r,points['center'])
        result=dict(step_fraction=f,guards=guards,prediction_conforming=False,reduces_original_maximum=False,eligible_for_later_review=False)
        if r['Residuals'] is not None:
            delta=[a-b for a,b in zip(r['Residuals'],m['baseline_original_75'])];wv=S['weighted_vector'](r,scales,names)
            wd=[a-b for a,b in zip(wv,m['baseline_weighted_81'])];thermal=[v*S['POLICY']['physical_weights']['thermal_w'] for v in wv[75:]]
            error=L['deviation'](delta,p['original_change']);we=L['deviation'](wd,p['weighted_change'])
            env=envelope_vector(r);env_error=max(abs(actual-base-change)/width for actual,base,change,width in zip(env,m['baseline_envelope'],p['envelope_change'],widths))
            maximum=max(map(abs,r['Residuals']));thermal_ok=all(abs(v)<=b+1. for v,b in zip(thermal,m['thermal_limits_w']))
            result.update(relative_prediction_error_original_75=error,relative_prediction_error_weighted_81=we,
                envelope_prediction_error_in_range_units=env_error,observed_envelope=env,
                observed_maximum_original=maximum,predicted_maximum_original=max(abs(a+b) for a,b in zip(m['baseline_original_75'],p['original_change'])),
                observed_original_change=delta,observed_thermal_w=thermal,thermal_constraints_conforming=thermal_ok,
                reduces_original_maximum=maximum<base_max,physical_residuals=U['physical_residuals'](r),cycle_powers=[o['CyclePower'] for o in r['Observations']])
            try:
                cap=L['capacity_vector'](C['reconstruct'](r));dc=[a-b for a,b in zip(cap,m['baseline_capacities'])]
                ce=L['deviation'](dc,p['capacity_change'],1e-9);e=C['CAPACITIES'].index('expansion')
                winners=[C['CAPACITIES'][min(range(5),key=lambda j:cap[5*i+j])] for i in range(3)]
                retained=min((cap[5*i+j]-cap[5*i+e])/(m['baseline_capacities'][5*i+j]-m['baseline_capacities'][5*i+e]) for i in range(3) for j in range(5) if j!=e)
                result.update(relative_capacity_prediction_error=ce,observed_winners=winners,minimum_retained_capacity_margin_fraction=retained)
                result['prediction_conforming']=guards['available'] and error<=.1 and we<=.1 and ce<=.1 and env_error<=.01 and winners==['expansion']*3 and retained>=.5
                result['eligible_for_later_review']=result['prediction_conforming'] and thermal_ok and maximum<base_max
            except Exception as failure:result['capacity_failure']=str(failure)
        observed.append(result)
    return dict(status='PASS-REPAIRED-ENVELOPE-OBJECTIVE-24-EVIDENCE',fixtures=fixtures(),recipes=8,maximum_runtime_steps=26,
        exact_repeats=True,parent_full_records_identical=True,model=predictions()[0],baseline_maximum_original=base_max,proposals=observed,
        both_predictions_conforming=all(p['prediction_conforming'] for p in observed),both_proposals_eligible_for_later_review=all(p['eligible_for_later_review'] for p in observed),
        maximum_global_energy_ledger_error_w=max(abs(e) for r in records if r['Residuals'] is not None for e in Q['audit_power'](r)),
        integrated_root_observed=any(r['Residuals'] is not None and max(map(abs,r['Residuals']))<=1 for r in first),
        integrated_search_started=False,seed_installed=False,r3_passed=False,m10_closed=False,
        limits=['Operational derivatives checked on existing selected stencils; only two composed steps newly measured',
            'Thermal headroom and buffered envelope constraints do not change production, original scales or acceptance thresholds',
            'No nonlinear search update, seed installation, hundred-step confirmation or R3 qualification'])

def execute():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing execution retained')
    P.require(P.load(OUT/'01-predictions.json')==predictions(),'Prospective prediction drift')
    with (OUT/'execution.log').open('x',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'1'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    (OUT/'03-measurements.jsonl').write_bytes((OUT/'batch-001/03-measurements.jsonl').read_bytes());return adjudicate()

def adjudicate():
    authenticate();P.require(P.load(OUT/'01-requests.json')==requests() and P.load(OUT/'01-predictions.json')==predictions(),'Prospective input drift')
    result=analyze([json.loads(s) for s in (OUT/'03-measurements.jsonl').read_text(encoding='utf-8').splitlines()]);P.write(OUT,'04-analysis.json',result);return result['status']

def freeze():
    adjudicate();P.freeze(ID,['00-domain-fixtures.log','01-predictions.json','01-requests.json','02-preexecution.json','03-measurements.jsonl','04-analysis.json','execution.log'])
    return 'PASS-FROZEN-ENVELOPE-OBJECTIVE-24'

def review():
    authenticate();P.require(P.load(ROOT/'eng'/(ID+'-returned.json'))['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);P.require(json.loads(data['01-requests.json'])==requests() and json.loads(data['01-predictions.json'])==predictions(),'Frozen prospective input drift')
    result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()]);P.require(result==json.loads(data['04-analysis.json']),'Frozen analysis drift');return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
