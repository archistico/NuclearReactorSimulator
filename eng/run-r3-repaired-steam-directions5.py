"""Measure paired mass/energy directions for the two unresolved steam-path axes."""
from __future__ import annotations
from functools import lru_cache
import json
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
D=runpy.run_path(str(ROOT/'eng/run-r3-repaired-branch-basis4.py'))
B,T,S=D['B'],D['T'],D['S']
ID='r3-repaired-steam-directions5';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT,BINARY=D['PROJECT'],D['BINARY']
POLICY=dict(recipes=114,repeats=2,maximum_runtime_steps=344,amplitudes=[1e-7,3e-7,1e-6,3e-6,1e-5,3e-5,1e-4],
            raw_pressure_reference_amplitude=1e-6,relative_linearity_tolerance=.1,physical_weights=S['POLICY']['physical_weights'],
            energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
            normalized_rank_threshold=1e-8,selection='Prefer validated raw-isobaric direction; otherwise iso-specific-internal-energy; largest adjacent validated pair',
            proposals=0,searches=0,production_changes=0,root_criterion_unchanged=True,r3_passed=False,m10_closed=False)

@lru_cache(maxsize=1)
def previous():
    data=S['frozen'](D['ID'])
    return {r['Id']:r for r in map(json.loads,data['03-measurements.jsonl'].splitlines())},json.loads(data['04-analysis.json'])

def directions():
    old,_=previous();h=POLICY['raw_pressure_reference_amplitude'];result=[]
    for node,index in (('control-out',0),('turbine-inlet',11)):
        def difference(quantity):
            return old[f'{node}:{quantity}/plus/{h:g}/repeat/1']['Raw'][index]['Pressure']-old[f'{node}:{quantity}/minus/{h:g}/repeat/1']['Raw'][index]['Pressure']
        ratio=-difference('mass')/difference('energy')
        for kind,coefficient in (('raw-isobaric',ratio),('iso-specific-internal-energy',1.)):
            result.append(dict(name=f'{node}:{kind}',node=node,indices=[2*index,2*index+1],coefficients=[1.,coefficient]))
    return result

def requests():
    points=[dict(Id='base',Coordinates=[0.]*24,OperatingPointFractions=[0.]*6)]
    for d in directions():
        for h in POLICY['amplitudes']:
            for sign,tag in ((1,'plus'),(-1,'minus')):
                coordinates=[0.]*24
                for index,k in zip(d['indices'],d['coefficients']):coordinates[index]=sign*h*k
                points.append(dict(Id=f'{d["name"]}/{tag}/{h:g}',Coordinates=coordinates,OperatingPointFractions=[0.]*6))
    return [{**p,'Id':p['Id']+f'/repeat/{repeat}'} for p in points for repeat in (1,2)]

def fixtures():
    P.require(len(requests())==114 and len({r['Id'] for r in requests()})==114,'Budget fixture')
    P.require(all(d['coefficients'][1]!=0 for d in directions()),'Singular paired direction')
    P.require(T['select_pair']([dict(amplitude=1.,usable=True,relative_deviation=.2)]) is None,'Linearity fixture')
    return 'PASS-STEAM-DIRECTIONS-FIXTURES'

def authenticate():
    c=P.load(CONTRACT)
    P.require(c['schema']==ID+'-v1' and c['policy']==POLICY and c['directions']==directions(),'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for entry in c['implementation_files']+c['frozen_inputs']:
        P.require(P.digest((ROOT/entry['path']).read_bytes())==entry['sha256'],'Input drift: '+entry['path'])

def preexecution():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing acquisition retained')
    P.write(OUT,'01-requests.json',requests());P.require(BINARY.exists(),'Build runner first')
    P.write(OUT,'02-preexecution.json',dict(status='PASS-PREEXECUTION',contract_sha256=P.digest(CONTRACT.read_bytes()),
            requests_sha256=P.digest((OUT/'01-requests.json').read_bytes()),fixtures=fixtures(),runtime_steps=0,
            assemblies=[dict(file=p.name,sha256=P.digest(p.read_bytes())) for p in sorted(BINARY.parent.glob('*.dll'))]))
    subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'--validate-only'],cwd=ROOT,check=True)
    return 'PASS-PREEXECUTION'

def working_directions(columns,domains):
    old_basis=json.loads(S['frozen'](B['ID'])['04-analysis.json']);_,branch=previous();result={}
    for d in B['directions']():
        selected=next(item for item in old_basis['directions'] if item['direction']==d['name'])['selected']
        if selected:
            fluid,operating=[0.]*24,[0.]*6
            if d['inventory_index'] is not None:fluid[d['inventory_index']]=1.
            else:operating[d['operating_index']]=1.
            result[d['name']]=dict(coordinates=fluid,operating=operating,coefficient_sign='both',sampled_amplitude=selected['amplitude'])
    for d in D['directions']():
        selected=next(item for item in branch['directions'] if item['direction']==d['name'])['selected']
        if selected:
            fluid=[0.]*24;fluid[d['index']]=selected['orientation']
            result[d['name']]=dict(coordinates=fluid,operating=[0.]*6,coefficient_sign=domains[d['name']]['coefficient_sign'],sampled_amplitude=selected['amplitude'])
    return result

def analyze(records):
    expected=requests();P.require(len(records)==len(expected)==114,'Recipe count drift')
    plan,scales,names=S['layout']();points={};energy_errors=[];torque_errors=[]
    for i in range(0,len(records),2):
        pair=records[i:i+2]
        for r,request in zip(pair,expected[i:i+2]):
            P.require(all(r[k]==request[k] for k in request),'Request/response drift')
            P.H3['H2']['verify_physics'](r,plan);energy_errors+=S['verify_extra'](r);torque_errors+=T['audit_mechanical'](r)
        P.require({k:v for k,v in pair[0].items() if k!='Id'}=={k:v for k,v in pair[1].items() if k!='Id'},'Repeatability drift')
        points[pair[0]['Id'].rsplit('/repeat/',1)[0]]=pair[0]
    old,prior=previous();base=points['base'];reference=old['base/repeat/1']
    for field in ('Raw','Residuals','Phases'):P.require(base[field]==reference[field],'Parent baseline drift')
    P.require([o['StateSha256'] for o in base['Observations']]==[o['StateSha256'] for o in reference['Observations']],'Parent full-state drift')
    columns=dict(prior['working_weighted_81_columns']);domains=dict(prior['coordinate_domains'])
    descriptions=working_directions(columns,domains);summaries=[];valid={}
    for d in directions():
        usable={};samples=[]
        for h in POLICY['amplitudes']:
            plus,minus=[points[f'{d["name"]}/{tag}/{h:g}'] for tag in ('plus','minus')]
            admitted=D['admissible'](plus,base) and D['admissible'](minus,base)
            samples.append(dict(amplitude=h,central_usable=admitted,plus=D['reasons'](plus,base),minus=D['reasons'](minus,base)))
            if admitted:usable[h]=[(a-b)/(2*h) for a,b in zip(S['weighted_vector'](plus,scales,names),S['weighted_vector'](minus,scales,names))]
        pairs=[]
        for a,b in zip(POLICY['amplitudes'],POLICY['amplitudes'][1:]):
            admitted=a in usable and b in usable
            pairs.append(dict(reference_amplitude=a,amplitude=b,usable=admitted,relative_deviation=S['relative'](usable[b],usable[a]) if admitted else None))
        selected=T['select_pair'](pairs)
        if selected:valid[d['name']]=(selected,usable[selected['amplitude']])
        summaries.append(dict(direction=d['name'],samples=samples,adjacent_pairs=pairs,selected=selected))
    chosen=[]
    for node in ('control-out','turbine-inlet'):
        candidates=[d for d in directions() if d['node']==node and d['name'] in valid]
        if not candidates:continue
        d=candidates[0];name=d['name'];selected,column=valid[name];fluid=[0.]*24
        for index,k in zip(d['indices'],d['coefficients']):fluid[index]=k
        columns[name]=column;domains[name]=dict(stencil='central-paired',orientation=1,coefficient_sign='both',probe_amplitude=selected['amplitude'])
        descriptions[name]=dict(coordinates=fluid,operating=[0.]*6,coefficient_sign='both',sampled_amplitude=selected['amplitude'])
        chosen.append(name)
    P.require(set(descriptions)==set(columns),'Working direction mapping incomplete')
    return dict(status='PASS-REPAIRED-STEAM-DIRECTIONS-EVIDENCE',fixtures=fixtures(),recipes=114,maximum_runtime_steps=344,
                exact_repeats=True,baseline_matches_parent=True,
                valid_recipes=sum(r['Reason']=='VALID' for r in records),
                in_envelope_recipes=sum(r['Residuals'] is not None and all(o['InEnvelope'] for o in r['Observations']) for r in records),
                maximum_energy_ledger_error_w=max(map(abs,energy_errors)),maximum_torque_ledger_error_nm=max(map(abs,torque_errors)),
                directions=summaries,chosen_paired_directions=chosen,working_weighted_81_columns=columns,working_directions=descriptions,
                coordinate_domains=domains,selected_direction_count=len(columns),one_sided_directions=prior['one_sided_coordinates'],
                rank_diagnostic=B['rank_diagnostic'](columns,POLICY['normalized_rank_threshold']),
                family_selected=False,root_observed=False,r3_passed=False,m10_closed=False,
                limits=['A paired direction does not validate either missing Cartesian derivative',
                        'Four one-sided coefficients remain nonnegative; rank does not establish cone feasibility',
                        'The summed direction and any proposal still require independent physical verification'])

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
    return 'PASS-FROZEN-STEAM-DIRECTIONS'

def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()]);P.require(result==json.loads(data['04-analysis.json']),'Replay drift')
    return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
