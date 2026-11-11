"""Predeclared branch-compatible central/one-sided probes of the eleven missing columns."""
from __future__ import annotations
import json
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
B=runpy.run_path(str(ROOT/'eng/run-r3-repaired-complete-basis3.py'))
T,S=B['T'],B['S']
ID='r3-repaired-branch-basis4'
OUT=ROOT/'artifacts'/ID
CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT,BINARY=T['PROJECT'],T['BINARY']
POLICY=dict(recipes=394,repeats=2,maximum_runtime_steps=1184,
            branch_amplitudes=[1e-8,3e-8,1e-7,3e-7,1e-6],
            central_amplitudes=[1e-7,3e-7,1e-6,3e-6,1e-5,3e-5,1e-4],
            relative_linearity_tolerance=.1,physical_weights=S['POLICY']['physical_weights'],
            energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
            normalized_rank_threshold=1e-8,one_sided_order=2,
            selection='Prefer any validated central pair; otherwise largest validated one-sided pair, plus sign wins ties',
            proposals=0,searches=0,production_changes=0,root_criterion_unchanged=True,r3_passed=False,m10_closed=False)
BRANCH=[('drum:mass',2),('drum:energy',3),('feedwater-inventory:mass',6),('hotwell:mass',10),('hotwell:energy',11),('suction:mass',20),('suction:energy',21)]
CENTRAL=[('control-out:mass',0),('control-out:energy',1),('turbine-inlet:mass',22),('turbine-inlet:energy',23)]

def directions():
    return [dict(name=name,index=index,one_sided=True,amplitudes=POLICY['branch_amplitudes']) for name,index in BRANCH]+[
        dict(name=name,index=index,one_sided=False,amplitudes=POLICY['central_amplitudes']) for name,index in CENTRAL]

def sample_amplitudes(direction):
    h=direction['amplitudes']
    return sorted(set(h+[2*x for x in h])) if direction['one_sided'] else h

def requests():
    points=[dict(Id='base',Coordinates=[0.]*24,OperatingPointFractions=[0.]*6)]
    for d in directions():
        for h in sample_amplitudes(d):
            for sign,tag in ((1,'plus'),(-1,'minus')):
                coordinates=[0.]*24;coordinates[d['index']]=sign*h
                points.append(dict(Id=f'{d["name"]}/{tag}/{h:g}',Coordinates=coordinates,OperatingPointFractions=[0.]*6))
    return [{**point,'Id':point['Id']+f'/repeat/{repeat}'} for point in points for repeat in (1,2)]

def oriented_difference(base,first,second,h):
    # Numerically stable form of (-3 f0 + 4 f(h) - f(2h))/(2h).
    return [((4*(a-b))-(c-b))/(2*h) for b,a,c in zip(base,first,second)]

def select(candidates):
    good=[p for p in candidates if p['usable'] and p['relative_deviation']<=POLICY['relative_linearity_tolerance']]
    central=[p for p in good if p['stencil']=='central']
    return max(central or good,key=lambda p:(p['amplitude'],p['orientation'])) if good else None

def fixtures():
    P.require(len(requests())==394 and len({r['Id'] for r in requests()})==394,'Budget fixture')
    P.require(oriented_difference([0.],[3.],[8.],1.)==[2.],'Quadratic one-sided fixture')
    P.require(oriented_difference([0.],[-1.],[0.],1.)==[-2.],'Negative direction fixture')
    cases=[dict(stencil='one-sided',amplitude=2.,orientation=-1,usable=True,relative_deviation=.01),
           dict(stencil='central',amplitude=1.,orientation=1,usable=True,relative_deviation=.02)]
    P.require(select(cases)['stencil']=='central','Central preference fixture')
    P.require(select([{**cases[0],'relative_deviation':.2}]) is None,'Linearity rejection fixture')
    return 'PASS-BRANCH-BASIS-FIXTURES'

def authenticate():
    c=P.load(CONTRACT)
    P.require(c['schema']==ID+'-v1' and c['policy']==POLICY and c['directions']==directions(),'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for entry in c['implementation_files']+c['frozen_inputs']:
        P.require(P.digest((ROOT/entry['path']).read_bytes())==entry['sha256'],'Input drift: '+entry['path'])

def preexecution():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing acquisition retained')
    P.write(OUT,'01-requests.json',requests())
    P.require(BINARY.exists(),'Build reusable runner first')
    P.write(OUT,'02-preexecution.json',dict(status='PASS-PREEXECUTION',contract_sha256=P.digest(CONTRACT.read_bytes()),
            requests_sha256=P.digest((OUT/'01-requests.json').read_bytes()),fixtures=fixtures(),runtime_steps=0,
            assemblies=[dict(file=p.name,sha256=P.digest(p.read_bytes())) for p in sorted(BINARY.parent.glob('*.dll'))]))
    subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'--validate-only'],cwd=ROOT,check=True)
    return 'PASS-PREEXECUTION'

def admissible(record,base):
    return S['compatible'](record,base) and T['mechanical_regime'](record)==T['mechanical_regime'](base)

def reasons(record,base):
    if record['Residuals'] is None:return dict(available=False,rejection=record['Reason'])
    return dict(available=admissible(record,base),healthy=record['Reason']=='VALID',
                in_envelope=all(o['InEnvelope'] for o in record['Observations']),
                phase_changes=[dict(checkpoint='raw' if i<12 else ['seed-step1','post-seed','1'][(i-12)//12],
                                    node=base['Raw'][i%12]['Id'],before=a,after=b)
                               for i,(a,b) in enumerate(zip(base['Phases'],record['Phases'])) if a!=b],
                same_hydraulic_and_cooling_regime=S['regime'](record)==S['regime'](base),
                same_mechanical_regime=T['mechanical_regime'](record)==T['mechanical_regime'](base))

def analyze(records):
    expected=requests();P.require(len(records)==len(expected)==394,'Recipe count drift')
    plan,scales,names=S['layout']();points={};energy_errors=[];torque_errors=[]
    for i in range(0,len(records),2):
        pair=records[i:i+2]
        for r,request in zip(pair,expected[i:i+2]):
            P.require(all(r[key]==request[key] for key in request),'Request/response drift')
            P.H3['H2']['verify_physics'](r,plan);energy_errors+=S['verify_extra'](r);torque_errors+=T['audit_mechanical'](r)
        P.require({k:v for k,v in pair[0].items() if k!='Id'}=={k:v for k,v in pair[1].items() if k!='Id'},'Repeatability drift')
        points[pair[0]['Id'].rsplit('/repeat/',1)[0]]=pair[0]
    parent=S['frozen'](B['ID']);old={r['Id']:r for r in map(json.loads,parent['03-measurements.jsonl'].splitlines())}
    original=json.loads(parent['04-analysis.json']);base=points['base'];overlaps=[]
    for key,r in points.items():
        prior=old.get(key+'/repeat/1')
        if prior and r['Residuals'] is not None:
            for field in ('Raw','Residuals','Phases'):P.require(r[field]==prior[field],'Parent point drift')
            P.require([o['StateSha256'] for o in r['Observations']]==[o['StateSha256'] for o in prior['Observations']],'Parent full-state drift')
            overlaps.append(key)
    columns=dict(original['selected_weighted_81_columns'])
    domains={name:dict(stencil='central',orientation=1,coefficient_sign='both') for name in columns}
    summaries=[];base_vector=S['weighted_vector'](base,scales,names)
    for d in directions():
        sampled=[];derivatives={}
        for h in sample_amplitudes(d):
            plus,minus=[points[f'{d["name"]}/{tag}/{h:g}'] for tag in ('plus','minus')]
            sampled.append(dict(amplitude=h,plus=reasons(plus,base),minus=reasons(minus,base)))
        for h in d['amplitudes']:
            plus,minus=[points[f'{d["name"]}/{tag}/{h:g}'] for tag in ('plus','minus')]
            if admissible(plus,base) and admissible(minus,base):
                derivatives[('central',1,h)]=[(a-b)/(2*h) for a,b in zip(S['weighted_vector'](plus,scales,names),S['weighted_vector'](minus,scales,names))]
            if d['one_sided']:
                for sign,tag in ((1,'plus'),(-1,'minus')):
                    first,second=points[f'{d["name"]}/{tag}/{h:g}'],points[f'{d["name"]}/{tag}/{2*h:g}']
                    if admissible(first,base) and admissible(second,base):
                        derivatives[('one-sided',sign,h)]=oriented_difference(base_vector,S['weighted_vector'](first,scales,names),S['weighted_vector'](second,scales,names),h)
        candidates=[]
        for stencil,sign in [('central',1)]+([('one-sided',1),('one-sided',-1)] if d['one_sided'] else []):
            for lower,upper in zip(d['amplitudes'],d['amplitudes'][1:]):
                a,b=(stencil,sign,lower),(stencil,sign,upper);available=a in derivatives and b in derivatives
                candidates.append(dict(stencil=stencil,orientation=sign,reference_amplitude=lower,amplitude=upper,usable=available,
                                       relative_deviation=S['relative'](derivatives[b],derivatives[a]) if available else None))
        chosen=select(candidates)
        if chosen:
            columns[d['name']]=derivatives[(chosen['stencil'],chosen['orientation'],chosen['amplitude'])]
            domains[d['name']]=dict(stencil=chosen['stencil'],orientation=chosen['orientation'],
                                   coefficient_sign='nonnegative' if chosen['stencil']=='one-sided' else 'both',
                                   probe_amplitude=chosen['amplitude'],furthest_sampled_fraction=(2 if chosen['stencil']=='one-sided' else 1)*chosen['amplitude'])
        summaries.append(dict(direction=d['name'],samples=sampled,adjacent_pairs=candidates,selected=chosen))
    return dict(status='PASS-REPAIRED-BRANCH-BASIS-EVIDENCE',fixtures=fixtures(),recipes=394,maximum_runtime_steps=1184,
                exact_repeats=True,parent_points_reproduced=overlaps,
                valid_recipes=sum(r['Reason']=='VALID' for r in records),
                in_envelope_recipes=sum(r['Residuals'] is not None and all(o['InEnvelope'] for o in r['Observations']) for r in records),
                maximum_energy_ledger_error_w=max(map(abs,energy_errors)),maximum_torque_ledger_error_nm=max(map(abs,torque_errors)),
                directions=summaries,working_weighted_81_columns=columns,coordinate_domains=domains,
                selected_column_count=len(columns),one_sided_coordinates=[name for name,domain in domains.items() if domain['stencil']=='one-sided'],
                rank_diagnostic=B['rank_diagnostic'](columns,POLICY['normalized_rank_threshold']),
                all_coordinate_directions_available=len(columns)==30,root_observed=False,family_selected=False,r3_passed=False,m10_closed=False,
                limits=['Oriented one-sided derivatives require nonnegative coefficients and a new physical verification',
                        'Sampled phase compatibility does not prove an entire step interval or the sum of directions remains in the branch',
                        'Numerical rank and complete availability do not establish cone feasibility or family sufficiency'])

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
    return 'PASS-FROZEN-BRANCH-BASIS'

def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json')
    P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()])
    P.require(result==json.loads(data['04-analysis.json']),'Replay drift')
    return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
