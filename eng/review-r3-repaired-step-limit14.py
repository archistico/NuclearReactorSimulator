"""Offline diagnosis of what limits the step of capacity-minimum model 12: sampled amplitudes or a conflict between rows."""
from __future__ import annotations
import json
import math
import runpy
import sys
import r3_h_protocol as P
import r3_h_minimax_lp as LP

ROOT=P.ROOT
M=runpy.run_path(str(ROOT/'eng/review-r3-repaired-capacity-minimum-model12.py'))
X,R,D,S=M['X'],M['R'],M['D'],M['S']
ID='r3-repaired-step-limit14';OUT=ROOT/'artifacts'/ID;CONTRACT=ROOT/'eng'/(ID+'-contract.json')
CAPACITIES,CHECKPOINTS=R['CAPACITIES'],R['CHECKPOINTS']
POLICY=dict(runtime_steps=0,model='capacity-minimum model 12, rebuilt and required identical to its frozen analysis',
            patterns=[['stop']*3,['admission']*3,['expansion']*3],
            boxes=['sampled amplitudes',1e-3,5e-3,1e-2,2e-2],inherited_coordinate_limit=.02,
            uniform_box='every direction coefficient and every aggregated coordinate within the stated fraction; one-sided coefficients nonnegative',
            branch_margin_kg_s=1e-3,also_without_branch_constraints=True,
            invariant='first-checkpoint exhaust-energy row minus (its flow response / torque flow response) times the first-checkpoint torque row',
            invariant_pattern_tolerance=1e-9,bound_agreement_tolerance=1e-6,active_multiplier_threshold=1e-4,
            lever_probe='frozen exhaust:energy probes at every recorded amplitude and sign',
            beyond_sampled_amplitudes='linear extrapolation of a model verified only inside them: an indication, not a prediction or a proof',
            proposals=0,searches=0,production_changes=0,root_criterion_unchanged=True,previous_results_rewritten=False,
            r3_passed=False,m10_closed=False)
EXHAUST=('seed-step1','exhaust','energy');TORQUE=('seed-step1','rotor','torque');LEVER='exhaust:energy'


def program(x,pattern,box,branch):
    """Minimax of the 81 predicted rows. box=None: sampled amplitudes; else a uniform coordinate fraction."""
    model,base,labels,rc=x['model'],x['base'],x['labels'],x['row_checkpoint'];n=len(labels)
    width=x['amplitudes'] if box is None else [box]*n;lower=[0. if l==0. else -1. for l in x['lower']]
    capacities,rows=X['affine'](model,base,labels,width,pattern,rc)
    scale=max(1.,max(abs(c) for c,_ in rows));a=[];b=[]
    for constant,slope in rows:
        shifted=(constant+math.fsum(s*l for s,l in zip(slope,lower)))/scale;s=[v/scale for v in slope]
        a.append(s+[-1.]);b.append(-shifted);a.append([-v for v in s]+[-1.]);b.append(shifted)
    if branch:
        for k,winner in enumerate(pattern):
            w=CAPACITIES.index(winner);wc,ws=capacities[k][w]
            for c in range(len(CAPACITIES)):
                if c!=w:
                    oc,os=capacities[k][c];d=[p-q for p,q in zip(ws,os)]
                    a.append(d+[0.]);b.append(oc-wc-POLICY['branch_margin_kg_s']-math.fsum(v*l for v,l in zip(d,lower)))
    for j in range(n):a.append([float(i==j) for i in range(n)]+[0.]);b.append(1.-lower[j])
    if box is not None:
        for i in range(30):
            column=[(d['coordinates']+d['operating'])[i] for d in x['descriptions']]
            if sum(v!=0 for v in column)>1:
                offset=math.fsum(v*l for v,l in zip(column,lower))
                a.append(column+[0.]);b.append(1.-offset);a.append([-v for v in column]+[0.]);b.append(1.+offset)
    result=LP.linear_program(a,b,[0.]*n+[-1.]);P.require(result['status']=='OPTIMAL-CERTIFIED','Step-limit LP not certified')
    names=row_names(x);multipliers={}
    for i in range(len(rows)):
        value=result['dual'][2*i]-result['dual'][2*i+1]
        if abs(value)>POLICY['active_multiplier_threshold']:multipliers[names[i]]=value
    coefficients=[(v+l)*w for v,l,w in zip(result['primal'][:n],lower,width)]
    return dict(optimum=result['primal'][n]*scale,row_multipliers=multipliers,
                directions_at_bound=sum(abs(c)==w for c,w in zip(coefficients,width)),
                certificate={k:result[k] for k in ('status','primal_violation','dual_violation','duality_gap','pivots')})


def row_names(x):return [' '.join(n) for n in x['names']]+[f'thermal {i}' for i in range(6)]


def invariant(x):
    """Flow-free combination of two first-checkpoint rows, and the bound it puts on the largest row."""
    model,base,labels=x['model'],x['base'],x['labels'];e=x['names'].index(EXHAUST);t=x['names'].index(TORQUE)
    P.require(x['row_checkpoint'][e]==x['row_checkpoint'][t]==0,'Invariant rows are not first-checkpoint rows')
    ratio=model['H'][e][0]/model['H'][t][0];value=base['y'][e]-ratio*base['y'][t]
    direct={l:model['G'][l][e]-ratio*model['G'][l][t] for l in labels}
    # The combination must not depend on the winner pattern: the stage-flow term cancels exactly.
    drift=0.
    for pattern in POLICY['patterns']:
        _,rows=X['affine'](model,base,labels,[1.]*len(labels),pattern,x['row_checkpoint'])
        drift=max([drift]+[abs(rows[e][1][j]-ratio*rows[t][1][j]-direct[l]) for j,l in enumerate(labels)])
    P.require(drift<=POLICY['invariant_pattern_tolerance'],'Invariant depends on the winner pattern')
    def bound(width):
        reach=math.fsum(abs(direct[l])*w for l,w in zip(labels,width))
        return dict(reach=reach,largest_row_lower_bound=max(0.,abs(value)-reach)/(1.+abs(ratio)))
    sampled=bound(x['amplitudes']);uniform={f'{f:g}':bound([f]*len(labels)) for f in POLICY['boxes'][1:]}
    share={l:abs(direct[l])*w for l,w in zip(labels,x['amplitudes'])};total=math.fsum(share.values())
    flow_share={l:dict(exhaust_energy_per_amplitude=model['G'][l][e]*w,torque_per_amplitude=model['G'][l][t]*w,
                       flow_free_part_per_amplitude=direct[l]*w) for l,w in zip(labels,x['amplitudes']) if abs(model['G'][l][t]*w)>1e-6 or abs(direct[l]*w)>1e-6}
    return dict(exhaust_energy_per_stage_flow=model['H'][e][0],torque_per_stage_flow_nm=model['H'][t][0],ratio_per_nm=ratio,
                baseline_exhaust_energy_row=base['y'][e],baseline_torque_row_nm=base['y'][t],baseline_value=value,
                maximum_pattern_dependence=drift,direction_effects=flow_share,
                sampled_reach_share={l:v/total for l,v in share.items() if v/total>1e-3},
                sampled_box=sampled,uniform_boxes=uniform,
                lever=LEVER,lever_response_per_unit=direct[LEVER],lever_coordinate_to_cancel=-value/direct[LEVER])


def lever_probes(x):
    _,_,probes=R['M']['inputs']();e=x['names'].index(EXHAUST);rows=[]
    for key in sorted(k for k in probes if k.startswith(LEVER+'/') and k.endswith('/repeat/1')):
        _,tag,amplitude,_,_=key.split('/');record=probes[key];guards=D['reasons'](record,x['base_record'])
        P.require(guards['available'] and not guards['phase_changes'],'Lever probe outside the baseline regime')
        step=float(amplitude)*(1. if tag=='plus' else -1.);value=S['weighted_vector'](record,x['scales'],x['names'])[e]
        rows.append(dict(record=key.rsplit('/repeat/',1)[0],coordinate=step,exhaust_energy_row=value,secant_per_unit=(value-x['base']['y'][e])/step))
    return sorted(rows,key=lambda r:r['coordinate'])


def fixtures():
    # One row 1-0.002z with z in [-1,1] (x=z+1): a weak lever leaves the optimum at value minus reach.
    r=LP.linear_program([[-.002,-1.],[.002,-1.],[1.,0.]],[-1.002,1.002,2.],[0.,-1.])
    P.require(r['status']=='OPTIMAL-CERTIFIED' and abs(r['primal'][1]-.998)<1e-12 and abs(r['primal'][0]-2.)<1e-12,'Step-limit LP fixture')
    return M['fixtures']()+'; PASS-STEP-LIMIT-14-FIXTURES'


def authenticate():
    c=P.load(CONTRACT);P.require(c['schema']==ID+'-v1' and c['policy']==POLICY,'Policy drift')
    for key in ('src','tests'):P.require(P.tree(key)==c['unchanged_baseline'][key],'Runtime/test tree drift')
    for entry in c['implementation_files']+c['frozen_inputs']:
        P.require(P.digest((ROOT/entry['path']).read_bytes())==entry['sha256'],'Input drift: '+entry['path'])


def analyze():
    x=M['build']();parent=json.loads(S['frozen'](M['ID'])['01-analysis.json'])
    P.require(parent['retrospective_checks_passed'] and x['model']['H']==parent['model']['flow_response_by_row']
              and x['model']['G']==parent['model']['direction_response_by_row'],'Rebuilt model differs from the frozen model 12')
    programs=[]
    for box in POLICY['boxes']:
        for pattern in POLICY['patterns']:
            for branch in (True,False):
                programs.append(dict(box=box,pattern=pattern,branch_constraints=branch,
                                     **program(x,tuple(pattern),None if box==POLICY['boxes'][0] else box,branch)))
    inv=invariant(x);probes=lever_probes(x)
    best={str(box):min(p['optimum'] for p in programs if p['box']==box) for box in POLICY['boxes']}
    bounds={str(POLICY['boxes'][0]):inv['sampled_box']['largest_row_lower_bound'],
            **{k:v['largest_row_lower_bound'] for k,v in inv['uniform_boxes'].items()}}
    gap={k:best[k]-bounds[k] for k in best}
    secants=[p['secant_per_unit'] for p in probes]
    limit=f"{POLICY['inherited_coordinate_limit']:g}"
    return dict(status='PASS-REPAIRED-STEP-LIMIT-14-EVIDENCE',fixtures=fixtures(),runtime_steps=0,
                baseline_maximum_weighted_81=max(map(abs,x['base']['y'])),
                programs=programs,best_optimum_by_box=best,invariant=inv,invariant_bound_by_box=bounds,optimum_minus_bound=gap,
                bound_never_exceeds_optimum=all(v>=-POLICY['bound_agreement_tolerance'] for v in gap.values()),
                bound_tight_in_sampled_box=abs(gap[str(POLICY['boxes'][0])])<=POLICY['bound_agreement_tolerance'],
                lever_probes=probes,lever_secant_range_per_unit=[min(secants),max(secants)],
                largest_probed_lever_coordinate=max(abs(p['coordinate']) for p in probes),
                conclusion=dict(
                    limiting_rows=['seed-step1 exhaust energy','seed-step1 rotor torque'],
                    same_limit_in_every_pattern_and_without_branch_constraints=max(p['optimum'] for p in programs if p['box']==POLICY['boxes'][0] and not p['branch_constraints'])
                        -min(p['optimum'] for p in programs if p['box']==POLICY['boxes'][0])<1e-3,
                    sampled_box_optimum=best[str(POLICY['boxes'][0])],inherited_limit_optimum=best[limit],
                    lever_coordinate_to_cancel=inv['lever_coordinate_to_cancel'],
                    lever_coordinate_exceeds_inherited_limit=abs(inv['lever_coordinate_to_cancel'])>POLICY['inherited_coordinate_limit'],
                    lever_coordinate_over_largest_probe=abs(inv['lever_coordinate_to_cancel'])/max(abs(p['coordinate']) for p in probes),
                    wider_sampling_alone_sufficient_in_linear_extrapolation=best[limit]<=1e-3),
                proposal_executed=False,search_started=False,family_selected=False,root_observed=False,r3_passed=False,m10_closed=False,
                limits=['Inside the sampled amplitudes the bound is a property of the verified model; beyond them it is a linear extrapolation, not a prediction',
                        'The exhaust-energy lever is measured only up to a coordinate of 1e-4: its response at the percent scale is unknown and may bend or change phase',
                        'The diagnosis concerns the 30 working directions of family H; quantities outside it, such as the condenser cooling side, are not examined',
                        'No recipe, proposal, search or seed; the root criterion and the inherited 2% limit are unchanged'])


def execute():
    authenticate();P.require(not (OUT/'01-analysis.json').exists(),'Existing review retained')
    result=analyze();P.write(OUT,'01-analysis.json',result);P.freeze(ID,['01-analysis.json']);return result['status']


def review():
    authenticate();receipt=ROOT/'eng'/(ID+'-returned.json');P.require(P.load(receipt)['contract_sha256']==P.digest(CONTRACT.read_bytes()),'Contract receipt drift')
    data=S['frozen'](ID);P.require(json.loads(json.dumps(analyze()))==json.loads(data['01-analysis.json']),'Replay drift')
    return json.loads(data['01-analysis.json'])['status']+' (offline; zero runtime steps)'


if __name__=='__main__':
    P.require(len(sys.argv)==2 and sys.argv[1] in ('fixtures','execute','review'),'Choose protocol stage')
    print(globals()[sys.argv[1]]())
