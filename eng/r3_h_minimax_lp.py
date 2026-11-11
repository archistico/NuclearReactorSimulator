"""Small deterministic two-phase LP solver for offline minimax model review."""
import math

EPSILON = 1e-10
MAXIMUM_PIVOTS = 20000


def linear_program(a, b, c):
    """Maximize c*x subject to A*x <= b, x >= 0; return primal/dual certificates."""
    m, n = len(b), len(c)
    if not m or not n or any(len(row) != n for row in a):
        raise ValueError("Invalid linear program dimensions")
    if not all(math.isfinite(v) for row in a for v in row) or not all(math.isfinite(v) for v in b + c):
        raise ValueError("Nonfinite linear program")
    basic = [n + i for i in range(m)]
    nonbasic = list(range(n)) + [-1]
    table = [row[:] + [-1., rhs] for row, rhs in zip(a, b)]
    table += [[-v for v in c] + [0., 0.], [0.] * n + [1., 0.]]
    pivots = 0

    def pivot(row, column):
        nonlocal pivots
        pivots += 1
        if pivots > MAXIMUM_PIVOTS:
            raise ValueError("LP-PIVOT-BUDGET")
        inverse = 1. / table[row][column]
        pivot_row = table[row]
        for i, values in enumerate(table):
            if i == row:
                continue
            factor = values[column] * inverse
            if factor:
                for j in range(n + 2):
                    if j != column:
                        values[j] -= pivot_row[j] * factor
            values[column] *= -inverse
        for j in range(n + 2):
            if j != column:
                pivot_row[j] *= inverse
        pivot_row[column] = inverse
        basic[row], nonbasic[column] = nonbasic[column], basic[row]

    def optimize(phase):
        objective = m + 1 if phase == 1 else m
        while True:
            columns = [j for j in range(n + 1) if phase == 1 or nonbasic[j] != -1]
            column = min(columns, key=lambda j: (table[objective][j], nonbasic[j]))
            if table[objective][column] >= -EPSILON:
                return True
            rows = [i for i in range(m) if table[i][column] > EPSILON]
            if not rows:
                return False
            row = min(rows, key=lambda i: (table[i][-1] / table[i][column], basic[i]))
            pivot(row, column)

    row = min(range(m), key=lambda i: (table[i][-1], basic[i]))
    if table[row][-1] < -EPSILON:
        pivot(row, n)
        if not optimize(1) or abs(table[m + 1][-1]) > EPSILON:
            return dict(status="INFEASIBLE", pivots=pivots)
        if -1 in basic:
            row = basic.index(-1)
            column = min(range(n + 1), key=lambda j: (table[row][j], nonbasic[j]))
            if abs(table[row][column]) > EPSILON:
                pivot(row, column)
    if not optimize(2):
        return dict(status="UNBOUNDED", pivots=pivots)
    primal = [0.] * n
    for i, index in enumerate(basic):
        if 0 <= index < n:
            primal[index] = table[i][-1]
    dual = [0.] * m
    for j, index in enumerate(nonbasic):
        if index >= n:
            dual[index - n] = table[m][j]
    primal_objective = sum(v * x for v, x in zip(c, primal))
    dual_objective = sum(v * y for v, y in zip(b, dual))
    primal_violation = max([0.] + [-v for v in primal] + [sum(v * x for v, x in zip(row, primal)) - rhs for row, rhs in zip(a, b)])
    dual_violation = max([0.] + [-v for v in dual] + [c[j] - sum(a[i][j] * dual[i] for i in range(m)) for j in range(n)])
    gap = abs(primal_objective - dual_objective)
    tolerance = 1e-7 * max(1., abs(primal_objective), abs(dual_objective))
    if max(primal_violation, dual_violation, gap) > tolerance:
        raise ValueError("LP-CERTIFICATE-FAILED")
    return dict(status="OPTIMAL-CERTIFIED", primal=primal, dual=dual, objective=primal_objective,
                primal_violation=primal_violation, dual_violation=dual_violation, duality_gap=gap, pivots=pivots)


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
    secondary_rhs.append(optimum + 1e-9)
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
    tests = []
    for name, a, b, c, status, objective in [
        ("phase-one-required", [[1., 1.], [-1., 0.], [1., 0.], [0., 1.]], [1.5, -.25, 1., 1.], [1., 1.], "OPTIMAL-CERTIFIED", 1.5),
        ("infeasible", [[1.], [-1.]], [0., -1.], [1.], "INFEASIBLE", None),
        ("unbounded", [[-1.]], [0.], [1.], "UNBOUNDED", None),
    ]:
        result = linear_program(a, b, c)
        assert result["status"] == status, name
        assert objective is None or abs(result["objective"] - objective) < 1e-9, name
        tests.append(dict(name=name, passed=True))
    for name, matrix, residual, target, optimum in [
        ("opposite-to-least-squares", [[1.], [-1.], [-1.]], [10., 9., 9.], [-.5], 9.5),
        ("coupled-root", [[3., 1.], [1., 2.]], [-.018, -.011], [.005, .003], 0.),
        ("zero-column-kept-fixed", [[1., 0.], [1., 0.]], [-.005, -.005], [.005, 0.], 0.),
        ("joint-descent-missed-by-single-axes", [[100., 0.], [0., 100.]], [1., 1.], [-.01, -.01], 0.),
    ]:
        radius = 1. if name == "opposite-to-least-squares" else .02
        # The first fixture needs a larger mathematical box; scale it into the authored box.
        if radius == 1.:
            matrix = [[v * 100 for v in row] for row in matrix]
            target = [v / 100 for v in target]
            radius = .01
        result = minimax_step(matrix, residual, [0.] * len(target), radius)
        assert abs(result["primary_maximum"] - optimum) < 1e-7, name
        assert max(abs(a - b) for a, b in zip(result["raw_delta"], target)) < 1e-7, name
        tests.append(dict(name=name, passed=True))
    return tests


if __name__ == "__main__":
    import json
    print(json.dumps(fixtures(), indent=2))
