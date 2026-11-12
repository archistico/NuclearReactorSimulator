"""Remeasure the historical working directions about diagnostic point 19, including capacities."""
from __future__ import annotations
from functools import lru_cache
import json
import math
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
N=runpy.run_path(str(ROOT/'eng/run-r3-repaired-cycle-power19.py'))
Q,S,D,C=N['Q'],N['S'],N['D'],N['C']
F=C['F'];B=D['B']
ID='r3-repaired-local-basis20';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT='eng/diagnostics/R3LocalBasisObservation/R3LocalBasisObservation.csproj'
BINARY=ROOT/'eng/diagnostics/R3LocalBasisObservation/bin/Release/net10.0/R3LocalBasisObservation.dll'
POLICY=dict(maximum_recipes=260,maximum_batches=1,maximum_batch_recipes=260,maximum_runtime_steps=782,repeats=2,
            center='source/1/repeat/1 of frozen cycle-power19',direction_source='working_directions of frozen steam-directions5; vectors only, no inherited derivatives',
            central_amplitude_factors=[.5,1.],historical_one_sided_amplitude_factors=[.5,1.,2.],both_signs_measured=True,
            selection='central at half/full historical amplitude if admissible and conforming; otherwise second-order one-sided half/full for historical one-sided directions, positive wins ties',
            relative_linearity_tolerance=.1,capacity_derivative_norm_floor=1e-12,neighbourhood_absolute_fraction_radius=.0002,
            binary64_domain_tolerance=1e-15,normalized_rank_threshold=1e-8,
            energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
            physical_weights=S['POLICY']['physical_weights'],proposals=0,searches=0,production_changes=0,
            root_criterion_unchanged=True,old_domains_unchanged=True,seed_installation=False,r3_passed=False,m10_closed=False)

@lru_cache(maxsize=1)
def lineage():
    data=S['frozen'](N['ID']);records={r['Id']:r for r in map(json.loads,data['03-measurements.jsonl'].splitlines())}
    directions,_=F['basis']()
    return records['source/1/repeat/1'],records['base/repeat/1'],directions['working_directions']

def requests():
    center,base,directions=lineage()
    points=[dict(Id='historical',Coordinates=base['Coordinates'],OperatingPointFractions=base['OperatingPointFractions']),
            dict(Id='center',Coordinates=center['Coordinates'],OperatingPointFractions=center['OperatingPointFractions'])]
    for label,d in sorted(directions.items()):
        factors=POLICY['historical_one_sided_amplitude_factors'] if d['coefficient_sign']=='nonnegative' else POLICY['central_amplitude_factors']
        for factor in factors:
            h=factor*d['sampled_amplitude']
            for sign,tag in ((1,'plus'),(-1,'minus')):
                points.append(dict(Id=f'{label}/{tag}/{h:g}',Coordinates=[a+sign*h*v for a,v in zip(center['Coordinates'],d['coordinates'])],
                                   OperatingPointFractions=[a+sign*h*v for a,v in zip(center['OperatingPointFractions'],d['operating'])]))
    return [{**p,'Id':p['Id']+f'/repeat/{repeat}'} for p in points for repeat in (1,2)]

def deviation(actual,reference,floor=1e-30):
    return S['norm']([a-b for a,b in zip(actual,reference)])/max(S['norm'](reference),floor)

def derivative(base,positive,negative,h,stencil):
    if stencil=='central':return [(a-b)/(2*h) for a,b in zip(positive,negative)]
    return D['oriented_difference'](base,positive,negative,h)

def choose(candidates):
    eligible=[c for c in candidates if c['available'] and c['relative_deviation_weighted_81']<=.1 and c['relative_deviation_capacities']<=.1]
    return next((c for c in eligible if c['stencil']=='central'),None) or next(iter(eligible),None)

def fixtures():
    req=requests();center,_,directions=lineage()
    P.require(len(directions)==30 and len(req)==260 and len({r['Id'] for r in req})==260,'Direction/request budget fixture')
    P.require(POLICY['maximum_runtime_steps']==2+3*len(req),'Runtime-step budget fixture')
    for r in req[2:]:
        P.require(all(abs(a-b)<=.0002+1e-15 for k in ('Coordinates','OperatingPointFractions') for a,b in zip(r[k],center[k])),'Neighbourhood fixture')
    P.require(derivative([1.],[6.],[2.],1.,'central')==[2.] and derivative([1.],[4.],[9.],1.,'one-sided')==[2.],'Central/quadratic derivative fixture')
    bad=dict(stencil='central',available=True,relative_deviation_weighted_81=.2,relative_deviation_capacities=0.)
    good=dict(stencil='one-sided',available=True,relative_deviation_weighted_81=.01,relative_deviation_capacities=.01)
    P.require(choose([bad,good])==good and choose([{**good,'available':False}]) is None,'Selection/rejection fixture')
    P.require(deviation([0.],[0.],1e-12)==0. and B['rank_diagnostic']({'a':[1.,0.],'b':[2.,0.]},1e-8)['rank']==1,'Zero capacity/rank fixture')
    return 'PASS-LOCAL-BASIS-20-FIXTURES'

def authenticate():
    c=P.load(CONTRACT)
    P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for e in c['implementation_files']+c['frozen_inputs']:P.require(P.digest((ROOT/e['path']).read_bytes())==e['sha256'],'Input drift: '+e['path'])

def preexecution():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists() and not (OUT/'batch-001').exists(),'Existing execution retained')
    P.require(BINARY.exists() and (OUT/'00-build.log').exists(),'Build diagnostic first')
    with (OUT/'00-domain-fixtures.log').open('w',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),'--self-test'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    token=dict(status='PASS-PREEXECUTION',contract_sha256=P.digest(CONTRACT.read_bytes()),fixtures=fixtures(),runtime_steps=0,recipes=len(requests()),
               assemblies=[dict(file=x.name,sha256=P.digest(x.read_bytes())) for x in sorted(BINARY.parent.glob('*.dll'))])
    for folder in (OUT,OUT/'batch-001'):
        P.write(folder,'01-requests.json',requests());P.write(folder,'02-preexecution.json',dict(**token,requests_sha256=P.digest((folder/'01-requests.json').read_bytes())))
    with (OUT/'preflight.log').open('w',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'1','--validate-only'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    return 'PASS-PREEXECUTION'

def capacity_vector(rows):return [r['capacities_kg_s'][k] for r in rows for k in C['CAPACITIES']]

def analyze(records):
    first=Q['audit_pairs'](records,requests());points={r['Id'].rsplit('/repeat/',1)[0]:r for r in first}
    parent,oldbase,directions=lineage();center=points['center'];plan,scales,names=S['layout']()
    for actual,prior in ((center,parent),(points['historical'],oldbase)):
        P.require({k:v for k,v in actual.items() if k!='Id'}=={k:v for k,v in prior.items() if k!='Id'},'Parent full-record drift')
    guards={label:D['reasons'](r,center) for label,r in points.items()};observed={};failures={}
    for label,r in points.items():
        if r['Residuals'] is None:failures[label]=r['Reason'];continue
        try:observed[label]=C['reconstruct'](r)
        except Exception as e:failures[label]=str(e)
    P.require(guards['center']['available'] and 'center' in observed,'Center unavailable')
    weighted={k:S['weighted_vector'](r,scales,names) for k,r in points.items() if r['Residuals'] is not None}
    capacities={k:capacity_vector(r) for k,r in observed.items()}
    summaries=[];columns={};cap_columns={};descriptions={}
    for label,d in sorted(directions.items()):
        h=d['sampled_amplitude'];candidates=[];stored={}
        stencils=[('central',1)]
        if d['coefficient_sign']=='nonnegative':stencils += [('one-sided',1),('one-sided',-1)]
        for stencil,sign in stencils:
            derivatives=[];cap_derivatives=[];used=[];available=True
            for factor in (.5,1.):
                step=h*factor;tag='plus' if sign==1 else 'minus'
                ids=[f'{label}/plus/{step:g}',f'{label}/minus/{step:g}'] if stencil=='central' else [f'{label}/{tag}/{step:g}',f'{label}/{tag}/{2*step:g}']
                used.extend(ids)
                valid=all(guards[i]['available'] and i in observed for i in ids);available &= valid
                if valid:
                    derivatives.append(derivative(weighted['center'],*[weighted[i] for i in ids],step,stencil))
                    cap_derivatives.append(derivative(capacities['center'],*[capacities[i] for i in ids],step,stencil))
            candidate=dict(stencil=stencil,orientation=sign,available=available,records=list(dict.fromkeys(used)),sampled_amplitude=h,
                relative_deviation_weighted_81=deviation(derivatives[1],derivatives[0]) if available else None,
                relative_deviation_capacities=deviation(cap_derivatives[1],cap_derivatives[0],1e-12) if available else None)
            candidates.append(candidate)
            if available:stored[(stencil,sign)]=(derivatives[1],cap_derivatives[1])
        selected=choose(candidates)
        if selected:
            orientation=selected['orientation'];columns[label],cap_columns[label]=stored[(selected['stencil'],orientation)]
            descriptions[label]=dict(coordinates=[orientation*x for x in d['coordinates']],operating=[orientation*x for x in d['operating']],
                                     sampled_amplitude=h,coefficient_sign='both' if selected['stencil']=='central' else 'nonnegative')
        summaries.append(dict(direction=label,historical_coefficient_sign=d['coefficient_sign'],candidates=candidates,selected=selected))
    rows=[r for values in observed.values() for r in values]
    residuals=sorted((dict(checkpoint=c,node=n,kind=k,scaled=v,physical=v*s) for (c,n,k),v,s in zip(names,center['Residuals'],scales)),key=lambda r:abs(r['scaled']),reverse=True)
    return dict(status='PASS-REPAIRED-LOCAL-BASIS-20-EVIDENCE',fixtures=fixtures(),recipes=len(records),maximum_runtime_steps=782,
        exact_repeats=True,parent_full_records_identical=True,guards=guards,observations=observed,capacity_failures=failures,directions=summaries,
        center_dominant_residuals=residuals[:10],center_maximum_original_scaled_residual=max(map(abs,center['Residuals'])),
        selected_direction_count=len(columns),working_directions=descriptions,working_weighted_81_columns=columns,working_capacity_columns=cap_columns,
        rank_diagnostic=B['rank_diagnostic'](columns,1e-8),
        summary=dict(available_points=sum(g['available'] for g in guards.values()),points=len(points),capacity_reconstructions=len(rows),
            exact_minimum_reconstructions=sum(r['exact_minimum'] for r in rows),
            maximum_flow_error_kg_s=max(abs(r['flow_error_kg_s']) for r in rows),
            maximum_global_energy_ledger_error_w=max(abs(e) for r in records if r['Residuals'] is not None for e in Q['audit_power'](r)),
            central_columns=sum(d['coefficient_sign']=='both' for d in descriptions.values()),one_sided_columns=sum(d['coefficient_sign']=='nonnegative' for d in descriptions.values()),
            unavailable_directions=[d['direction'] for d in summaries if d['selected'] is None],
            center_winners=[r['winner'] for r in observed['center']],winner_counts={k:sum(r['winner']==k for r in rows) for k in C['CAPACITIES']}),
        proposal_executed=False,integrated_search_started=False,seed_installed=False,r3_passed=False,m10_closed=False,
        limits=['Only the sampled directional stencils are compared; unselected directions remain unavailable',
                'Capacity derivatives are measured separately; no composed minimum model or proposal is validated',
                'Numerical rank is diagnostic; it does not establish feasible cone sufficiency or an integrated root',
                'No hundred-step confirmation, seed installation or R3 qualification'])

def execute():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing execution retained')
    with (OUT/'execution.log').open('x',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'1'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    (OUT/'03-measurements.jsonl').write_bytes((OUT/'batch-001/03-measurements.jsonl').read_bytes())
    return adjudicate()

def adjudicate():
    authenticate();P.require(P.load(OUT/'01-requests.json')==requests(),'Request drift')
    result=analyze([json.loads(s) for s in (OUT/'03-measurements.jsonl').read_text(encoding='utf-8').splitlines()]);P.write(OUT,'04-analysis.json',result)
    return result['status']

def freeze():
    adjudicate();P.freeze(ID,['00-build.log','00-domain-fixtures.log','01-requests.json','02-preexecution.json','03-measurements.jsonl','04-analysis.json','execution.log'])
    return 'PASS-FROZEN-LOCAL-BASIS-20'

def review():
    authenticate();P.require(P.load(ROOT/'eng'/(ID+'-returned.json'))['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);P.require(json.loads(data['01-requests.json'])==requests(),'Frozen requests drift')
    result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()]);P.require(result==json.loads(data['04-analysis.json']),'Frozen analysis drift')
    return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
