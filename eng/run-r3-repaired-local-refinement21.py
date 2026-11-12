"""Smaller, same-branch stencils for the eight directions unavailable in local observation 20."""
from __future__ import annotations
from functools import lru_cache
import json
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
L=runpy.run_path(str(ROOT/'eng/run-r3-repaired-local-basis20.py'))
Q,S,D,C,B=L['Q'],L['S'],L['D'],L['C'],L['B']
ID='r3-repaired-local-refinement21';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT,BINARY=L['PROJECT'],L['BINARY']
POLICY=dict(maximum_recipes=196,maximum_batches=1,maximum_batch_recipes=196,maximum_runtime_steps=590,repeats=2,
            direction_source='the eight unselected directions in local-basis20',amplitude_factors=[.001,.003,.01,.03,.1,.3],
            selection='largest adjacent central pair with weighted81 and capacity deviations <=10%, original guards and center capacity winner at all checkpoints',
            relative_linearity_tolerance=.1,capacity_derivative_norm_floor=1e-12,normalized_rank_threshold=1e-8,
            neighbourhood_absolute_fraction_radius=.0002,binary64_domain_tolerance=1e-15,
            energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
            physical_weights=S['POLICY']['physical_weights'],proposals=0,searches=0,production_changes=0,
            old_domains_unchanged=True,root_criterion_unchanged=True,seed_installation=False,r3_passed=False,m10_closed=False)

@lru_cache(maxsize=1)
def lineage():
    data=S['frozen'](L['ID']);a=json.loads(data['04-analysis.json'])
    points={r['Id'].rsplit('/repeat/',1)[0]:r for r in map(json.loads,data['03-measurements.jsonl'].splitlines()) if r['Id'].endswith('/repeat/1')}
    descriptions=L['lineage']()[2]
    directions={k:descriptions[k] for k in a['summary']['unavailable_directions']}
    P.require(len(directions)==8 and all(d['coefficient_sign']=='both' for d in directions.values()),'Refinement scope drift')
    return a,points,directions

def requests():
    _,old,directions=lineage();center=old['center']
    points=[dict(Id=k,Coordinates=old[k]['Coordinates'],OperatingPointFractions=old[k]['OperatingPointFractions']) for k in ('historical','center')]
    for label,d in sorted(directions.items()):
        for factor in POLICY['amplitude_factors']:
            h=factor*d['sampled_amplitude']
            for sign,tag in ((1,'plus'),(-1,'minus')):
                points.append(dict(Id=f'{label}/{tag}/{h:g}',Coordinates=[a+sign*h*v for a,v in zip(center['Coordinates'],d['coordinates'])],
                    OperatingPointFractions=[a+sign*h*v for a,v in zip(center['OperatingPointFractions'],d['operating'])]))
    return [{**p,'Id':p['Id']+f'/repeat/{repeat}'} for p in points for repeat in (1,2)]

def select(candidates):
    good=[c for c in candidates if c['available'] and c['relative_deviation_weighted_81']<=.1 and c['relative_deviation_capacities']<=.1]
    return max(good,key=lambda c:c['sampled_amplitude']) if good else None

def fixtures():
    req=requests();center=lineage()[1]['center']
    P.require(len(req)==196 and len({r['Id'] for r in req})==196 and 2+3*196==POLICY['maximum_runtime_steps'],'Budget fixture')
    for r in req[2:]:P.require(all(abs(a-b)<=.0002+1e-15 for k in ('Coordinates','OperatingPointFractions') for a,b in zip(r[k],center[k])),'Domain fixture')
    good=dict(available=True,relative_deviation_weighted_81=.01,relative_deviation_capacities=.01,sampled_amplitude=1.)
    P.require(select([good,{**good,'sampled_amplitude':2.}])['sampled_amplitude']==2.
        and select([{**good,'available':False},{**good,'relative_deviation_capacities':.11}]) is None,'Selection/rejection fixture')
    return 'PASS-LOCAL-REFINEMENT-21-FIXTURES'

def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for e in c['implementation_files']+c['frozen_inputs']:P.require(P.digest((ROOT/e['path']).read_bytes())==e['sha256'],'Input drift: '+e['path'])

def preexecution():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists() and not (OUT/'batch-001').exists(),'Existing execution retained')
    P.require(BINARY.exists(),'Build the local-basis console first')
    with (OUT/'00-domain-fixtures.log').open('w',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),'--self-test'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    token=dict(status='PASS-PREEXECUTION',contract_sha256=P.digest(CONTRACT.read_bytes()),fixtures=fixtures(),runtime_steps=0,recipes=len(requests()),
        assemblies=[dict(file=x.name,sha256=P.digest(x.read_bytes())) for x in sorted(BINARY.parent.glob('*.dll'))])
    for folder in (OUT,OUT/'batch-001'):
        P.write(folder,'01-requests.json',requests());P.write(folder,'02-preexecution.json',dict(**token,requests_sha256=P.digest((folder/'01-requests.json').read_bytes())))
    with (OUT/'preflight.log').open('w',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'1','--validate-only'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    return 'PASS-PREEXECUTION'

def analyze(records):
    first=Q['audit_pairs'](records,requests());points={r['Id'].rsplit('/repeat/',1)[0]:r for r in first}
    prior,old,directions=lineage();center=points['center'];_,scales,names=S['layout']()
    for k in ('historical','center'):
        P.require({p:v for p,v in points[k].items() if p!='Id'}=={p:v for p,v in old[k].items() if p!='Id'},'Parent full-record drift')
    guards={k:D['reasons'](r,center) for k,r in points.items()};observed={};failures={}
    for k,r in points.items():
        if r['Residuals'] is None:failures[k]=r['Reason'];continue
        try:observed[k]=C['reconstruct'](r)
        except Exception as e:failures[k]=str(e)
    winners=lambda k:[r['winner'] for r in observed[k]]
    weighted={k:S['weighted_vector'](r,scales,names) for k,r in points.items() if r['Residuals'] is not None}
    caps={k:L['capacity_vector'](v) for k,v in observed.items()}
    summaries=[];columns=dict(prior['working_weighted_81_columns']);cap_columns=dict(prior['working_capacity_columns']);descriptions=dict(prior['working_directions'])
    for label,d in sorted(directions.items()):
        sampled=[];derivatives={};cap_derivatives={}
        for factor in POLICY['amplitude_factors']:
            h=factor*d['sampled_amplitude'];ids=[f'{label}/{tag}/{h:g}' for tag in ('plus','minus')]
            available=all(guards[k]['available'] and k in observed and winners(k)==winners('center') for k in ids)
            sampled.append(dict(amplitude=h,records=ids,available=available,winners={k:winners(k) for k in ids if k in observed}))
            if available:
                derivatives[h]=L['derivative'](weighted['center'],*[weighted[k] for k in ids],h,'central')
                cap_derivatives[h]=L['derivative'](caps['center'],*[caps[k] for k in ids],h,'central')
        pairs=[]
        for low,high in zip(sampled,sampled[1:]):
            lo,hi=low['amplitude'],high['amplitude'];valid=low['available'] and high['available']
            pairs.append(dict(available=valid,reference_amplitude=lo,sampled_amplitude=hi,
                relative_deviation_weighted_81=L['deviation'](derivatives[hi],derivatives[lo]) if valid else None,
                relative_deviation_capacities=L['deviation'](cap_derivatives[hi],cap_derivatives[lo],1e-12) if valid else None))
        chosen=select(pairs)
        if chosen:
            h=chosen['sampled_amplitude'];columns[label]=derivatives[h];cap_columns[label]=cap_derivatives[h]
            descriptions[label]={**d,'sampled_amplitude':h}
        summaries.append(dict(direction=label,samples=sampled,adjacent_pairs=pairs,selected=chosen))
    rows=[r for series in observed.values() for r in series]
    return dict(status='PASS-REPAIRED-LOCAL-REFINEMENT-21-EVIDENCE',fixtures=fixtures(),recipes=len(records),maximum_runtime_steps=590,
        exact_repeats=True,parent_full_records_identical=True,guards=guards,observations=observed,capacity_failures=failures,directions=summaries,
        working_directions=descriptions,working_weighted_81_columns=columns,working_capacity_columns=cap_columns,
        selected_direction_count=len(columns),rank_diagnostic=B['rank_diagnostic'](columns,1e-8),
        summary=dict(recovered_directions=[d['direction'] for d in summaries if d['selected']],unavailable_directions=[d['direction'] for d in summaries if not d['selected']],
            available_points=sum(g['available'] for g in guards.values()),points=len(points),
            exact_minimum_reconstructions=sum(r['exact_minimum'] for r in rows),capacity_reconstructions=len(rows),
            maximum_flow_error_kg_s=max(abs(r['flow_error_kg_s']) for r in rows),center_winners=winners('center'),
            central_columns=sum(d['coefficient_sign']=='both' for d in descriptions.values()),one_sided_columns=sum(d['coefficient_sign']=='nonnegative' for d in descriptions.values()),
            maximum_global_energy_ledger_error_w=max(abs(e) for r in records if r['Residuals'] is not None for e in Q['audit_power'](r))),
        proposal_executed=False,integrated_search_started=False,seed_installed=False,r3_passed=False,m10_closed=False,
        limits=['New columns are confined to the center expansion branch at the sampled scales',
                'Inherited columns retain their observation-20 scope and orientation',
                'A complete directional basis or diagnostic rank is not a validated composed prediction or integrated root'])

def execute():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing execution retained')
    with (OUT/'execution.log').open('x',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'1'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    (OUT/'03-measurements.jsonl').write_bytes((OUT/'batch-001/03-measurements.jsonl').read_bytes())
    return adjudicate()

def adjudicate():
    authenticate();P.require(P.load(OUT/'01-requests.json')==requests(),'Request drift')
    result=analyze([json.loads(s) for s in (OUT/'03-measurements.jsonl').read_text(encoding='utf-8').splitlines()]);P.write(OUT,'04-analysis.json',result);return result['status']

def freeze():
    adjudicate();P.freeze(ID,['00-domain-fixtures.log','01-requests.json','02-preexecution.json','03-measurements.jsonl','04-analysis.json','execution.log'])
    return 'PASS-FROZEN-LOCAL-REFINEMENT-21'

def review():
    authenticate();P.require(P.load(ROOT/'eng'/(ID+'-returned.json'))['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);P.require(json.loads(data['01-requests.json'])==requests(),'Frozen requests drift')
    result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()]);P.require(result==json.loads(data['04-analysis.json']),'Frozen analysis drift')
    return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
