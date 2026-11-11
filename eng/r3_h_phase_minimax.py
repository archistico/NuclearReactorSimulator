"""Local minimax subproblem constrained to the predicted hotwell mixture branch."""
import math

from r3_h_minimax_lp import linear_program


def minimax_step(jacobian, residuals, coordinates, radius, qualities, quality_jacobian):
    if len(qualities) != len(quality_jacobian) or not all(math.isfinite(q) and 0 < q < 1 for q in qualities):
        raise ValueError("Invalid mixture branch margins")
    scale = max(1., max(map(abs, residuals)))
    active = [j for j in range(len(coordinates)) if any(row[j] != 0 for row in jacobian + quality_jacobian)]
    if not active:
        raise ValueError("No active phase/residual column")
    lower = [max(-1., (-.02 - coordinates[j]) / radius) for j in active]
    upper = [min(1., (.02 - coordinates[j]) / radius) for j in active]
    matrix = [[row[j] * radius / scale for j in active] for row in jacobian]
    normalized = [r / scale + sum(a * l for a, l in zip(row, lower)) for row, r in zip(matrix, residuals)]
    n = len(active)
    constraints = [row + [-1.] for row in matrix] + [[-a for a in row] + [-1.] for row in matrix]
    rhs = [-r for r in normalized] + normalized
    for j in range(n):
        row = [0.] * (n + 1)
        row[j] = 1.
        constraints.append(row)
        rhs.append(upper[j] - lower[j])
    for q, gradient in zip(qualities, quality_jacobian):
        row = [gradient[j] * radius for j in active]
        normalization = max(abs(q), max(map(abs, row)), 1e-300)
        constraints.append([-a / normalization for a in row] + [0.])
        rhs.append((.9 * q + sum(a * l for a, l in zip(row, lower))) / normalization)
    primary = linear_program(constraints, rhs, [0.] * n + [-1.])
    if primary["status"] != "OPTIMAL-CERTIFIED":
        raise ValueError("Phase primary LP: " + primary["status"])
    optimum = primary["primal"][n]
    second_constraints = [row + [0.] * n for row in constraints]
    second_rhs = rhs[:]
    cap = [0.] * (2 * n + 1)
    cap[n] = 1.
    second_constraints.append(cap)
    second_rhs.append(optimum + 1e-6)
    for j in range(n):
        row = [0.] * (2 * n + 1)
        row[j], row[n + 1 + j] = 1., -1.
        second_constraints.append(row)
        second_rhs.append(-lower[j])
        row = [0.] * (2 * n + 1)
        row[j], row[n + 1 + j] = -1., -1.
        second_constraints.append(row)
        second_rhs.append(lower[j])
    secondary = linear_program(second_constraints, second_rhs, [0.] * (n + 1) + [-1.] * n)
    if secondary["status"] != "OPTIMAL-CERTIFIED":
        raise ValueError("Phase secondary LP: " + secondary["status"])
    raw = [0.] * len(coordinates)
    for k, j in enumerate(active):
        raw[j] = radius * (lower[k] + secondary["primal"][k])
    delta = [.99 * d for d in raw]
    predicted_q = [q + sum(a * d for a, d in zip(row, delta)) for q, row in zip(qualities, quality_jacobian)]
    if not all(math.isfinite(d) and abs(d) <= radius * (1 + 1e-12) and abs(x + d) <= .02 for x, d in zip(coordinates, delta)):
        raise ValueError("Phase step escaped box")
    if not all(.1 * q <= actual < 1 for q, actual in zip(qualities, predicted_q)):
        raise ValueError("Phase certificate failed in original quality units")
    return dict(delta=delta, predicted_quality=predicted_q, primary_maximum=optimum * scale,
                certificates=[{key: result[key] for key in ("status", "primal_violation", "dual_violation", "duality_gap", "pivots")} for result in (primary, secondary)])


def fixtures():
    cases = []
    for name, q, gradient in [("ordinary-margin", 1e-3, 1.), ("tiny-quality-normalized", 1e-11, 1e-4)]:
        result = minimax_step([[1000.]], [1.], [0.], .01, [q], [[gradient]])
        bound = -.9 * q / gradient
        if result["delta"][0] < bound or result["delta"][0] >= 0 or result["predicted_quality"][0] < .1 * q:
            raise ValueError("Branch bound fixture failed")
        cases.append(dict(name=name, passed=True))
    # Two individually feasible coordinate steps can jointly cross a branch boundary.
    result = minimax_step([[1., 0.], [0., 1.]], [1., 1.], [0., 0.], .01, [1e-3], [[1., 1.]])
    if sum(result["delta"]) < -.0009 or min(result["delta"]) >= 0:
        raise ValueError("Coupled branch constraint fixture failed")
    cases.append(dict(name="simultaneous-coordinates-respect-shared-margin", passed=True))
    try:
        minimax_step([[1.]], [1.], [0.], .01, [0.], [[1.]])
    except ValueError:
        cases.append(dict(name="zero-quality-is-not-fictitious-mixture-interior", passed=True))
    else:
        raise ValueError("Invalid mixture margin accepted")
    return cases
