"""Repeated six-width derivative audit at the frozen H8 boundary; no optimization."""
import argparse
import json
import math

import r3_h_protocol as P
import r3_h_phase_trajectory as T
import r3_h_c4_energy_margin as E
import r3_h_multiscale_derivatives as D

ID = "r3-h-multiscale-derivative-trial9"
OUT = P.ROOT / "artifacts" / ID
POLICY = dict(variables=24, residuals=75, fractional_bound=.02,
              derivative_widths=[1e-9, 1e-8, 1e-7, 1e-6, 1e-5, 1e-4], repeats=2,
              maximum_recipes=578, maximum_batches=13, maximum_physical_steps=1760,
              directional_error_fraction=.25, small_directional_fractions=[.001, .0001],
              required_adjacent_widths=2, production_change=False, seed_installation=False,
              optimization=False, residual_reweighting=False, r3_passed=False)


def frozen(protocol):
    receipt = P.ROOT / "eng" / (protocol + "-returned.json")
    P.require(P.load(receipt)["contract_sha256"] == P.digest((P.ROOT / "eng" / (protocol + "-contract.json")).read_bytes()), "Frozen prerequisite contract drift")
    return P.frozen(P.ROOT / "eng/frozen-evidence" / protocol, receipt)


def inputs():
    h8 = json.loads(frozen("r3-h-phase-constrained-search8")["04-result.json"])
    P.require(not h8["root_observed"], "Unexpected prerequisite root")
    base = h8["final"]
    data = frozen("r3-h-directional-derivative-trial8")
    records = [r for line in data["03-batches.jsonl"].decode().splitlines() for r in json.loads(line)["records"]]
    P.require({**base, "Id": "base"} == {**records[0], "Id": "base"}, "H8 and directional baseline differ")
    return base, records[2::2], json.loads(data["01-static.json"])["points"][1:]


def points():
    base, _, _ = inputs()
    result = [dict(id="base", coordinates=base["Coordinates"])]
    for k, h in enumerate(POLICY["derivative_widths"]):
        for j, x in enumerate(base["Coordinates"]):
            for sign in (1, -1):
                if abs(x + sign * h) <= .02:
                    coordinates = base["Coordinates"][:]
                    coordinates[j] += sign * h
                    result.append(dict(id=f"width/{k}/probe/{j}/{sign}", coordinates=coordinates))
    return result


def requests():
    return [dict(Id=point["id"] + f"/repeat/{repeat}", Coordinates=point["coordinates"])
            for point in points() for repeat in (1, 2)]


def static():
    P.authenticate(ID, POLICY)
    planned = requests()
    batches = math.ceil(len(planned) / 48)
    P.require(len(planned) <= POLICY["maximum_recipes"] and batches <= POLICY["maximum_batches"], "Static physical budget exceeded")
    P.write(OUT, "01-static.json", dict(status="PASS-STATIC", contract_sha256=P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()),
                                       fixtures=D.fixtures()+E.fixtures(), requests=planned, recipes=len(planned), batches=batches,
                                       maximum_physical_steps=3*len(planned)+2*batches))


def execute():
    P.authenticate(ID, POLICY)
    P.Batches(ID, POLICY["maximum_recipes"], POLICY["maximum_batches"]).evaluate(requests())


def energy(record):
    return [E.node_margin(n)["energy_margin_j_kg"] for n in T.nodes(record)]


def audit(batches):
    base, directional, directional_points = inputs()
    expected = requests()
    P.require([b["batch"] for b in batches] == list(range(1, len(batches)+1)), "Batch sequence drift")
    P.require([r for b in batches for r in b["requests"]] == expected, "Multiscale request drift")
    P.require(all(1 <= len(b["requests"]) <= 48 and len(b["requests"]) == len(b["records"]) for b in batches), "Multiscale batch size drift")
    records = [r for b in batches for r in b["records"]]
    P.require(len(records) == len(expected) <= POLICY["maximum_recipes"] and len(batches) <= POLICY["maximum_batches"], "Physical budget drift")
    for request, record in zip(expected, records):
        P.require(request["Id"] == record["Id"] and request["Coordinates"] == record["Coordinates"], "Probe identity drift")
        P.audit_record(record)
    for a, b in zip(records[::2], records[1::2]):
        P.require({**a, "Id": "repeat"} == {**b, "Id": "repeat"}, "Probe repeats differ")
    P.require({**records[0], "Id": "base"} == {**base, "Id": "base"}, "Physical baseline not reproduced")
    unique = {r["Id"].rsplit("/repeat/", 1)[0]: r for r in records[::2]}
    baseline = max(map(abs, base["Residuals"]))
    base_energy = energy(base)
    profiles = []
    for k, h in enumerate(POLICY["derivative_widths"]):
        columns = []
        for j in range(24):
            samples = [(s, unique[f"width/{k}/probe/{j}/{s}"]) for s in (1, -1) if f"width/{k}/probe/{j}/{s}" in unique]
            same = [(s, r) for s, r in samples if r["Reason"] == "VALID" and r["Phases"] == base["Phases"]]
            residual = D.column(base["Residuals"], [(s, r["Residuals"]) for s, r in same], h)
            margins = D.column(base_energy, [(s, energy(r)) for s, r in same], h)
            unilateral = [dict(sign=s, residual_derivative=D.column(base["Residuals"], [(s, r["Residuals"])], h),
                              energy_derivative=D.column(base_energy, [(s, energy(r))], h)) for s, r in same]
            columns.append(dict(coordinate=j, selected_signs=[s for s, _ in same],
                                stencil="central" if len(same)==2 else f"one-sided/{same[0][0]}" if same else "unavailable-phase-or-domain",
                                residual_derivative=residual, energy_derivative=margins, unilateral=unilateral,
                                observations=[dict(sign=s, id=r["Id"], reason=r["Reason"], phase_compatible=r["Reason"]=="VALID" and r["Phases"]==base["Phases"],
                                                   phase_changes=[i for i, (a,b) in enumerate(zip(base["Phases"],r.get("Phases",[]))) if a!=b]) for s,r in samples]))
        complete = all(c["residual_derivative"] is not None for c in columns)
        comparisons, convexity = [], None
        if complete:
            jacobian = [list(row) for row in zip(*(c["residual_derivative"] for c in columns))]
            energy_jacobian = [list(row) for row in zip(*(c["energy_derivative"] for c in columns))]
            for point, actual in zip(directional_points, directional):
                P.require(point["id"] + "/repeat/1" == actual["Id"], "Directional identity drift")
                delta = [x-b for x,b in zip(actual["Coordinates"],base["Coordinates"])]
                predicted = T.predict(base["Residuals"],jacobian,delta)
                predicted_energy = T.predict(base_energy,energy_jacobian,delta)
                error = max(abs(a-b) for a,b in zip(actual["Residuals"],predicted))
                actual_maximum = max(map(abs,actual["Residuals"]))
                reduction = baseline-actual_maximum
                compatible = actual["Phases"] == base["Phases"]
                limit = POLICY["directional_error_fraction"]*abs(reduction)
                comparisons.append(dict(fraction=point["fraction"],phase_compatible=compatible,
                                        actual_maximum=actual_maximum,predicted_maximum=max(map(abs,predicted)),actual_reduction=reduction,
                                        maximum_residual_error=error,sign_resolution_error_limit=limit,
                                        resolves_directional_change=compatible and error<=limit,
                                        actual_energy_margin=energy(actual),predicted_energy_margin=predicted_energy,
                                        maximum_energy_margin_error_j_kg=max(abs(a-b) for a,b in zip(energy(actual),predicted_energy))))
            selected = [next(r for p,r in zip(directional_points,directional) if p["fraction"]==f) for f in POLICY["small_directional_fractions"]]
            large, small = selected
            convexity = D.affine_error_bound(baseline,max(map(abs,small["Residuals"])),max(map(abs,large["Residuals"])),.1,
                                             jacobian,[x-b for x,b in zip(small["Coordinates"],base["Coordinates"])],
                                             [x-b for x,b in zip(large["Coordinates"],base["Coordinates"])])
        resolves_small = complete and all(any(c["fraction"]==f and c["resolves_directional_change"] for c in comparisons) for f in POLICY["small_directional_fractions"])
        profiles.append(dict(width=h,complete=complete,unavailable_columns=[c["coordinate"] for c in columns if c["residual_derivative"] is None],
                             columns=columns,directional_comparisons=comparisons,affine_error_bound=convexity,resolves_small_steps=resolves_small))
    adjacent = []
    for left,right in zip(profiles,profiles[1:]):
        changes=[]
        for a,b in zip(left["columns"],right["columns"]):
            if a["residual_derivative"] is not None and b["residual_derivative"] is not None:
                absolute=max(abs(x-y) for x,y in zip(a["residual_derivative"],b["residual_derivative"]))
                scale=max(max(map(abs,a["residual_derivative"])),max(map(abs,b["residual_derivative"])),1e-300)
                changes.append(dict(coordinate=a["coordinate"],same_stencil=a["selected_signs"]==b["selected_signs"],
                                    relative_maximum_column_change=absolute/scale))
        adjacent.append(dict(widths=[left["width"],right["width"]],column_comparisons=changes,
                             resolves_small_steps=left["resolves_small_steps"] and right["resolves_small_steps"]))
    valid = [r for r in unique.values() if r["Reason"]=="VALID"]
    compatible = [r for r in valid if r["Phases"]==base["Phases"]]
    best = min(compatible,key=lambda r:max(map(abs,r["Residuals"])))
    passed = any(a["resolves_small_steps"] for a in adjacent)
    return dict(status="PASS-REPEATED-MULTISCALE-DERIVATIVE-AUDIT",recipes=len(records),batches=len(batches),
                maximum_physical_steps=3*len(records)+2*len(batches),base_maximum=baseline,
                profiles=profiles,adjacent_width_comparisons=adjacent,
                phase_incompatible_unique_probes=len(valid)-len(compatible),invalid_unique_probes=len(unique)-len(valid),
                best_phase_compatible_observation=dict(id=best["Id"],coordinates=best["Coordinates"],maximum=max(map(abs,best["Residuals"])),qualified=False),
                sufficient_small_step_model_observed=passed,search_ready=False,
                next_activity="REVIEW-STEP-SCALE-AND-BOUNDARY-REPRESENTATION-USING-RETURNED-EVIDENCE" if not passed else "REPEATED-ENERGY-GUARDED-PROPOSAL-TRIAL-BEFORE-SEARCH",
                r3_passed=False,m10_closed=False,seed_installed=False)


def adjudicate():
    P.authenticate(ID,POLICY)
    result=audit([json.loads(line) for line in (OUT/"03-batches.jsonl").read_text().splitlines()])
    P.write(OUT,"05-adjudication.json",result)
    print_summary(result)


def print_summary(result):
    print(json.dumps({**{k:v for k,v in result.items() if k not in ("profiles","adjacent_width_comparisons")},
                      "profiles":[dict(width=p["width"],complete=p["complete"],unavailable_columns=p["unavailable_columns"],
                                       resolves_small_steps=p["resolves_small_steps"],affine_error_bound=p["affine_error_bound"],
                                       directional_errors=[dict(fraction=c["fraction"],maximum_residual_error=c["maximum_residual_error"],
                                                                maximum_energy_margin_error_j_kg=c["maximum_energy_margin_error_j_kg"]) for c in p["directional_comparisons"]]) for p in result["profiles"]]},indent=2))


def review():
    P.authenticate(ID,POLICY)
    data=frozen(ID)
    result=audit([json.loads(line) for line in data["03-batches.jsonl"].decode().splitlines()])
    P.require(result==json.loads(data["05-adjudication.json"]),"Frozen multiscale adjudication drift")
    print_summary(result)


if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage",choices=["static","preexecution","execute","adjudicate","review"])
    {"static":static,"preexecution":lambda:(P.authenticate(ID,POLICY),P.preexecution(ID)),"execute":execute,
     "adjudicate":adjudicate,"review":review}[parser.parse_args().stage]()
