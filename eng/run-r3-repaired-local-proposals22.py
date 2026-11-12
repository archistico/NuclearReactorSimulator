"""Prospective composed predictions on the remeasured diagnostic point-19 basis."""
from __future__ import annotations
from functools import lru_cache
import json
import math
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
R=runpy.run_path(str(ROOT/'eng/run-r3-repaired-local-refinement21.py'))
L,Q,S,D,C=R['L'],R['Q'],R['S'],R['D'],R['C']
F=L['F']
ID='r3-repaired-local-proposals22';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT,BINARY=L['PROJECT'],L['BINARY']
POLICY=dict(maximum_recipes=8,maximum_batches=1,maximum_batch_recipes=8,maximum_runtime_steps=26,repeats=2,
            step_fractions=[.25,.5],objective='max-absolute-weighted-81-residual; historical certified bounded LP and secondary minimum normalized L1 step',
            box='remeasured sampled amplitudes; two one-sided coefficients nonnegative',
            model='affine directional response about point19; capacities predicted separately, without inheriting a minimum model from the old point',
            branch_fraction_cap='all predicted expansion-to-other capacity gaps retain at least half their center margin; cap full step at 1',
            relative_prediction_tolerance=.1,capacity_relative_tolerance=.1,capacity_prediction_norm_floor_kg_s=1e-9,
            winner_agreement_required=True,center_expansion_branch_required=True,
            neighbourhood_absolute_fraction_radius=.0002,binary64_domain_tolerance=1e-15,
            energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
            physical_weights=S['POLICY']['physical_weights'],searches=0,production_changes=0,root_criterion_unchanged=True,
            old_domains_unchanged=True,seed_installation=False,r3_passed=False,m10_closed=False)

@lru_cache(maxsize=1)
def model():
    data=S['frozen'](R['ID']);a=json.loads(data['04-analysis.json']);prior,points,_=R['lineage']();center=points['center'];historical=points['historical']
    P.require(a['selected_direction_count']==a['rank_diagnostic']['rank']==30,'Remeasured basis incomplete')
    _,scales,names=S['layout']();base=S['weighted_vector'](center,scales,names)
    labels=sorted(a['working_directions']);descriptions=[a['working_directions'][k] for k in labels];h=[d['sampled_amplitude'] for d in descriptions]
    matrix=[[a['working_weighted_81_columns'][k][i]*amp for k,amp in zip(labels,h)] for i in range(81)]
    lower=[0. if d['coefficient_sign']=='nonnegative' else -1. for d in descriptions]
    normalized,certificate=F['solve_box'](matrix,base,lower,[1.]*30)
    coeff=[v*amp for v,amp in zip(normalized,h)]
    dy=[math.fsum(row[j]*normalized[j] for j in range(30)) for row in matrix]
    cap0=L['capacity_vector'](prior['observations']['center'])
    dc=[math.fsum(a['working_capacity_columns'][k][i]*v for k,v in zip(labels,coeff)) for i in range(15)]
    branchcap=1.;e=C['CAPACITIES'].index('expansion')
    for checkpoint in range(3):
        for k in range(5):
            if k==e:continue
            gap=cap0[5*checkpoint+k]-cap0[5*checkpoint+e]
            change=dc[5*checkpoint+k]-dc[5*checkpoint+e]
            P.require(gap>0.,'Center expansion winner lacks strict margin')
            if change<0:branchcap=min(branchcap,.5*gap/-change)
    P.require(branchcap>0 and S['norm'](dy)>0,'No nonzero bounded prediction')
    fluid=[math.fsum(v*d['coordinates'][i] for v,d in zip(coeff,descriptions)) for i in range(24)]
    operating=[math.fsum(v*d['operating'][i] for v,d in zip(coeff,descriptions)) for i in range(6)]
    P.require(all(abs(x)<=.0002+1e-15 for x in fluid+operating),'Composed proposal outside local diagnostic domain')
    return dict(labels=labels,coefficients=coeff,normalized_coefficients=normalized,certificate=certificate,
                center=center,historical=historical,baseline_weighted_81=base,predicted_change_weighted_81=dy,
                baseline_capacities=cap0,predicted_capacity_change=dc,branch_fraction_cap=branchcap,
                coordinates=fluid,operating=operating)

def predictions():
    m=model()
    return [{k:v for k,v in m.items() if k not in ('center','historical')},
            [dict(step_fraction=f*m['branch_fraction_cap'],requested_fraction=f,
                  predicted_change_weighted_81=[f*m['branch_fraction_cap']*v for v in m['predicted_change_weighted_81']],
                  predicted_capacity_change=[f*m['branch_fraction_cap']*v for v in m['predicted_capacity_change']]) for f in POLICY['step_fractions']]]

def requests():
    m=model();points=[dict(Id=k,Coordinates=m[k]['Coordinates'],OperatingPointFractions=m[k]['OperatingPointFractions']) for k in ('historical','center')]
    for p in predictions()[1]:
        f=p['step_fraction'];points.append(dict(Id=f"proposal/{p['requested_fraction']:g}",
            Coordinates=[a+f*v for a,v in zip(m['center']['Coordinates'],m['coordinates'])],
            OperatingPointFractions=[a+f*v for a,v in zip(m['center']['OperatingPointFractions'],m['operating'])]))
    return [{**p,'Id':p['Id']+f'/repeat/{repeat}'} for p in points for repeat in (1,2)]

def fixtures():
    req=requests();m=model()
    P.require(len(req)==8 and len({r['Id'] for r in req})==8 and 2+3*8==26,'Budget fixture')
    P.require(all(v>=-1e-12 for k,v in zip(m['labels'],m['normalized_coefficients']) if R['lineage']()[0]['working_directions'].get(k,{}).get('coefficient_sign')=='nonnegative'),'One-sided coefficient fixture')
    for p in predictions()[1]:
        cap=[a+b for a,b in zip(m['baseline_capacities'],p['predicted_capacity_change'])]
        P.require(all(C['CAPACITIES'][min(range(5),key=lambda j:cap[5*i+j])]=='expansion' for i in range(3)),'Predicted branch fixture')
    return 'PASS-LOCAL-PROPOSALS-22-FIXTURES'

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
    token=dict(status='PASS-PREEXECUTION',contract_sha256=P.digest(CONTRACT.read_bytes()),fixtures=fixtures(),runtime_steps=0,recipes=len(requests()),
        predictions_sha256=P.digest((OUT/'01-predictions.json').read_bytes()),
        assemblies=[dict(file=x.name,sha256=P.digest(x.read_bytes())) for x in sorted(BINARY.parent.glob('*.dll'))])
    for folder in (OUT,OUT/'batch-001'):
        P.write(folder,'01-requests.json',requests());P.write(folder,'02-preexecution.json',dict(**token,requests_sha256=P.digest((folder/'01-requests.json').read_bytes())))
    with (OUT/'preflight.log').open('w',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'1','--validate-only'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    return 'PASS-PREEXECUTION'

def analyze(records):
    first=Q['audit_pairs'](records,requests());points={r['Id'].rsplit('/repeat/',1)[0]:r for r in first};m=model();_,scales,names=S['layout']()
    for k in ('historical','center'):
        P.require({p:v for p,v in points[k].items() if p!='Id'}=={p:v for p,v in m[k].items() if p!='Id'},'Parent full-record drift')
    observed=[]
    for p in predictions()[1]:
        label=f"proposal/{p['requested_fraction']:g}";r=points[label];guards=D['reasons'](r,points['center'])
        result=dict(record=label,step_fraction=p['step_fraction'],requested_fraction=p['requested_fraction'],guards=guards,prediction_conforming=False)
        if r['Residuals'] is not None:
            actual=[a-b for a,b in zip(S['weighted_vector'](r,scales,names),m['baseline_weighted_81'])]
            error=L['deviation'](actual,p['predicted_change_weighted_81']);result.update(relative_prediction_error_weighted_81=error,
                maximum_original_scaled_residual=max(map(abs,r['Residuals'])),maximum_weighted_81=max(map(abs,S['weighted_vector'](r,scales,names))),
                predicted_change_weighted_81=p['predicted_change_weighted_81'],observed_change_weighted_81=actual)
            try:
                capacities=C['reconstruct'](r);capdelta=[a-b for a,b in zip(L['capacity_vector'](capacities),m['baseline_capacities'])]
                caperror=L['deviation'](capdelta,p['predicted_capacity_change'],1e-9);winners=[v['winner'] for v in capacities]
                result.update(relative_capacity_prediction_error=caperror,observed_winners=winners,predicted_winners=['expansion']*3,
                    observed_capacity_change=capdelta,predicted_capacity_change=p['predicted_capacity_change'],
                    cycle_powers=[o['CyclePower'] for o in r['Observations']],exhaust_rows=Q['V']['physical_rows'](r))
                result['prediction_conforming']=guards['available'] and error<=.1 and caperror<=.1 and winners==['expansion']*3
            except Exception as e:result['capacity_failure']=str(e)
        observed.append(result)
    return dict(status='PASS-REPAIRED-LOCAL-PROPOSALS-22-EVIDENCE',fixtures=fixtures(),recipes=8,maximum_runtime_steps=26,
        exact_repeats=True,parent_full_records_identical=True,model=predictions()[0],proposals=observed,
        both_predictions_conforming=all(r['prediction_conforming'] for r in observed),
        maximum_global_energy_ledger_error_w=max(abs(e) for r in records if r['Residuals'] is not None for e in Q['audit_power'](r)),
        integrated_root_observed=any(r['Residuals'] is not None and max(map(abs,r['Residuals']))<=1 for r in first),
        integrated_search_started=False,seed_installed=False,r3_passed=False,m10_closed=False,
        limits=['Two fractional composed steps only; no nonlinear search or remeasurement after an accepted step',
                'A conforming change prediction does not establish full stationarity, a new search domain or a root',
                'No hundred-step confirmation or integrated R3 qualification'])

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
    return 'PASS-FROZEN-LOCAL-PROPOSALS-22'

def review():
    authenticate();P.require(P.load(ROOT/'eng'/(ID+'-returned.json'))['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);P.require(json.loads(data['01-requests.json'])==requests() and json.loads(data['01-predictions.json'])==predictions(),'Frozen prospective input drift')
    result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()]);P.require(result==json.loads(data['04-analysis.json']),'Frozen analysis drift')
    return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
