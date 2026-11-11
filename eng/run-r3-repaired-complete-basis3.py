"""Acquire all thirty repaired-state columns with the unchanged diagnostic criteria."""
from __future__ import annotations
import json
import math
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
T=runpy.run_path(str(ROOT/'eng/run-r3-repaired-missing-directions2.py'))
S=T['S']
ID='r3-repaired-complete-basis3'
OUT=ROOT/'artifacts'/ID
CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT=T['PROJECT']
BINARY=T['BINARY']
POLICY=dict(recipes=362,repeats=2,maximum_runtime_steps=1088,
            ordinary_amplitudes=[1e-6,1e-5,1e-4],pressure_amplitudes=[1e-7,1e-6,2e-6],exhaust_energy_amplitudes=[1e-6,3e-5,1e-4],
            relative_linearity_tolerance=.1,physical_weights=S['POLICY']['physical_weights'],
            energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
            normalized_rank_threshold=1e-8,selection=T['POLICY']['selection'],
            proposals=0,searches=0,production_changes=0,root_criterion_unchanged=True,r3_passed=False,m10_closed=False)
OPERATING=['control-valve','condensate-pump','feedwater-pump','neutron-population','fuel-temperature-celsius','structure-temperature-celsius']

def directions():
    plan,_,_=S['layout']()
    result=[]
    for index,node in enumerate(plan['search']['variable_order']):
        for offset,quantity in enumerate(('mass','energy')):
            name=f'{node}:{quantity}'
            amplitudes=POLICY['pressure_amplitudes'] if node=='pressure' else POLICY['exhaust_energy_amplitudes'] if name=='exhaust:energy' else POLICY['ordinary_amplitudes']
            result.append(dict(name=name,inventory_index=2*index+offset,operating_index=None,amplitudes=amplitudes))
    result += [dict(name=name,inventory_index=None,operating_index=i,amplitudes=POLICY['ordinary_amplitudes']) for i,name in enumerate(OPERATING)]
    return result

def requests():
    points=[dict(Id='base',Coordinates=[0.]*24,OperatingPointFractions=[0.]*6)]
    for direction in directions():
        for h in direction['amplitudes']:
            for sign,tag in ((1,'plus'),(-1,'minus')):
                fluid,operating=[0.]*24,[0.]*6
                if direction['inventory_index'] is not None: fluid[direction['inventory_index']]=sign*h
                else: operating[direction['operating_index']]=sign*h
                points.append(dict(Id=f'{direction["name"]}/{tag}/{h:g}',Coordinates=fluid,OperatingPointFractions=operating))
    return [{**point,'Id':point['Id']+f'/repeat/{repeat}'} for point in points for repeat in (1,2)]

def rank_diagnostic(columns,threshold):
    """Twice-reorthogonalized, pivoted Gram-Schmidt of unit-norm columns; diagnostic only."""
    pending={name:[v/S['norm'](column) for v in column] for name,column in columns.items() if S['norm'](column)>0}
    basis=[];pivots=[]
    while pending:
        residuals={}
        for name,column in pending.items():
            vector=column[:]
            for _ in range(2):
                for q in basis:
                    dot=math.fsum(a*b for a,b in zip(q,vector))
                    vector=[a-dot*b for a,b in zip(vector,q)]
            residuals[name]=vector
        name=max(residuals,key=lambda k:S['norm'](residuals[k]))
        vector=residuals[name];length=S['norm'](vector)
        if length<=threshold: break
        basis.append([x/length for x in vector]);pivots.append(dict(coordinate=name,normalized_remaining_norm=length));del pending[name]
    return dict(rank=len(basis),columns=len(columns),threshold=threshold,pivots=pivots,remaining=list(pending),certified_rank=False)

def fixtures():
    P.require(len(directions())==30 and len(requests())==362,'Basis/request budget fixture')
    P.require(rank_diagnostic({'a':[1.,0.],'b':[2.,0.]},1e-8)['rank']==1,'Dependent rank fixture')
    P.require(rank_diagnostic({'a':[1.,0.],'b':[0.,1.]},1e-8)['rank']==2,'Independent rank fixture')
    return 'PASS-COMPLETE-BASIS-FIXTURES'

def authenticate():
    c=P.load(CONTRACT)
    P.require(c['schema']==ID+'-v1' and c['policy']==POLICY and c['directions']==directions(),'Policy drift')
    for key in ('src','tests'): P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
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

def analyze(records):
    expected=requests();P.require(len(records)==len(expected)==362,'Recipe count drift')
    plan,scales,names=S['layout']();points={};energy_errors=[];torque_errors=[]
    for i in range(0,len(records),2):
        pair=records[i:i+2]
        for r,request in zip(pair,expected[i:i+2]):
            P.require(all(r[k]==request[k] for k in request),'Request/response drift')
            P.H3['H2']['verify_physics'](r,plan)
            energy_errors+=S['verify_extra'](r);torque_errors+=T['audit_mechanical'](r)
        P.require({k:v for k,v in pair[0].items() if k!='Id'}=={k:v for k,v in pair[1].items() if k!='Id'},'Repeatability drift')
        points[pair[0]['Id'].rsplit('/repeat/',1)[0]]=pair[0]
    old=S['frozen'](T['ID']);prior=T['parent_records']()
    prior.update({r['Id']:r for r in map(json.loads,old['03-measurements.jsonl'].splitlines())})
    overlaps=[]
    for key,r in points.items():
        reference=prior.get(key+'/repeat/1')
        if reference and r['Residuals'] is not None:
            for field in ('Raw','Residuals','Phases'): P.require(r[field]==reference[field],'Parent point drift')
            P.require([o['StateSha256'] for o in r['Observations']]==[o['StateSha256'] for o in reference['Observations']],'Parent full-state drift')
            overlaps.append(key)
    base=points['base'];summaries=[];columns={}
    for direction in directions():
        usable={};samples=[]
        for h in direction['amplitudes']:
            plus,minus=[points[f'{direction["name"]}/{tag}/{h:g}'] for tag in ('plus','minus')]
            valid=S['compatible'](plus,base) and S['compatible'](minus,base)
            mechanical_match=T['mechanical_regime'](plus)==T['mechanical_regime'](base)==T['mechanical_regime'](minus) if plus['Residuals'] and minus['Residuals'] else False
            admitted=valid and mechanical_match
            sample=dict(amplitude=h,central_usable=admitted,original_guards_passed=valid,mechanical_regime_match=mechanical_match,
                        plus_reason=plus['Reason'],minus_reason=minus['Reason'])
            if admitted:
                usable[h]=[(a-b)/(2*h) for a,b in zip(S['weighted_vector'](plus,scales,names),S['weighted_vector'](minus,scales,names))]
            samples.append(sample)
        pairs=[]
        for lower,upper in zip(direction['amplitudes'],direction['amplitudes'][1:]):
            admitted=lower in usable and upper in usable
            pairs.append(dict(reference_amplitude=lower,amplitude=upper,usable=admitted,
                              relative_deviation=S['relative'](usable[upper],usable[lower]) if admitted else None))
        selected=T['select_pair'](pairs)
        if selected: columns[direction['name']]=usable[selected['amplitude']]
        summaries.append(dict(direction=direction['name'],samples=samples,adjacent_pairs=pairs,selected=selected))
    return dict(status='PASS-REPAIRED-COMPLETE-BASIS-EVIDENCE',fixtures=fixtures(),recipes=362,maximum_runtime_steps=1088,
                exact_repeats=True,parent_points_reproduced=overlaps,
                valid_recipes=sum(r['Reason']=='VALID' for r in records),
                in_envelope_recipes=sum(r['Residuals'] is not None and all(o['InEnvelope'] for o in r['Observations']) for r in records),
                maximum_energy_ledger_error_w=max(map(abs,energy_errors)),maximum_torque_ledger_error_nm=max(map(abs,torque_errors)),
                directions=summaries,selected_weighted_81_columns=columns,selected_column_count=len(columns),
                all_coordinate_columns_available=len(columns)==30,rank_diagnostic=rank_diagnostic(columns,POLICY['normalized_rank_threshold']),
                family_selected=False,root_observed=False,r3_passed=False,m10_closed=False,
                limits=['Column availability and numerical rank do not establish family sufficiency',
                        'No proposal or nonlinear prediction has been verified; no extrapolation beyond the sampled scales',
                        'Six thermal diagnostic rows supplement the original 75 root residuals'])

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
    return 'PASS-FROZEN-COMPLETE-BASIS'

def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json')
    P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()])
    P.require(result==json.loads(data['04-analysis.json']),'Replay drift')
    return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
