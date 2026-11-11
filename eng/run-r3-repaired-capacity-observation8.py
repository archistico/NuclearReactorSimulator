"""Observe every stage-flow capacity and its winner at all checkpoints of trial 6 and of its active secants."""
from __future__ import annotations
from functools import lru_cache
import copy
import json
import math
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
M=runpy.run_path(str(ROOT/'eng/review-r3-repaired-flow-minimum7.py'))
F,E,D,B,T,S=M['F'],M['E'],M['D'],M['B'],M['T'],M['S']
ID='r3-repaired-capacity-observation8';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT='eng/diagnostics/R3CapacityObservation/R3CapacityObservation.csproj'
BINARY=ROOT/'eng/diagnostics/R3CapacityObservation/bin/Release/net10.0/R3CapacityObservation.dll'
CHECKPOINTS=('seed-step1','post-seed','1')
CAPACITIES=('stop','control','admission','expansion','inventory')
VALVES=dict(stop=('Stop','header','stop-out'),control=('Control','stop-out','control-out'),admission=('Admission','control-out','turbine-inlet'))
POLICY=dict(recipes=38,repeats=2,maximum_runtime_steps=116,dt_s=.01,
            parents='trial 6 records plus both repeated central probes of each active trial-6 direction, identical requests',
            flow_tolerance_kg_s=1e-9,pressure_tolerance_pa=1e-6,first_step_attribution_tolerance_kg_s=1e-9,
            near_tie_kg_s=1e-6,linear_valve_characteristic=True,
            parent_record_identity='exact after removing only the additive StageFlow block',
            capacity_secants='descriptive only; no model, prediction, proposal or validation',
            models=0,proposals=0,searches=0,production_changes=0,root_criterion_unchanged=True,
            previous_results_rewritten=False,r3_passed=False,m10_closed=False)

@lru_cache(maxsize=1)
def lineage():
    _,records,probes=M['inputs']();minimum=json.loads(S['frozen'](M['ID'])['01-analysis.json']);basis,_=F['basis']()
    parents=list(records.values());components=[]
    for c in minimum['active_components']:
        label=c['direction'];h=basis['working_directions'][label]['sampled_amplitude']
        ids={tag:f'{label}/{tag}/{h:g}' for tag in ('plus','minus')}
        components.append(dict(direction=label,amplitude=h,coefficient=c['coefficient'],ids=ids))
        parents+=[probes[f'{ids[tag]}/repeat/{repeat}'] for tag in ('plus','minus') for repeat in (1,2)]
    return parents,components,minimum

def requests():
    return [dict(Id=r['Id'],Coordinates=r['Coordinates'],OperatingPointFractions=r['OperatingPointFractions']) for r in lineage()[0]]

def strip(record):
    result=copy.deepcopy(record)
    for o in result.get('Observations') or []:o.pop('StageFlow')
    return result

def reconstruct(record):
    """Rebuild the production minimum from the step's own valve snapshots and the committed pre-step nodes."""
    previous={n['Id']:n for n in record['Raw']};rows=[]
    for checkpoint,o in zip(CHECKPOINTS,record['Observations']):
        P.require(o['Checkpoint']==checkpoint,'Checkpoint order drift')
        f=o['StageFlow'];P.require(f['InletNodeId']=='turbine-inlet' and f['ExhaustNodeId']=='exhaust','Stage endpoint drift')
        P.require(not f['TripBlocked'] and not f['TurbineAdmissionOpeningInhibited'] and f['StopValvesForcedClosed']==[],'Protection override outside scope')
        inlet,exhaust=previous['turbine-inlet'],previous['exhaust']
        pressure_errors=[S['close'](f['InletPressurePa'],inlet['Pressure'],POLICY['pressure_tolerance_pa'],'Stage inlet pressure is not the committed node'),
                         S['close'](f['ExhaustPressurePa'],exhaust['Pressure'],POLICY['pressure_tolerance_pa'],'Stage exhaust pressure is not the committed node')]
        capacities={};valves={};flow_errors=[]
        for name,(key,source,target) in VALVES.items():
            v=f[key]
            P.require(not v['IsFailSafeActive'] and v['EffectivePosition']==v['CommittedPosition'],'Valve position is not the committed effective position')
            P.require(v['FlowCoefficient']==v['EffectivePosition'],'Valve characteristic is not linear')
            difference=previous[source]['Pressure']-previous[target]['Pressure']
            pressure_errors.append(S['close'](v['PressureDifferencePa'],difference,POLICY['pressure_tolerance_pa'],'Valve pressure difference is not the committed nodes'))
            c=v['FlowCoefficient'];hydraulic=0. if c==0 else math.copysign(math.sqrt(abs(difference)/(v['ResistancePaS2Kg2']/(c*c))),difference)
            flow_errors.append(S['close'](hydraulic,v['MassFlowKgS'],POLICY['flow_tolerance_kg_s'],'Valve law does not reproduce the snapshot flow'))
            capacities[name]=max(0.,v['MassFlowKgS'])
            requested=[x['Requested'] for x in v['RequestedPositions']]
            valves[name]=dict(effective_position=v['EffectivePosition'],requested_positions=requested,
                              requested_minus_effective=[x-v['EffectivePosition'] for x in requested])
        driving=f['InletPressurePa']-f['ExhaustPressurePa']
        capacities['expansion']=math.sqrt(driving/f['ExpansionResistancePaS2Kg2']) if driving>0 else 0.
        capacities['inventory']=.5*inlet['Mass']/POLICY['dt_s']
        winner=min(CAPACITIES,key=lambda k:capacities[k]);flow=capacities[winner]
        error=S['close'](flow,f['CommandedMassFlowKgS'],POLICY['flow_tolerance_kg_s'],'Capacity minimum does not reproduce the commanded stage flow')
        if f['InletQuality'] is not None:
            S['close'](f['CommandedMassFlowKgS']*f['InletQuality'],f['EffectiveMassFlowKgS'],POLICY['flow_tolerance_kg_s'],'Admitted vapor flow mismatch')
        ordered=sorted(capacities.values())
        rows.append(dict(checkpoint=checkpoint,capacities_kg_s=capacities,winner=winner,margin_to_next_kg_s=ordered[1]-flow,
                         near_tie=sorted(k for k in CAPACITIES if capacities[k]-flow<=POLICY['near_tie_kg_s']),
                         commanded_flow_kg_s=f['CommandedMassFlowKgS'],effective_vapor_flow_kg_s=f['EffectiveMassFlowKgS'],
                         inlet_quality=f['InletQuality'],inlet_phase=f['InletPhase'],flow_error_kg_s=error,exact_minimum=error==0.,
                         governor_percent=o['Governor'],valves=valves,
                         maximum_pressure_error_pa=max(map(abs,pressure_errors)),maximum_valve_flow_error_kg_s=max(map(abs,flow_errors))))
        previous={n['Id']:n for n in o['Nodes']}
    return rows

def secants(plus,minus,h):
    return [dict(checkpoint=a['checkpoint'],
                 capacity_secants_kg_s=dict((k,(a['capacities_kg_s'][k]-b['capacities_kg_s'][k])/(2*h)) for k in CAPACITIES),
                 commanded_flow_secant_kg_s=(a['commanded_flow_kg_s']-b['commanded_flow_kg_s'])/(2*h),
                 winners=dict(plus=a['winner'],minus=b['winner']),same_winner=a['winner']==b['winner'])
            for a,b in zip(plus,minus)]

def fixtures():
    P.require(len(requests())==POLICY['recipes']==len({r['Id'] for r in requests()}),'Budget fixture')
    P.require(POLICY['maximum_runtime_steps']==2+3*POLICY['recipes'],'Runtime-step fixture')
    node=lambda i,p,m=1.:dict(Id=i,Pressure=p,Mass=m)
    valve=lambda k,flow:dict(IsFailSafeActive=False,EffectivePosition=1.,CommittedPosition=1.,FlowCoefficient=1.,
                             PressureDifferencePa=k,MassFlowKgS=flow,ResistancePaS2Kg2=1.,RequestedPositions=[])
    raw=[node('header',4.),node('stop-out',3.),node('control-out',2.),node('turbine-inlet',1.,10.),node('exhaust',0.)]
    flow=dict(InletNodeId='turbine-inlet',ExhaustNodeId='exhaust',TripBlocked=False,TurbineAdmissionOpeningInhibited=False,StopValvesForcedClosed=[],
              Stop=valve(1.,1.),Control=valve(1.,1.),Admission=valve(1.,1.),InletPressurePa=1.,ExhaustPressurePa=0.,
              ExpansionResistancePaS2Kg2=4.,CommandedMassFlowKgS=.5,EffectiveMassFlowKgS=.5,InletQuality=1.,InletPhase='fixture')
    row=reconstruct(dict(Raw=raw,Observations=[dict(Checkpoint='seed-step1',Governor=0.,StageFlow=flow,Nodes=raw)]))[0]
    P.require(row['winner']=='expansion' and row['margin_to_next_kg_s']==.5 and row['near_tie']==['expansion'],'Capacity-minimum fixture')
    return 'PASS-CAPACITY-OBSERVATION-FIXTURES'

def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for entry in c['implementation_files']+c['frozen_inputs']:
        P.require(P.digest((ROOT/entry['path']).read_bytes())==entry['sha256'],'Input drift: '+entry['path'])

def preexecution():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing acquisition retained')
    P.require((OUT/'00-build.log').exists() and BINARY.exists(),'Build the capacity observation console first')
    P.write(OUT,'01-requests.json',requests())
    P.write(OUT,'02-preexecution.json',dict(status='PASS-PREEXECUTION',contract_sha256=P.digest(CONTRACT.read_bytes()),
            requests_sha256=P.digest((OUT/'01-requests.json').read_bytes()),fixtures=fixtures(),runtime_steps=0,
            assemblies=[dict(file=p.name,sha256=P.digest(p.read_bytes())) for p in sorted(BINARY.parent.glob('*.dll'))]))
    subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'--validate-only'],cwd=ROOT,check=True)
    return 'PASS-PREEXECUTION'

def analyze(records):
    expected=requests();parents,components,minimum=lineage()
    P.require(len(records)==len(expected)==POLICY['recipes'],'Recipe count drift')
    plan,_,_=S['layout']();points={};energy_errors=[];torque_errors=[];observed={}
    for i in range(0,len(records),2):
        pair=records[i:i+2]
        for r,request,parent in zip(pair,expected[i:i+2],parents[i:i+2]):
            P.require(all(r[k]==request[k] for k in request),'Request/response drift')
            P.require(r['Residuals'] is not None and r['Reason']=='VALID','Parent-valid recipe became unavailable')
            P.require(strip(r)==parent,'Parent record drift outside the additive StageFlow block')
            P.H3['H2']['verify_physics'](r,plan);energy_errors+=S['verify_extra'](r);torque_errors+=T['audit_mechanical'](r)
        P.require({k:v for k,v in pair[0].items() if k!='Id'}=={k:v for k,v in pair[1].items() if k!='Id'},'Repeatability drift')
        label=pair[0]['Id'].rsplit('/repeat/',1)[0];points[label]=pair[0];observed[label]=reconstruct(pair[0])
    base=points['base']
    attribution=[]
    for label,row in [('base',minimum['baseline'])]+[(f'proposal/{p["step_fraction"]:g}',p['capacity_reconstruction']) for p in minimum['proposals']] \
            +[(c['ids'][tag],a[tag]) for c,a in zip(components,minimum['active_components']) for tag in ('plus','minus')]:
        first=observed[label][0]
        errors=[S['close'](first['capacities_kg_s'][k],row['capacities_kg_s'][k],POLICY['first_step_attribution_tolerance_kg_s'],'First-step attribution drift') for k in CAPACITIES]
        P.require(first['winner']==row['winner'],'First-step winner drift')
        attribution.append(dict(record=label,winner=first['winner'],maximum_capacity_difference_kg_s=max(map(abs,errors))))
    directions=[dict(direction=c['direction'],amplitude=c['amplitude'],trial6_coefficient=c['coefficient'],
                     guards={tag:D['reasons'](points[c['ids'][tag]],base) for tag in ('plus','minus')},
                     checkpoints=secants(observed[c['ids']['plus']],observed[c['ids']['minus']],c['amplitude'])) for c in components]
    rows=[row for label in observed for row in observed[label]]
    return dict(status='PASS-REPAIRED-CAPACITY-OBSERVATION-EVIDENCE',fixtures=fixtures(),recipes=POLICY['recipes'],
                maximum_runtime_steps=POLICY['maximum_runtime_steps'],exact_repeats=True,parent_records_identical=True,
                first_step_attribution_reproduced=attribution,
                maximum_energy_ledger_error_w=max(map(abs,energy_errors)),maximum_torque_ledger_error_nm=max(map(abs,torque_errors)),
                observations={label:observed[label] for label in observed},
                winners_by_checkpoint={label:[r['winner'] for r in observed[label]] for label in observed},
                active_directions=directions,
                summary=dict(exact_minimum_reconstructions=sum(r['exact_minimum'] for r in rows),reconstructions=len(rows),
                             maximum_flow_error_kg_s=max(abs(r['flow_error_kg_s']) for r in rows),
                             maximum_pressure_error_pa=max(r['maximum_pressure_error_pa'] for r in rows),
                             maximum_valve_flow_error_kg_s=max(r['maximum_valve_flow_error_kg_s'] for r in rows),
                             winner_counts={k:sum(r['winner']==k for r in rows) for k in CAPACITIES},
                             minimum_margin_by_checkpoint={c:min(observed[label][i]['margin_to_next_kg_s'] for label in observed) for i,c in enumerate(CHECKPOINTS)},
                             maximum_control_lag={c:max((abs(x) for label in observed for x in observed[label][i]['valves']['control']['requested_minus_effective']),default=None)
                                                  for i,c in enumerate(CHECKPOINTS)},
                             records_changing_winner_between_checkpoints=sorted(label for label in observed if len({r['winner'] for r in observed[label]})>1),
                             directions_with_winner_change_between_signs={c['direction']:[x['checkpoint'] for x in d['checkpoints'] if not x['same_winner']]
                                                                          for c,d in zip(components,directions)}),
                model_built=False,proposal_executed=False,search_started=False,family_selected=False,root_observed=False,r3_passed=False,m10_closed=False,
                limits=['Capacity secants are descriptive; a secant across different winners is not a derivative of the stage flow',
                        'No model of the minimum is fitted or validated here; trial-6 prediction failures are unchanged',
                        'Only the 38 parent recipes are observed; no new direction, amplitude or proposal'])

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
    return 'PASS-FROZEN-CAPACITY-OBSERVATION'

def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()]);P.require(result==json.loads(data['04-analysis.json']),'Replay drift')
    return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
