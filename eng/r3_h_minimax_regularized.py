"""Minimax LP with explicit 1e-6 normalized secondary cap; physical criteria unchanged."""
import math
from r3_h_minimax_lp import linear_program


def minimax_step(jacobian, residuals, coordinates, radius):
    size = len(coordinates)
    scale = max(1., max(map(abs, residuals)))
    active = [j for j in range(size) if any(row[j] != 0 for row in jacobian)]
    if not active:
        return dict(raw_delta=[0.] * size, delta=[0.] * size, primary_maximum=max(map(abs, residuals)), certificates=[])
    lower = [max(-1., (-.02 - coordinates[j]) / radius) for j in active]
    upper = [min(1., (.02 - coordinates[j]) / radius) for j in active]
    matrix = [[row[j] * radius / scale for j in active] for row in jacobian]
    normalized = [r / scale + sum(a * l for a, l in zip(row, lower)) for row, r in zip(matrix, residuals)]
    n = len(active)
    constraints = [row + [-1.] for row in matrix] + [[-v for v in row] + [-1.] for row in matrix]
    rhs = [-v for v in normalized] + normalized
    for j in range(n):
        row = [0.] * (n + 1)
        row[j] = 1.
        constraints.append(row)
        rhs.append(upper[j] - lower[j])
    primary = linear_program(constraints, rhs, [0.] * n + [-1.])
    if primary["status"] != "OPTIMAL-CERTIFIED":
        raise ValueError("Minimax primary LP: " + primary["status"])
    optimum = primary["primal"][n]
    secondary_constraints = [row + [0.] * n for row in constraints]
    secondary_rhs = rhs[:]
    cap = [0.] * (2 * n + 1)
    cap[n] = 1.
    secondary_constraints.append(cap)
    secondary_rhs.append(optimum + 1e-6)
    for j in range(n):
        row = [0.] * (2 * n + 1)
        row[j], row[n + 1 + j] = 1., -1.
        secondary_constraints.append(row)
        secondary_rhs.append(-lower[j])
        row = [0.] * (2 * n + 1)
        row[j], row[n + 1 + j] = -1., -1.
        secondary_constraints.append(row)
        secondary_rhs.append(lower[j])
    secondary = linear_program(secondary_constraints, secondary_rhs, [0.] * (n + 1) + [-1.] * n)
    if secondary["status"] != "OPTIMAL-CERTIFIED":
        raise ValueError("Minimax secondary LP: " + secondary["status"])
    raw = [0.] * size
    for k, j in enumerate(active):
        raw[j] = radius * (lower[k] + secondary["primal"][k])
    delta = [.99 * d for d in raw]
    if not all(math.isfinite(d) and abs(d) <= radius * (1 + 1e-12) and abs(x + d) <= .02 for x, d in zip(coordinates, delta)):
        raise ValueError("Certified minimax step escaped physical/trust box")
    return dict(raw_delta=raw, delta=delta, primary_maximum=optimum * scale,
                certificates=[{key: result[key] for key in ("status", "primal_violation", "dual_violation", "duality_gap", "pivots")} for result in (primary, secondary)])


def fixtures():
    results = []
    for name, matrix, residual in [
        ("opposite-to-squares", [[100.],[-100.],[-100.]], [10.,9.,9.]),
        ("coupled-root", [[3.,1.],[1.,2.]], [-.018,-.011]),
        ("zero-column", [[1.,0.],[1.,0.]], [-.005,-.005]),
        ("joint-descent", [[100.,0.],[0.,100.]], [1.,1.]),
    ]:
        result = minimax_step(matrix,residual,[0.]*len(matrix[0]),.02)
        raw = [r+sum(a*d for a,d in zip(row,result["raw_delta"])) for row,r in zip(matrix,residual)]
        scale=max(1.,max(map(abs,residual)))
        if max(map(abs,raw)) > result["primary_maximum"] + 1e-6*scale + 1e-7:
            raise ValueError("Secondary objective escaped its declared minimax cap")
        if name=="zero-column" and result["raw_delta"][1]!=0:
            raise ValueError("Zero column moved")
        if name=="opposite-to-squares" and abs(result["primary_maximum"]-9.5)>1e-7:
            raise ValueError("Wrong primary minimax optimum")
        if name!="opposite-to-squares" and abs(result["primary_maximum"])>1e-7:
            raise ValueError("Known root was lost by primary program")
        results.append(dict(name=name,passed=True))
    return results
