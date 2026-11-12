"""Bounded two-inventory first-checkpoint reconstruction with full-cycle power observation."""
from __future__ import annotations
import json
import math
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
V=runpy.run_path(str(ROOT/'eng/run-r3-repaired-exhaust-domain16.py'))
C,S,D,T=V['C'],V['S'],V['D'],V['T']
ID='r3-repaired-exhaust-balance17';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT='eng/diagnostics/R3ExhaustBalanceObservation/R3ExhaustBalanceObservation.csproj'
BINARY=ROOT/'eng/diagnostics/R3ExhaustBalanceObservation/bin/Release/net10.0/R3ExhaustBalanceObservation.dll'
POLICY=dict(maximum_recipes=84,maximum_batches=26,maximum_batch_recipes=8,maximum_runtime_steps=304,repeats=2,
            initial_point=[-.008,.035],coordinate_indices=[4,5],bounds=[[-.02,.02],[0.,.04]],
            derivative_steps=[1e-5,1e-5],maximum_iterations=5,maximum_halvings=3,
            first_checkpoint_mass_tolerance_kg_s=1e-6,first_checkpoint_energy_tolerance_w=.01,
            energy_ledger_tolerance_w=.01,mass_ledger_tolerance_kg_s=1e-6,torque_ledger_tolerance_nm=1e-6,
            determinant_relative_floor=1e-12,acceptance='same original guards and capacity reconstruction, strict decrease of mass-kg/s and energy-MW Euclidean norm',
            local_reconstruction=True,integrated_search=False,production_changes=0,root_criterion_unchanged=True,
            previous_results_rewritten=False,seed_installation=False,r3_passed=False,m10_closed=False)

def inside(x):return all(math.isfinite(v) and lo<=v<=hi for v,(lo,hi) in zip(x,POLICY['bounds']))
def norm(y):return math.hypot(*y)
def balanced(y):return abs(y[0])<=POLICY['first_checkpoint_mass_tolerance_kg_s'] and abs(y[1])*1e6<=POLICY['first_checkpoint_energy_tolerance_w']

def reconstruct(evaluate,describe):
    """All adaptive requests are generated here, identically for live acquisition and replay."""
    base,current=evaluate([('base',[0.,0.]),('start',POLICY['initial_point'])])
    x=POLICY['initial_point'][:];info=describe(current,base);history=[]
    if not info['valid']:return dict(stop='INITIAL-GUARD-REJECTION',point=x,current=current['Id'],history=history)
    for iteration in range(POLICY['maximum_iterations']+1):
        y=info['value'];history.append(dict(iteration=iteration,point=x[:],value=y,record=current['Id']))
        if balanced(y):return dict(stop='FIRST-CHECKPOINT-NODE-BALANCED',point=x,current=current['Id'],history=history)
        if iteration==POLICY['maximum_iterations']:break
        requests=[]
        for j,h in enumerate(POLICY['derivative_steps']):
            for sign,tag in ((1,'plus'),(-1,'minus')):
                p=x[:];p[j]+=sign*h
                if not inside(p):return dict(stop='DERIVATIVE-OUTSIDE-DOMAIN',point=x,current=current['Id'],history=history)
                requests.append((f'iteration/{iteration+1}/difference/{j}/{tag}',p))
        samples=evaluate(requests);observations=[describe(r,base) for r in samples]
        if not all(o['valid'] for o in observations):return dict(stop='DERIVATIVE-GUARD-REJECTION',point=x,current=current['Id'],history=history)
        columns=[[(observations[2*j]['value'][i]-observations[2*j+1]['value'][i])/(2*h) for i in range(2)]
                 for j,h in enumerate(POLICY['derivative_steps'])]
        a,c=columns[0];b,d=columns[1];det=a*d-b*c
        if abs(det)<=POLICY['determinant_relative_floor']*max(abs(a*d),abs(b*c),1e-30):
            return dict(stop='SINGULAR-LOCAL-MODEL',point=x,current=current['Id'],history=history)
        delta=[(-d*y[0]+b*y[1])/det,(c*y[0]-a*y[1])/det]
        history[-1].update(jacobian_columns=columns,newton_delta=delta,trial_results=[])
        accepted=False
        for halving in range(POLICY['maximum_halvings']+1):
            fraction=.5**halving;proposal=[v+fraction*p for v,p in zip(x,delta)]
            if not inside(proposal):
                history[-1]['trial_results'].append(dict(fraction=fraction,point=proposal,reason='OUTSIDE-DOMAIN'));continue
            trial=evaluate([(f'iteration/{iteration+1}/trial/{halving}',proposal)])[0];diagnostic=describe(trial,base)
            entry=dict(fraction=fraction,point=proposal,record=trial['Id'],valid=diagnostic['valid'])
            if diagnostic['valid']:entry['value']=diagnostic['value']
            history[-1]['trial_results'].append(entry)
            if diagnostic['valid'] and norm(diagnostic['value'])<norm(y):
                x,current,info=proposal,trial,diagnostic;accepted=True;entry['accepted']=True;break
        if not accepted:return dict(stop='NO-ADMISSIBLE-DESCENT',point=x,current=current['Id'],history=history)
    return dict(stop='ITERATION-BUDGET',point=x,current=current['Id'],history=history)

def fixtures():
    evaluated=[]
    def evaluate(points):
        evaluated.extend(p for _,p in points)
        return [dict(Id=tag+'/repeat/1',point=p) for tag,p in points]
    def describe(record,_):
        x=record['point'];v=x[1]-.0357
        return dict(valid=True,value=[2*(x[0]+.0065),v+10*v*v])
    result=reconstruct(evaluate,describe)
    P.require(result['stop']=='FIRST-CHECKPOINT-NODE-BALANCED' and all(inside(p) for p in evaluated),'Nonlinear local-reconstruction fixture')
    singular=reconstruct(evaluate,lambda r,_:dict(valid=True,value=[r['point'][0],2*r['point'][0]]))
    P.require(singular['stop']=='SINGULAR-LOCAL-MODEL','Singular fixture')
    rejected=reconstruct(evaluate,lambda r,_:dict(valid=False))
    P.require(rejected['stop']=='INITIAL-GUARD-REJECTION','Guard fixture')
    p=dict(NuclearW=1000.,PrimaryBoundaryW=0.,GenericHeatW=0.,PumpHydraulicW=-25.,FeedwaterConditioningW=0.,
           CondenserW=500.,TurbineShaftW=275.,GeneratorMechanicalW=150.,ElectricalW=100.,GeneratorLossW=50.,PassiveLossW=25.,
           ThermofluidExpectedW=200.,NetExternalW=300.,StoredEnergyChangeJ=3.,InitialThermofluidJ=0.,FinalThermofluidJ=2.,
           InitialRotorJ=0.,FinalRotorJ=1.,ExternalMassKgS=0.)
    fixture=dict(Residuals=[0.],Raw=[dict(Id='fixture',Mass=1.,Energy=0.)],
                 Observations=[dict(Nodes=[dict(Id='fixture',Mass=1.,Energy=2.)],Extra=dict(ThermalBefore=[],ThermalAfter=[]),CyclePower=p)])
    P.require(max(map(abs,audit_power(fixture)))==0,'Global first-law fixture')
    bad=json.loads(json.dumps(fixture));bad['Observations'][0]['CyclePower']['NuclearW']=1100.
    try:audit_power(bad)
    except ValueError:pass
    else:raise ValueError('Power ownership mismatch accepted')
    P.require(POLICY['maximum_recipes']==4+5*(8+4*2) and POLICY['maximum_batches']==1+5*(1+4)
              and POLICY['maximum_runtime_steps']==3*84+2*26,'Worst-case budget fixture')
    return 'PASS-EXHAUST-BALANCE-17-FIXTURES'

def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for e in c['implementation_files']+c['frozen_inputs']:P.require(P.digest((ROOT/e['path']).read_bytes())==e['sha256'],'Input drift: '+e['path'])

def request(tag,point,repeat):
    coordinates=[0.]*24
    for j,value in zip(POLICY['coordinate_indices'],point):coordinates[j]=value
    return dict(Id=tag+f'/repeat/{repeat}',Coordinates=coordinates,OperatingPointFractions=[0.]*6)

def strip(record):
    result=json.loads(json.dumps(record))
    for o in result.get('Observations',[]):o.pop('CyclePower')
    return result

def audit_power(record):
    errors=[];previous={n['Id']:n for n in record['Raw']}
    if record['Residuals'] is None:return errors
    for o in record['Observations']:
        now={n['Id']:n for n in o['Nodes']};p=o['CyclePower']
        fluid=math.fsum((n['Energy']-previous[n['Id']]['Energy'])/.01 for n in now.values())
        thermal=math.fsum((a['EnergyJ']-b['EnergyJ'])/.01 for a,b in zip(o['Extra']['ThermalAfter'],o['Extra']['ThermalBefore']))
        rotor=(p['FinalRotorJ']-p['InitialRotorJ'])/.01
        external=math.fsum([p['NuclearW'],p['PrimaryBoundaryW'],p['GenericHeatW'],p['PumpHydraulicW'],p['FeedwaterConditioningW'],
                           -p['CondenserW'],-p['ElectricalW'],-p['GeneratorLossW'],-p['PassiveLossW']])
        S['close'](math.fsum((n['Mass']-previous[n['Id']]['Mass'])/.01 for n in now.values()),p['ExternalMassKgS'],
                   POLICY['mass_ledger_tolerance_kg_s'],'Full-cycle mass ownership')
        for actual,expected,label in ((external,p['NetExternalW'],'External power ownership'),
            (external+p['ElectricalW']+p['GeneratorLossW']+p['PassiveLossW']-p['TurbineShaftW'],p['ThermofluidExpectedW'],'Thermofluid power ownership'),
            (p['GeneratorMechanicalW'],p['ElectricalW']+p['GeneratorLossW'],'Electrical conversion ownership'),
            (p['TurbineShaftW']-p['GeneratorMechanicalW']-p['PassiveLossW'],rotor,'Rotor kinetic-energy ownership'),
            (fluid+thermal+rotor,p['StoredEnergyChangeJ']/.01,'Conserved storage ownership'),
            (fluid+thermal+rotor,external,'Full-cycle first law'),
            ((p['FinalThermofluidJ']-p['InitialThermofluidJ'])/.01,fluid+thermal,'Thermofluid storage ownership')):
            errors.append(S['close'](actual,expected,POLICY['energy_ledger_tolerance_w'],label))
        previous=now
    return errors

def audit_pairs(records,expected):
    P.require(len(records)==len(expected),'Batch response count drift');plan,_,_=S['layout']();first=[]
    for i in range(0,len(records),2):
        pair=records[i:i+2]
        for r,sent in zip(pair,expected[i:i+2]):
            P.require(all(r[k]==sent[k] for k in sent),'Request/response drift')
            P.H3['H2']['verify_physics'](r,plan)
            if r['Residuals'] is not None:S['verify_extra'](r);T['audit_mechanical'](r);audit_power(r)
        P.require({k:v for k,v in pair[0].items() if k!='Id'}=={k:v for k,v in pair[1].items() if k!='Id'},'Repeatability drift')
        first.append(pair[0])
    return first

def describe(record,base):
    guards=D['reasons'](record,base)
    if not guards['available']:return dict(valid=False,guards=guards)
    try:C['reconstruct'](record)
    except Exception as e:return dict(valid=False,guards=guards,capacity_failure=str(e))
    before=next(n for n in record['Raw'] if n['Id']=='exhaust');after=next(n for n in record['Observations'][0]['Nodes'] if n['Id']=='exhaust')
    return dict(valid=True,value=[(after['Mass']-before['Mass'])/.01,(after['Energy']-before['Energy'])/.01/1e6],guards=guards)

def preexecution():
    authenticate();P.require(not (OUT/'03-batches.jsonl').exists(),'Existing execution retained')
    P.require(BINARY.exists() and (OUT/'00-build.log').exists(),'Build diagnostic first')
    with (OUT/'00-domain-fixtures.log').open('w',encoding='utf-8') as log:
        subprocess.run(['dotnet',str(BINARY),'--self-test'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    P.write(OUT,'02-preexecution.json',dict(status='PASS-PREEXECUTION',contract_sha256=P.digest(CONTRACT.read_bytes()),fixtures=fixtures(),runtime_steps=0,
        assemblies=[dict(file=p.name,sha256=P.digest(p.read_bytes())) for p in sorted(BINARY.parent.glob('*.dll'))]))
    return 'PASS-PREEXECUTION'

class Batches:
    def __init__(self,replay=None):self.replay=replay;self.cursor=0;self.recipes=0;self.records=[]
    def evaluate(self,points):
        self.cursor+=1;expected=[request(tag,x,r) for tag,x in points for r in (1,2)]
        self.recipes+=len(expected)
        P.require(len(expected)<=8 and self.cursor<=26 and self.recipes<=84,'Physical budget exceeded')
        if self.replay is not None:
            P.require(self.cursor<=len(self.replay),'Missing frozen batch')
            batch=self.replay[self.cursor-1]
            P.require(batch['batch']==self.cursor and batch['requests']==expected,'Adaptive request replay drift')
            records=batch['records']
        else:
            folder=OUT/f'batch-{self.cursor:03d}';P.require(not folder.exists(),'Batch directory already exists')
            P.write(folder,'01-requests.json',expected);token=P.load(OUT/'02-preexecution.json')
            P.require(token['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Preexecution token drift')
            P.write(folder,'02-preexecution.json',dict(**token,requests_sha256=P.digest((folder/'01-requests.json').read_bytes()),recipes=len(expected)))
            args=['dotnet',str(BINARY),str(ROOT),ID,str(self.cursor)]
            with (folder/'execution.log').open('x',encoding='utf-8') as log:
                subprocess.run(args+['--validate-only'],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
                subprocess.run(args,cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
            records=[json.loads(s) for s in (folder/'03-measurements.jsonl').read_text(encoding='utf-8').splitlines()]
            with (OUT/'03-batches.jsonl').open('a',encoding='utf-8') as log:log.write(json.dumps(dict(batch=self.cursor,requests=expected,records=records),allow_nan=False)+'\n')
        result=audit_pairs(records,expected);self.records.extend(records)
        if self.replay is None:print(f'batch={self.cursor} repeated_recipes={self.recipes}',flush=True)
        return result

def execute():
    authenticate();OUT.mkdir(parents=True,exist_ok=True)
    with (OUT/'03-batches.jsonl').open('x',encoding='utf-8'):pass
    batches=Batches();result=reconstruct(batches.evaluate,describe)
    P.write(OUT,'04-local-result.json',result)
    return adjudicate()

def adjudication(batches,result):
    records=batches.records;by_id={r['Id']:r for r in records};base=by_id['base/repeat/1'];current=by_id[result['current']]
    parent=S['frozen'](V['ID']);parents={r['Id']:r for r in map(json.loads,parent['03-measurements.jsonl'].splitlines())}
    for repeat in (1,2):P.require(strip(by_id[f'base/repeat/{repeat}'])==parents[f'base/repeat/{repeat}'],'Parent baseline drift beyond additive CyclePower block')
    rows=V['physical_rows'](current);budgets=[];previous={n['Id']:n for n in current['Raw']}
    for o,row in zip(current['Observations'],rows):
        p=o['CyclePower'];n={a['Id']:a for a in o['Nodes']}
        fluid={key:(a['Energy']-previous[key]['Energy'])/.01 for key,a in n.items()}
        budgets.append(dict(checkpoint=o['Checkpoint'],node=row,powers=p,fluid_storage_rates_w=fluid,
                            source_increment_if_other_powers_fixed_w=-p['NetExternalW'],
                            source_increment_fraction_if_other_powers_fixed=-p['NetExternalW']/p['NuclearW']))
        previous=n
    return dict(status='PASS-REPAIRED-EXHAUST-BALANCE-17-EVIDENCE',fixtures=fixtures(),stop=result['stop'],result=result,
                exact_repeats=True,parent_baseline_identical=True,recipes=batches.recipes,batches=batches.cursor,
                runtime_steps=3*batches.recipes+2*batches.cursor,
                local_first_checkpoint_balanced=result['stop']=='FIRST-CHECKPOINT-NODE-BALANCED',
                selected_raw_coordinates=current['Coordinates'],selected_record=current['Id'],
                maximum_global_energy_ledger_error_w=max(abs(x) for r in records for x in audit_power(r)),
                selected_capacities=C['reconstruct'](current),checkpoints=budgets,
                maximum_original_scaled_residual=max(map(abs,current['Residuals'])),
                integrated_root_observed=max(map(abs,current['Residuals']))<=1,integrated_search_started=False,
                seed_installed=False,family_selected=False,r3_passed=False,m10_closed=False,
                limits=['The local target is exhaust mass/energy at the first seed checkpoint only, not the 75-residual equilibrium',
                        'The inferred source increment holds all other powers fixed; it is not a new recipe or a controller adjustment',
                        'Only the declared two-inventory diagnostic domain is explored; no old search bound is changed'])

def adjudicate():
    authenticate();trace=[json.loads(s) for s in (OUT/'03-batches.jsonl').read_text(encoding='utf-8').splitlines()]
    batches=Batches(trace);result=reconstruct(batches.evaluate,describe)
    P.require(batches.cursor==len(trace) and result==P.load(OUT/'04-local-result.json'),'Local reconstruction replay drift')
    analysis=adjudication(batches,result);P.write(OUT,'05-analysis.json',analysis);return analysis['status']

def freeze():
    adjudicate();P.freeze(ID,['00-build.log','00-domain-fixtures.log','02-preexecution.json','03-batches.jsonl','04-local-result.json','05-analysis.json'])
    return 'PASS-FROZEN-EXHAUST-BALANCE-17'

def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);trace=[json.loads(s) for s in data['03-batches.jsonl'].splitlines()]
    batches=Batches(trace);result=reconstruct(batches.evaluate,describe)
    P.require(result==json.loads(data['04-local-result.json']) and batches.cursor==len(trace),'Adaptive algorithm replay drift')
    analysis=adjudication(batches,result);P.require(analysis==json.loads(data['05-analysis.json']),'Frozen analysis drift')
    return analysis['status']+' (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
