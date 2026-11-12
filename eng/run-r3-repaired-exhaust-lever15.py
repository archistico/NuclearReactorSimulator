"""Measure the exhaust-energy lever at percent scale, inside the inherited 2% limit."""
from __future__ import annotations
from functools import lru_cache
import json
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
L=runpy.run_path(str(ROOT/'eng/review-r3-repaired-step-limit14.py'))
C=L['R']['C'];S=C['S'];D=C['D'];T=C['T']
ID='r3-repaired-exhaust-lever15';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT,BINARY=C['PROJECT'],C['BINARY']
POLICY=dict(recipes=22,repeats=2,maximum_runtime_steps=68,amplitudes=[1e-4,1e-3,5e-3,1e-2,2e-2],
            coordinate='exhaust:energy',coordinate_index=5,inherited_coordinate_limit=.02,
            response_tolerance_relative=.1,reference='first-step flow-free combination and slope frozen in review 14',
            energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
            phase_or_regime_change='recorded, never repaired or silently omitted',
            proposals=0,searches=0,production_changes=0,root_criterion_unchanged=True,previous_results_rewritten=False,r3_passed=False,m10_closed=False)

def requests():
    points=[dict(Id='base',Coordinates=[0.]*24,OperatingPointFractions=[0.]*6)]
    for h in POLICY['amplitudes']:
        for sign,tag in ((1,'plus'),(-1,'minus')):
            coordinates=[0.]*24;coordinates[5]=sign*h
            points.append(dict(Id=f'exhaust:energy/{tag}/{h:g}',Coordinates=coordinates,OperatingPointFractions=[0.]*6))
    return [{**p,'Id':p['Id']+f'/repeat/{repeat}'} for p in points for repeat in (1,2)]

@lru_cache(maxsize=1)
def reference():
    data=S['frozen'](L['ID']);analysis=json.loads(data['01-analysis.json'])
    old=S['frozen'](C['ID']);records={r['Id']:r for r in map(json.loads,old['03-measurements.jsonl'].splitlines())}
    return analysis,records

def classify(guards,relative_error):
    if guards.get('phase_changes'):return 'PHASE-CHANGE'
    if not guards.get('healthy',False):return 'HEALTH-OR-DOMAIN-REJECTION'
    if not guards.get('in_envelope',False):return 'OUTSIDE-ENVELOPE'
    if not guards['available']:return 'REGIME-CHANGE'
    return 'WITHIN-REFERENCE-TOLERANCE' if relative_error<=POLICY['response_tolerance_relative'] else 'BENT-RESPONSE'

def fixtures():
    P.require(len(requests())==POLICY['recipes']==len({r['Id'] for r in requests()}),'Budget fixture')
    P.require(POLICY['maximum_runtime_steps']==2+3*len(requests()),'Step budget fixture')
    P.require(max(abs(x) for r in requests() for x in r['Coordinates'])==.02,'Inherited-bound fixture')
    guards=dict(phase_changes=[],healthy=True,in_envelope=True,available=True)
    P.require(classify(guards,.05)=='WITHIN-REFERENCE-TOLERANCE' and classify(guards,.2)=='BENT-RESPONSE','Response fixture')
    P.require(classify({**guards,'phase_changes':[1]},0)=='PHASE-CHANGE','Phase precedence fixture')
    return 'PASS-EXHAUST-LEVER-15-FIXTURES'

def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for entry in c['implementation_files']+c['frozen_inputs']:
        P.require(P.digest((ROOT/entry['path']).read_bytes())==entry['sha256'],'Input drift: '+entry['path'])

def preexecution():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing acquisition retained')
    P.require(BINARY.exists() and (OUT/'00-build.log').exists(),'Build capacity console first')
    P.write(OUT,'01-requests.json',requests())
    P.write(OUT,'02-preexecution.json',dict(status='PASS-PREEXECUTION',contract_sha256=P.digest(CONTRACT.read_bytes()),
            requests_sha256=P.digest((OUT/'01-requests.json').read_bytes()),fixtures=fixtures(),runtime_steps=0,
            assemblies=[dict(file=p.name,sha256=P.digest(p.read_bytes())) for p in sorted(BINARY.parent.glob('*.dll'))]))
    subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'--validate-only'],cwd=ROOT,check=True)
    return 'PASS-PREEXECUTION'

def physical_rows(record):
    previous={n['Id']:n for n in record['Raw']};rows=[]
    for o in record['Observations']:
        now={n['Id']:n for n in o['Nodes']};c=o['Extra']['Condenser'];stage=o['Extra']['Stage']
        rows.append(dict(checkpoint=o['Checkpoint'],exhaust_energy_w=(now['exhaust']['Energy']-previous['exhaust']['Energy'])/.01,
                         exhaust_mass_kg_s=(now['exhaust']['Mass']-previous['exhaust']['Mass'])/.01,rotor_torque_nm=o['RotorTorque'],
                         condenser=c,stage_vapor_kg_s=stage['VaporMassKgS'],pressure_pa=previous['exhaust']['Pressure'],
                         temperature_k=previous['exhaust']['Temperature'],quality=previous['exhaust']['Quality']))
        previous=now
    return rows

def analyze(records):
    expected=requests();P.require(len(records)==len(expected)==22,'Recipe budget drift')
    plan,_,_=S['layout']();points={};energy_errors=[];torque_errors=[];parent,old=reference();overlaps=[]
    for i in range(0,len(records),2):
        pair=records[i:i+2]
        for r,request in zip(pair,expected[i:i+2]):
            P.require(all(r[k]==request[k] for k in request),'Request/response drift')
            P.H3['H2']['verify_physics'](r,plan)
            if r['Residuals'] is not None:
                energy_errors+=S['verify_extra'](r);torque_errors+=T['audit_mechanical'](r)
            if r['Id'] in old:P.require(r==old[r['Id']],'Overlapping capacity observation identity drift');overlaps.append(r['Id'])
        P.require({k:v for k,v in pair[0].items() if k!='Id'}=={k:v for k,v in pair[1].items() if k!='Id'},'Repeatability drift')
        points[pair[0]['Id'].rsplit('/repeat/',1)[0]]=pair[0]
    base=points['base'];P.require(base['Reason']=='VALID','Baseline invalid')
    base_rows=physical_rows(base);r=parent['invariant']['ratio_per_nm'];slope=parent['invariant']['lever_response_per_unit']
    phi=lambda row:row['exhaust_energy_w']/1e6-r*row['rotor_torque_nm']
    baseline=phi(base_rows[0]);S['close'](baseline,parent['invariant']['baseline_value'],1e-12,'Invariant reference drift')
    results=[]
    for key,record in points.items():
        if key=='base':continue
        coordinate=record['Coordinates'][5];guards=D['reasons'](record,base);item=dict(record=key,coordinate=coordinate,guards=guards)
        if record['Residuals'] is not None:
            rows=physical_rows(record);combination=phi(rows[0]);secant=(combination-baseline)/coordinate
            error=abs(secant/slope-1.)
            try:capacities=C['reconstruct'](record);failure=None
            except Exception as exc:capacities=None;failure=str(exc)
            item.update(classification=classify(guards,error),physical=rows,capacities=capacities,reconstruction_failure=failure,
                        first_combination_mw=combination,combination_secant_per_fraction=secant,relative_secant_deviation=error,
                        energy_secant_mw_per_fraction=(rows[0]['exhaust_energy_w']-base_rows[0]['exhaust_energy_w'])/1e6/coordinate,
                        linear_reference_prediction_mw=baseline+slope*coordinate,
                        inferred_cancel_coordinate=-baseline/secant if secant else None)
        else:item.update(classification='HEALTH-OR-DOMAIN-REJECTION',physical=None)
        results.append(item)
    admitted=[x for x in results if x['guards']['available'] and x['physical'] is not None]
    endpoint=next(x for x in results if x['coordinate']==.02)
    return dict(status='PASS-REPAIRED-EXHAUST-LEVER-15-EVIDENCE',fixtures=fixtures(),recipes=22,maximum_runtime_steps=68,
                exact_repeats=True,parent_records_reproduced=overlaps,baseline_physical=base_rows,first_step_ratio_per_nm=r,
                baseline_first_combination_mw=baseline,reference_lever_response_per_fraction=slope,observations=results,
                valid_recipes=sum(r['Reason']=='VALID' for r in records),
                in_envelope_recipes=sum(r['Residuals'] is not None and all(o['InEnvelope'] for o in r['Observations']) for r in records),
                maximum_energy_ledger_error_w=max(map(abs,energy_errors)),maximum_torque_ledger_error_nm=max(map(abs,torque_errors)),
                summary=dict(admitted_probe_points=len(admitted),all_admitted_within_reference_tolerance=all(x['classification']=='WITHIN-REFERENCE-TOLERANCE' for x in admitted),
                             positive_endpoint_admitted=endpoint['guards']['available'],positive_endpoint_first_combination_mw=endpoint.get('first_combination_mw'),
                             positive_endpoint_sign_crossed=(endpoint['first_combination_mw']*baseline<=0) if endpoint.get('physical') else None,
                             cancel_coordinate_secant_range=[min(x['inferred_cancel_coordinate'] for x in admitted),max(x['inferred_cancel_coordinate'] for x in admitted)] if admitted else None),
                family_selected=False,proposal_executed=False,search_started=False,root_observed=False,r3_passed=False,m10_closed=False,
                limits=['Only exhaust internal energy is perturbed; no general infeasibility claim for the complete family',
                        'The flow-free combination and frozen cancellation ratio concern the first checkpoint only',
                        'Secant cancellation coordinates beyond 2% are extrapolations, not observed roots or an enlarged search bound'])

def adjudicate():
    authenticate();result=analyze([json.loads(s) for s in (OUT/'03-measurements.jsonl').read_text(encoding='utf-8').splitlines()])
    P.write(OUT,'04-analysis.json',result);return result['status']

def execute():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing acquisition retained')
    with (OUT/'execution.log').open('x',encoding='utf-8') as stream:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID],cwd=ROOT,stdout=stream,stderr=subprocess.STDOUT,check=True)
    return adjudicate()

def freeze():
    adjudicate();P.freeze(ID,['00-build.log','01-requests.json','02-preexecution.json','03-measurements.jsonl','04-analysis.json','execution.log'])
    return 'PASS-FROZEN-EXHAUST-LEVER-15'

def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()])
    P.require(json.loads(json.dumps(result))==json.loads(data['04-analysis.json']),'Replay drift')
    return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
