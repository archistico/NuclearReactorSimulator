"""Prospective source/thermal-state diagnostic from the measured full-cycle power deficit."""
from __future__ import annotations
from functools import lru_cache
import json
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
R=runpy.run_path(str(ROOT/'eng/run-r3-repaired-exhaust-branch18.py'))
Q,S,D,C=R['Q'],R['S'],R['D'],R['C']
ID='r3-repaired-cycle-power19';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT='eng/diagnostics/R3CyclePowerObservation/R3CyclePowerObservation.csproj'
BINARY=ROOT/'eng/diagnostics/R3CyclePowerObservation/bin/Release/net10.0/R3CyclePowerObservation.dll'
POLICY=dict(maximum_recipes=8,maximum_batches=1,maximum_batch_recipes=8,maximum_runtime_steps=26,repeats=2,
            source_fractions=[.5,1.],neutron_probe_upper_limit=.03,temperature_fraction_upper_limit=.003,
            temperature_convention='fraction of the authored Celsius values, same as the historical harness',
            prediction='first-checkpoint net external power changes by initial nuclear power times neutron fraction; temperatures preserve initial thermal transfer balance',
            relative_prediction_tolerance=.01,energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
            all_inventory_coordinates='fixed to the selected local point 18, except for the unchanged historical baseline',
            diagnostic_only=True,old_domains_unchanged=True,integrated_search=False,production_changes=0,root_criterion_unchanged=True,
            seed_installation=False,r3_passed=False,m10_closed=False)

@lru_cache(maxsize=1)
def plan():
    data=S['frozen'](R['ID']);analysis=json.loads(data['05-analysis.json'])
    P.require(analysis['local_first_checkpoint_balanced'],'Parent local balance not achieved')
    records={r['Id']:r for b in map(json.loads,data['03-batches.jsonl'].splitlines()) for r in b['records']}
    selected=records[analysis['selected_record']];first=selected['Observations'][0];power=first['CyclePower'];heat=first['Extra']['ChannelHeat'][0]
    source=-power['NetExternalW'];fraction=source/power['NuclearW']
    ports={h['FromDomainId']:h for h in first['Extra']['HeatTransfers']}
    fuel=source*heat['FuelW']/power['NuclearW']/ports['fuel']['ConductanceWK']/305.62514906467646
    structure=source*heat['StructureW']/power['NuclearW']/ports['structure']['ConductanceWK']/289.13956081139787
    P.require(0<fraction<=.03 and 0<fuel<=.003 and 0<structure<=.003,'Source/temperature proposal outside explicit domain')
    return dict(local_record=selected,baseline_record=records['base/repeat/1'],source_increment_w=source,neutron_fraction=fraction,
                operating_fractions=[0.,0.,0.,fraction,fuel,structure],baseline_net_power_w=power['NetExternalW'],nuclear_reference_w=power['NuclearW'])

def requests():
    p=plan();points=[dict(Id='base',Coordinates=[0.]*24,OperatingPointFractions=[0.]*6),
                    dict(Id='local',Coordinates=p['local_record']['Coordinates'],OperatingPointFractions=[0.]*6)]
    for f in POLICY['source_fractions']:
        points.append(dict(Id=f'source/{f:g}',Coordinates=p['local_record']['Coordinates'],OperatingPointFractions=[f*x for x in p['operating_fractions']]))
    return [{**point,'Id':point['Id']+f'/repeat/{repeat}'} for point in points for repeat in (1,2)]

def fixtures():
    P.require(len(requests())==8 and len({r['Id'] for r in requests()})==8,'Budget fixture')
    p=plan();P.require(abs(p['baseline_net_power_w']+p['source_increment_w'])<1e-8,'First-law prediction fixture')
    P.require(all(r['Coordinates']==p['local_record']['Coordinates'] for r in requests() if not r['Id'].startswith('base/')),'Fixed-inventory fixture')
    return 'PASS-CYCLE-POWER-19-FIXTURES'

def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for e in c['implementation_files']+c['frozen_inputs']:P.require(P.digest((ROOT/e['path']).read_bytes())==e['sha256'],'Input drift: '+e['path'])

def preexecution():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists() and not (OUT/'batch-001').exists(),'Existing execution retained')
    P.require(BINARY.exists() and (OUT/'00-build.log').exists(),'Build diagnostic first')
    with (OUT/'00-domain-fixtures.log').open('w',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),'--self-test'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    p=plan();P.write(OUT,'01-prediction.json',{k:v for k,v in p.items() if not k.endswith('_record')})
    token=dict(status='PASS-PREEXECUTION',contract_sha256=P.digest(CONTRACT.read_bytes()),fixtures=fixtures(),runtime_steps=0,recipes=8,
               assemblies=[dict(file=x.name,sha256=P.digest(x.read_bytes())) for x in sorted(BINARY.parent.glob('*.dll'))])
    for folder in (OUT,OUT/'batch-001'):
        P.write(folder,'01-requests.json',requests());P.write(folder,'02-preexecution.json',dict(**token,requests_sha256=P.digest((folder/'01-requests.json').read_bytes())))
    with (OUT/'preflight.log').open('w',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'1','--validate-only'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    return 'PASS-PREEXECUTION'

def analyze(records):
    expected=requests();first=Q['audit_pairs'](records,expected);points={r['Id'].rsplit('/repeat/',1)[0]:r for r in first};p=plan()
    for key,parent in (('base',p['baseline_record']),('local',p['local_record'])):
        P.require({k:v for k,v in points[key].items() if k!='Id'}=={k:v for k,v in parent.items() if k!='Id'},'Parent record identity drift')
    local=points['local'];proposals=[]
    for f in POLICY['source_fractions']:
        r=points[f'source/{f:g}'];guards=D['reasons'](r,points['base'])
        if r['Residuals'] is None:
            proposals.append(dict(source_fraction_of_proposal=f,guards=guards,prediction_conforming=False,rejection=r['Reason']));continue
        observed=r['Observations'][0]['CyclePower']['NetExternalW']
        delta=observed-p['baseline_net_power_w'];predicted=f*p['source_increment_w'];error=abs(delta-predicted)/abs(predicted)
        rows=Q['V']['physical_rows'](r);thermal=[]
        for o in r['Observations']:
            thermal.append(dict(checkpoint=o['Checkpoint'],rates_w={a['Id']:(a['EnergyJ']-b['EnergyJ'])/.01
                for a,b in zip(o['Extra']['ThermalAfter'],o['Extra']['ThermalBefore'])}))
        proposals.append(dict(source_fraction_of_proposal=f,operating_fractions=r['OperatingPointFractions'],guards=guards,
                              predicted_first_net_power_w=p['baseline_net_power_w']+predicted,observed_first_net_power_w=observed,
                              relative_change_prediction_error=error,prediction_conforming=guards['available'] and error<=POLICY['relative_prediction_tolerance'],
                              exhaust_checkpoints=rows,thermal_checkpoints=thermal,
                              cycle_powers=[o['CyclePower'] for o in r['Observations']],
                              maximum_original_scaled_residual=max(map(abs,r['Residuals']))))
    return dict(status='PASS-REPAIRED-CYCLE-POWER-19-EVIDENCE',fixtures=fixtures(),recipes=8,runtime_steps=26,exact_repeats=True,parent_records_identical=True,
                prediction={k:v for k,v in p.items() if not k.endswith('_record')},proposals=proposals,
                all_predictions_conforming=all(r['prediction_conforming'] for r in proposals),
                maximum_global_energy_ledger_error_w=max(abs(e) for r in records for e in Q['audit_power'](r)),
                integrated_root_observed=any(r['Residuals'] is not None and max(map(abs,r['Residuals']))<=1 for r in first),integrated_search_started=False,
                seed_installed=False,family_selected=False,r3_passed=False,m10_closed=False,
                limits=['Only the first-checkpoint global power response is predicted; full stationarity requires every inventory and checkpoint',
                        'The neutron-population extension is diagnostic and does not change the old two-percent domains or controller gains',
                        'No new seed, hundred-step confirmation or R3 qualification is performed'])

def execute():
    authenticate();P.require(not (OUT/'03-measurements.jsonl').exists(),'Existing execution retained')
    with (OUT/'execution.log').open('x',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),str(ROOT),ID,'1'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    (OUT/'03-measurements.jsonl').write_bytes((OUT/'batch-001/03-measurements.jsonl').read_bytes())
    return adjudicate()

def adjudicate():
    authenticate();P.require(P.load(OUT/'01-requests.json')==requests(),'Prospective request drift')
    result=analyze([json.loads(s) for s in (OUT/'03-measurements.jsonl').read_text(encoding='utf-8').splitlines()]);P.write(OUT,'04-analysis.json',result);return result['status']

def freeze():
    adjudicate();P.freeze(ID,['00-build.log','00-domain-fixtures.log','01-prediction.json','01-requests.json','02-preexecution.json','03-measurements.jsonl','04-analysis.json','execution.log'])
    return 'PASS-FROZEN-CYCLE-POWER-19'

def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);P.require(json.loads(data['01-requests.json'])==requests(),'Frozen request drift')
    result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()]);P.require(result==json.loads(data['04-analysis.json']),'Frozen analysis drift')
    return result['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
