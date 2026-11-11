"""Primary-only minimax LP; deterministic basis selection, no secondary tie LP."""
import math

from r3_h_minimax_lp import linear_program


def minimax_step(jacobian, residuals, coordinates, radius):
    scale = max(1., max(map(abs, residuals)))
    active = [j for j in range(len(coordinates)) if any(row[j] != 0 for row in jacobian)]
    if not active:
        return dict(delta=[0.] * len(coordinates), raw_delta=[0.] * len(coordinates), primary_maximum=max(map(abs, residuals)), certificates=[])
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
    raw = [0.] * len(coordinates)
    for k, j in enumerate(active):
        raw[j] = radius * (lower[k] + primary["primal"][k])
    delta = [.99 * d for d in raw]
    if not all(math.isfinite(d) and abs(d) <= radius * (1 + 1e-12) and abs(x + d) <= .02 for x, d in zip(coordinates, delta)):
        raise ValueError("Certified minimax step escaped physical/trust box")
    return dict(delta=delta, raw_delta=raw, primary_maximum=primary["primal"][n] * scale,
                certificates=[{key: primary[key] for key in ("status", "primal_violation", "dual_violation", "duality_gap", "pivots")}])


def fixtures():
    results = []
    for name, matrix, residual, target, optimum in [
        ("opposite-to-least-squares", [[100.], [-100.], [-100.]], [10., 9., 9.], [-.005], 9.5),
        ("coupled-root", [[3., 1.], [1., 2.]], [-.018, -.011], [.005, .003], 0.),
        ("zero-column", [[1., 0.], [1., 0.]], [-.005, -.005], [.005, 0.], 0.),
        ("joint-descent", [[100., 0.], [0., 100.]], [1., 1.], [-.01, -.01], 0.),
    ]:
        result = minimax_step(matrix, residual, [0.] * len(target), .02)
        if abs(result["primary_maximum"] - optimum) > 1e-7 or max(abs(a - b) for a, b in zip(result["raw_delta"], target)) > 1e-7:
            raise ValueError("Primary-only fixture failed: " + name)
        results.append(dict(name=name, passed=True))
    return results
