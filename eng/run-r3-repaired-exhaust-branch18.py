"""Repeat the bounded local reconstruction with a fixed, explicit hotwell branch reserve."""
from __future__ import annotations
import json
import runpy
import subprocess
import sys
import r3_h_protocol as P

ROOT=P.ROOT
Q=runpy.run_path(str(ROOT/'eng/run-r3-repaired-exhaust-balance17.py'))
V,C,S,D,T=Q['V'],Q['C'],Q['S'],Q['D'],Q['T']
ID='r3-repaired-exhaust-branch18';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
PROJECT='eng/diagnostics/R3ExhaustBranchObservation/R3ExhaustBranchObservation.csproj'
BINARY=ROOT/'eng/diagnostics/R3ExhaustBranchObservation/bin/Release/net10.0/R3ExhaustBranchObservation.dll'
POLICY=dict(**Q['POLICY'],fixed_hotwell_mass_fraction=1e-6,
            hotwell_reserve='zero for the historical baseline, fixed +1e-6 for every other raw recipe; no runtime correction',
            parent_stop='INITIAL-GUARD-REJECTION',algorithm='unchanged local reconstruction 17, including every stop, tolerance and budget')

def request(tag,point,repeat):
    r=Q['request'](tag,point,repeat)
    if tag!='base':r['Coordinates'][10]=POLICY['fixed_hotwell_mass_fraction']
    return r

def fixtures():
    P.require(request('base',[0.,0.],1)['Coordinates'][10]==0.,'Baseline reserve fixture')
    P.require(request('start',POLICY['initial_point'],1)['Coordinates'][10]==1e-6,'Fixed reserve fixture')
    return Q['fixtures']()+'; PASS-EXHAUST-BRANCH-18-FIXTURES'

def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for e in c['implementation_files']+c['frozen_inputs']:P.require(P.digest((ROOT/e['path']).read_bytes())==e['sha256'],'Input drift: '+e['path'])

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
        self.cursor+=1;expected=[request(tag,x,r) for tag,x in points for r in (1,2)];self.recipes+=len(expected)
        P.require(len(expected)<=8 and self.cursor<=26 and self.recipes<=84,'Physical budget exceeded')
        if self.replay is not None:
            P.require(self.cursor<=len(self.replay),'Missing frozen batch');batch=self.replay[self.cursor-1]
            P.require(batch['batch']==self.cursor and batch['requests']==expected,'Adaptive request replay drift');records=batch['records']
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
        first=Q['audit_pairs'](records,expected);self.records.extend(records)
        if self.replay is None:print(f'batch={self.cursor} repeated_recipes={self.recipes}',flush=True)
        return first

def analyze(batches,result):
    analysis=Q['adjudication'](batches,result)
    parent=S['frozen'](Q['ID']);prior_trace=[json.loads(s) for s in parent['03-batches.jsonl'].splitlines()]
    prior=json.loads(parent['05-analysis.json']);P.require(prior['stop']==POLICY['parent_stop'],'Parent rejection drift')
    by_id={r['Id']:r for r in batches.records}
    for repeat in (1,2):
        baseline=next(r for r in prior_trace[0]['records'] if r['Id']==f'base/repeat/{repeat}')
        P.require(by_id[baseline['Id']]==baseline,'Parent full baseline drift')
    for r in batches.records:
        P.require(r['Coordinates'][10]==(0. if r['Id'].startswith('base/repeat/') else POLICY['fixed_hotwell_mass_fraction']),'Fixed reserve drift')
    analysis.update(status='PASS-REPAIRED-EXHAUST-BRANCH-18-EVIDENCE',fixtures=fixtures(),fixed_hotwell_mass_fraction=1e-6,
                    fixed_hotwell_reserve_kg=by_id['base/repeat/1']['Raw'][5]['Mass']*1e-6,
                    original_guard_rejection_retained=prior['stop'],parent_full_baseline_identical=True)
    return analysis

def execute():
    authenticate();OUT.mkdir(parents=True,exist_ok=True)
    with (OUT/'03-batches.jsonl').open('x',encoding='utf-8'):pass
    batches=Batches();result=Q['reconstruct'](batches.evaluate,Q['describe']);P.write(OUT,'04-local-result.json',result)
    return adjudicate()

def adjudicate():
    authenticate();trace=[json.loads(s) for s in (OUT/'03-batches.jsonl').read_text(encoding='utf-8').splitlines()]
    batches=Batches(trace);result=Q['reconstruct'](batches.evaluate,Q['describe'])
    P.require(batches.cursor==len(trace) and result==P.load(OUT/'04-local-result.json'),'Adaptive algorithm replay drift')
    analysis=analyze(batches,result);P.write(OUT,'05-analysis.json',analysis);return analysis['status']

def freeze():
    adjudicate();P.freeze(ID,['00-build.log','00-domain-fixtures.log','02-preexecution.json','03-batches.jsonl','04-local-result.json','05-analysis.json'])
    return 'PASS-FROZEN-EXHAUST-BRANCH-18'

def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);trace=[json.loads(s) for s in data['03-batches.jsonl'].splitlines()]
    batches=Batches(trace);result=Q['reconstruct'](batches.evaluate,Q['describe'])
    P.require(result==json.loads(data['04-local-result.json']) and batches.cursor==len(trace),'Adaptive algorithm replay drift')
    P.require(analyze(batches,result)==json.loads(data['05-analysis.json']),'Frozen analysis drift')
    return 'PASS-REPAIRED-EXHAUST-BRANCH-18-EVIDENCE (offline; zero runtime steps)'

if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','preexecution','execute','adjudicate','freeze','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
