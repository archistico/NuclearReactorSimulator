"""Observe the stage-flow capacities of the 22 working directions that observation 8 did not cover."""
from __future__ import annotations
from functools import lru_cache
import json
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
R=runpy.run_path(str(ROOT/'eng/review-r3-repaired-capacity-minimum-model9.py'))
C,M,F,D,S,T=R['C'],R['M'],R['F'],R['D'],R['S'],R['C']['T']
ID='r3-repaired-capacity-observation11';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT,BINARY=C['PROJECT'],C['BINARY']
CAPACITIES,CHECKPOINTS=C['CAPACITIES'],C['CHECKPOINTS']
POLICY=dict(recipes=90,repeats=2,maximum_runtime_steps=272,central_directions=18,one_sided_directions=4,
            parents='frozen probes of the 22 working directions without observed capacities, identical requests: both signs at the sampled '
                    'amplitude for the 18 central directions; the positive sign at the sampled and at the doubled amplitude for the four '
                    'one-sided directions; plus the baseline',
            one_sided_negative_probes='not repeated: their frozen records change the phase of hotwell or suction',
            one_sided_amplitude_factors=[1.,2.],
            parent_record_identity='exact after removing only the additive StageFlow block',
            baseline='identical to the frozen observation-8 baseline record, StageFlow included',
            reconstruction='observation-8 reconstruction and tolerances, unchanged; a record outside its scope is reported, not repaired',
            near_flow_change_kg_s=1e-9,
            capacity_differences='descriptive only; no model, prediction, proposal or validation',
            models=0,proposals=0,searches=0,production_changes=0,root_criterion_unchanged=True,
            previous_results_rewritten=False,r3_passed=False,m10_closed=False)


@lru_cache(maxsize=1)
def lineage():
    observation,records8,_=R['inputs']();_,_,probes=M['inputs']();basis,_=F['basis']()
    observed={d['direction'] for d in observation['active_directions']};parents=[probes['base/repeat/1'],probes['base/repeat/2']];directions=[]
    for label in sorted(basis['working_directions']):
        if label in observed:continue
        d=basis['working_directions'][label];h=d['sampled_amplitude'];one_sided=d['coefficient_sign']=='nonnegative'
        P.require(one_sided==(label in basis['one_sided_directions']),'One-sided direction drift')
        ids=[f'{label}/plus/{f*h:g}' for f in POLICY['one_sided_amplitude_factors']] if one_sided else [f'{label}/{tag}/{h:g}' for tag in ('plus','minus')]
        directions.append(dict(direction=label,amplitude=h,one_sided=one_sided,ids=ids))
        parents+=[probes[f'{i}/repeat/{repeat}'] for i in ids for repeat in (1,2)]
    P.require(len(directions)==POLICY['central_directions']+POLICY['one_sided_directions']
              and sum(d['one_sided'] for d in directions)==POLICY['one_sided_directions'],'Direction count drift')
    return parents,directions,records8['base']


def requests():
    return [dict(Id=r['Id'],Coordinates=r['Coordinates'],OperatingPointFractions=r['OperatingPointFractions']) for r in lineage()[0]]


def observe(record):
    """Observation-8 reconstruction; a record outside its declared scope is reported with the failed check."""
    try:return C['reconstruct'](record),None
    except Exception as failure:return None,str(failure)


def differences(rows,base,amplitudes):
    """Capacity and flow changes from the baseline, per unit coordinate, for each observed point of one direction."""
    return [dict(checkpoint=b['checkpoint'],
                 capacity_change_per_unit_kg_s=[dict((k,(r[i]['capacities_kg_s'][k]-b['capacities_kg_s'][k])/a) for k in CAPACITIES) for r,a in zip(rows,amplitudes)],
                 commanded_flow_change_kg_s=[r[i]['commanded_flow_kg_s']-b['commanded_flow_kg_s'] for r in rows],
                 winners=[r[i]['winner'] for r in rows],baseline_winner=b['winner'],
                 same_winner=all(r[i]['winner']==b['winner'] for r in rows)) for i,b in enumerate(base)]


def fixtures():
    P.require(len(requests())==POLICY['recipes']==len({r['Id'] for r in requests()}),'Budget fixture')
    P.require(POLICY['recipes']==2*(1+2*POLICY['central_directions']+len(POLICY['one_sided_amplitude_factors'])*POLICY['one_sided_directions'])
              and POLICY['maximum_runtime_steps']==2+3*POLICY['recipes'],'Runtime-step fixture')
    row=lambda c,w,f:dict(checkpoint='seed-step1',capacities_kg_s=dict(zip(CAPACITIES,c)),winner=w,commanded_flow_kg_s=f)
    d=differences([[row([3.,2.,5.,5.,5.],'control',2.)],[row([1.,2.,5.,5.,5.],'stop',1.)]],[row([2.,2.,5.,5.,5.],'stop',2.)],[.5,-.5])[0]
    P.require(d['capacity_change_per_unit_kg_s'][0]['stop']==2. and d['capacity_change_per_unit_kg_s'][1]['stop']==2.
              and d['commanded_flow_change_kg_s']==[0.,-1.] and not d['same_winner'],'Capacity-difference fixture')
    P.require(observe(dict(Raw=[],Observations=[dict(Checkpoint='other')]))[0] is None,'Out-of-scope fixture')
    return C['fixtures']()+'; PASS-CAPACITY-OBSERVATION-11-FIXTURES'


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
    expected=requests();parents,directions,base8=lineage()
    P.require(len(records)==len(expected)==POLICY['recipes'],'Recipe count drift')
    plan,_,_=S['layout']();points={};energy_errors=[];torque_errors=[]
    for i in range(0,len(records),2):
        pair=records[i:i+2]
        for r,request,parent in zip(pair,expected[i:i+2],parents[i:i+2]):
            P.require(all(r[k]==request[k] for k in request),'Request/response drift')
            P.require(r['Residuals'] is not None and r['Reason']=='VALID','Parent-valid recipe became unavailable')
            P.require(C['strip'](r)==parent,'Parent record drift outside the additive StageFlow block')
            P.H3['H2']['verify_physics'](r,plan);energy_errors+=S['verify_extra'](r);torque_errors+=T['audit_mechanical'](r)
        P.require({k:v for k,v in pair[0].items() if k!='Id'}=={k:v for k,v in pair[1].items() if k!='Id'},'Repeatability drift')
        points[pair[0]['Id'].rsplit('/repeat/',1)[0]]=pair[0]
    base=points['base']
    P.require({k:v for k,v in base.items() if k!='Id'}=={k:v for k,v in base8.items() if k!='Id'},'Observation-8 baseline drift')
    base_rows,failure=observe(base);P.require(failure is None,'Baseline outside the reconstruction scope')
    observed={'base':base_rows};unavailable={};rows=[]
    for d in directions:
        guards={i:D['reasons'](points[i],base) for i in d['ids']};reconstructed={}
        for i in d['ids']:
            result,failure=observe(points[i])
            if failure is None:reconstructed[i]=observed[i]=result
            else:unavailable[i]=failure
        complete=len(reconstructed)==len(d['ids'])
        row=dict(direction=d['direction'],amplitude=d['amplitude'],one_sided=d['one_sided'],records=d['ids'],guards=guards,
                 all_guards_available=all(g['available'] and not g['phase_changes'] for g in guards.values()),capacities_observed=complete)
        if complete:
            sign=[f*d['amplitude'] for f in POLICY['one_sided_amplitude_factors']] if d['one_sided'] else [d['amplitude'],-d['amplitude']]
            series=[reconstructed[i] for i in d['ids']];row['checkpoints']=differences(series,base_rows,sign)
            if d['one_sided']:
                row['second_difference_kg_s']=[dict((k,series[1][c]['capacities_kg_s'][k]-2*series[0][c]['capacities_kg_s'][k]+base_rows[c]['capacities_kg_s'][k])
                                                    for k in CAPACITIES) for c in range(len(CHECKPOINTS))]
            else:
                row['flow_sum_kg_s']=[series[0][c]['commanded_flow_kg_s']+series[1][c]['commanded_flow_kg_s']-2*base_rows[c]['commanded_flow_kg_s']
                                      for c in range(len(CHECKPOINTS))]
            row['maximum_flow_change_kg_s']=max(abs(x) for c in row['checkpoints'] for x in c['commanded_flow_change_kg_s'])
            row['winner_changes']=[c['checkpoint'] for c in row['checkpoints'] if not c['same_winner']]
        rows.append(row)
    flat=[r for label in observed for r in observed[label]];done=[r for r in rows if r['capacities_observed']]
    return dict(status='PASS-REPAIRED-CAPACITY-OBSERVATION-11-EVIDENCE',fixtures=fixtures(),recipes=POLICY['recipes'],
                maximum_runtime_steps=POLICY['maximum_runtime_steps'],exact_repeats=True,parent_records_identical=True,baseline_matches_observation8=True,
                maximum_energy_ledger_error_w=max(map(abs,energy_errors)),maximum_torque_ledger_error_nm=max(map(abs,torque_errors)),
                observations=observed,winners_by_checkpoint={label:[r['winner'] for r in observed[label]] for label in observed},
                reconstruction_unavailable=unavailable,directions=rows,
                summary=dict(directions=len(rows),directions_observed=len(done),
                             directions_with_all_guards=sum(r['all_guards_available'] for r in rows),
                             exact_minimum_reconstructions=sum(r['exact_minimum'] for r in flat),reconstructions=len(flat),
                             maximum_flow_error_kg_s=max(abs(r['flow_error_kg_s']) for r in flat),
                             maximum_pressure_error_pa=max(r['maximum_pressure_error_pa'] for r in flat),
                             maximum_valve_flow_error_kg_s=max(r['maximum_valve_flow_error_kg_s'] for r in flat),
                             winner_counts={k:sum(r['winner']==k for r in flat) for k in CAPACITIES},
                             minimum_margin_by_checkpoint={c:min(observed[label][i]['margin_to_next_kg_s'] for label in observed) for i,c in enumerate(CHECKPOINTS)},
                             directions_changing_winner={r['direction']:r['winner_changes'] for r in done if r['winner_changes']},
                             directions_changing_flow=sorted(r['direction'] for r in done if r['maximum_flow_change_kg_s']>POLICY['near_flow_change_kg_s']),
                             directions_not_changing_flow=sorted(r['direction'] for r in done if r['maximum_flow_change_kg_s']<=POLICY['near_flow_change_kg_s']),
                             maximum_flow_change_kg_s={r['direction']:r['maximum_flow_change_kg_s'] for r in done}),
                model_built=False,proposal_executed=False,search_started=False,family_selected=False,root_observed=False,r3_passed=False,m10_closed=False,
                limits=['Capacity differences are descriptive; a difference across different winners is not a derivative of the stage flow',
                        'No model is fitted or validated here; model 9 and the proposals of trial 10 still cover eight directions only',
                        'One-sided directions are observed on the positive side only; their negative probes change phase',
                        'Only the 90 parent recipes are observed; no new direction, amplitude or proposal'])


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
    return 'PASS-FROZEN-CAPACITY-OBSERVATION-11'


def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);result=analyze([json.loads(s) for s in data['03-measurements.jsonl'].splitlines()])
    P.require(json.loads(json.dumps(result))==json.loads(data['04-analysis.json']),'Replay drift')
    return result['status']+' (offline; zero runtime steps)'


if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
