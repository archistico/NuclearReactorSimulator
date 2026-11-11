"""Offline first-seed attribution of trial 6 to the stage-flow capacity minimum."""
from __future__ import annotations
from functools import lru_cache
import json
import math
import runpy
import sys
import r3_h_protocol as P

ROOT=P.ROOT
F=runpy.run_path(str(ROOT/'eng/run-r3-repaired-proposal-trial6.py'))
E,D,B,T,S=F['E'],F['D'],F['B'],F['T'],F['S']
ID='r3-repaired-flow-minimum7';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
POLICY=dict(runtime_steps=0,scope='first seed checkpoint only',mass_tolerance_kg_s=1e-9,torque_tolerance_nm=1e-6,
            valve_resistance=1000.,expansion_resistance=21400.,dt_s=.01,control_valve_percent=29.281329697436618,
            previous_guards_rewritten=False,production_changes=0,r3_passed=False,m10_closed=False)

def reconstruct(record):
    P.require(record['OperatingPointFractions']==[0.]*6,'Changed operating point outside reconstruction scope')
    n={r['Id']:r for r in record['Raw']};o=record['Observations'][0]
    P.require(o['Checkpoint']=='seed-step1' and o['Governor']==POLICY['control_valve_percent'],'First-step valve command outside scope')
    opening=o['Governor']/100.
    capacities=dict(stop=math.sqrt(max(0.,n['header']['Pressure']-n['stop-out']['Pressure'])/POLICY['valve_resistance']),
                    control=math.sqrt(max(0.,n['stop-out']['Pressure']-n['control-out']['Pressure'])/(POLICY['valve_resistance']/(opening*opening))),
                    admission=math.sqrt(max(0.,n['control-out']['Pressure']-n['turbine-inlet']['Pressure'])/POLICY['valve_resistance']),
                    expansion=math.sqrt(max(0.,n['turbine-inlet']['Pressure']-n['exhaust']['Pressure'])/POLICY['expansion_resistance']),
                    inventory=.5*n['turbine-inlet']['Mass']/POLICY['dt_s'])
    winner=min(capacities,key=capacities.get);flow=capacities[winner]
    vapor=flow*n['turbine-inlet']['Quality'];actual=o['Extra']['Stage']['VaporMassKgS']
    error=S['close'](vapor,actual,POLICY['mass_tolerance_kg_s'],'First-step capacity minimum does not reproduce stage vapor flow')
    mechanical=o['Extra']['Mechanical']
    turbine=vapor*mechanical['StageIdealWorkJKg']*mechanical['StageEfficiency']/max(mechanical['InitialOmega'],mechanical['RatedOmega'])
    net=turbine-mechanical['ExternalLoadTorqueNm']-mechanical['LossTorqueNm']
    torque_error=S['close'](net,o['RotorTorque'],POLICY['torque_tolerance_nm'],'Reconstructed torque mismatch')
    return dict(capacities_kg_s=capacities,winner=winner,margin_to_next_kg_s=sorted(capacities.values())[1]-flow,
                total_flow_kg_s=flow,vapor_flow_kg_s=vapor,flow_error_kg_s=error,
                reconstructed_net_torque_nm=net,torque_error_nm=torque_error)

@lru_cache(maxsize=1)
def inputs():
    trial=S['frozen'](F['ID']);analysis=json.loads(trial['04-analysis.json'])
    records={r['Id']:r for r in map(json.loads,trial['03-measurements.jsonl'].splitlines())}
    probes={}
    for protocol in (B['ID'],D['ID'],E['ID']):
        probes.update({r['Id']:r for r in map(json.loads,S['frozen'](protocol)['03-measurements.jsonl'].splitlines())})
    return analysis,records,probes

def fixtures():
    flow=lambda values:min(values)
    # Central coordinate secants of min(x,y) need not predict a joint directional change.
    h=1.;dx=(flow([h,0])-flow([-h,0]))/(2*h);dy=(flow([0,h])-flow([0,-h]))/(2*h)
    P.require(dx==dy==.5 and flow([1.,2.])!=dx+2*dy,'Minimum kink fixture')
    return 'PASS-FLOW-MINIMUM-FIXTURE'

def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for entry in c['implementation_files']+c['frozen_inputs']:
        P.require(P.digest((ROOT/entry['path']).read_bytes())==entry['sha256'],'Input drift: '+entry['path'])

def analyze():
    analysis,records,probes=inputs();basis,_=F['basis']();base=records['base/repeat/1'];baseline=reconstruct(base)
    model=analysis['model'];components=[]
    for label,coefficient in zip(model['labels'],model['coefficients']):
        if abs(coefficient)<=1e-13:continue
        h=basis['working_directions'][label]['sampled_amplitude']
        plus,minus=[probes[f'{label}/{tag}/{h:g}/repeat/1'] for tag in ('plus','minus')]
        pr,mr=reconstruct(plus),reconstruct(minus)
        derivative=(plus['Observations'][0]['RotorTorque']-minus['Observations'][0]['RotorTorque'])/(2*h)
        row=24
        S['close'](derivative,basis['working_weighted_81_columns'][label][row],POLICY['torque_tolerance_nm'],'Stored first-step torque derivative differs')
        components.append(dict(direction=label,coefficient=coefficient,plus=pr,minus=mr,
                               first_step_torque_derivative=derivative,linear_full_step_torque_change_nm=coefficient*derivative))
    results=[]
    for trial in analysis['proposals']:
        fraction=trial['step_fraction'];record=records[f'proposal/{fraction:g}/repeat/1'];physical=reconstruct(record)
        predicted=math.fsum(c['linear_full_step_torque_change_nm'] for c in components)*fraction
        S['close'](predicted,trial['predicted_change_weighted_81'][24],POLICY['torque_tolerance_nm'],'Stored combined first-step prediction differs')
        observed=physical['reconstructed_net_torque_nm']-baseline['reconstructed_net_torque_nm']
        S['close'](observed,trial['observed_change_weighted_81'][24],POLICY['torque_tolerance_nm'],'Stored observed torque change differs')
        results.append(dict(step_fraction=fraction,capacity_reconstruction=physical,previous_recorded_guards=trial['guards'],
                            first_step_predicted_torque_change_nm=predicted,first_step_observed_torque_change_nm=observed,
                            first_step_affine_error_nm=observed-predicted,winning_capacity_changed=physical['winner']!=baseline['winner']))
    return dict(status='PASS-REPAIRED-FLOW-MINIMUM-ATTRIBUTION',fixtures=fixtures(),runtime_steps=0,
                baseline=baseline,active_components=components,proposals=results,
                earlier_trial_rewritten=False,later_checkpoints_attributed=False,
                interpretation='Central secants span different winners of the stage-flow minimum; their affine sum fails at the first seed step while the exact minimum law reproduces the measurements',
                limits=['Only the first seed uses the verified common initial valve command; no claim for later actuator states',
                        'This diagnoses an omitted flow-limiter branch, not a new physics defect or a qualified piecewise model'],
                family_selected=False,root_observed=False,r3_passed=False,m10_closed=False)

def execute():
    authenticate();P.require(not (OUT/'01-analysis.json').exists(),'Existing review retained')
    result=analyze();P.write(OUT,'01-analysis.json',result)
    P.freeze(ID,['01-analysis.json']);return result['status']

def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);result=analyze();P.require(result==json.loads(data['01-analysis.json']),'Replay drift')
    return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','execute','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
