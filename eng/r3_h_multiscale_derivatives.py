"""Finite-difference diagnostics with explicit stencil availability and error bounds."""
import math

import r3_h_protocol as P


def column(base, samples, h):
    P.require(math.isfinite(h) and h > 0, "Invalid derivative width")
    P.require(len(samples) <= 2 and len({s for s, _ in samples}) == len(samples)
              and all(s in (-1, 1) for s, _ in samples), "Ambiguous derivative stencil")
    P.require(all(len(v) == len(base) for _, v in samples), "Derivative dimension drift")
    P.require(all(math.isfinite(v) for values in [base] + [v for _, v in samples] for v in values), "Nonfinite derivative input")
    if not samples:
        return None
    if len(samples) == 2:
        plus = next(v for s, v in samples if s == 1)
        minus = next(v for s, v in samples if s == -1)
        return [(a - b) / (2 * h) for a, b in zip(plus, minus)]
    sign, sample = samples[0]
    return [(a - b) / (sign * h) for a, b in zip(sample, base)]


def affine_error_bound(base_score, small_score, large_score, ratio, jacobian, small_delta, large_delta):
    # ||r + J*d||inf is convex. Allow for the fact that rounded coordinates
    # are not exactly collinear, rather than treating their nominal fractions as exact.
    P.require(0 < ratio < 1, "Invalid convexity ratio")
    mismatch = [a - ratio * b for a, b in zip(small_delta, large_delta)]
    allowance = max(abs(math.fsum(a * d for a, d in zip(row, mismatch))) for row in jacobian)
    defect = math.fsum((small_score, -(1 - ratio) * base_score, -ratio * large_score))
    return dict(nominal_convexity_defect=defect, coordinate_collinearity_error=max(map(abs, mismatch)),
                coordinate_rounding_allowance=allowance,
                uniform_residual_error_lower_bound=max(0., (defect - allowance) / (1 + ratio)))


def fixtures():
    x, h = 2., .125
    base = [x * x, 3 * x]
    samples = [(s, [(x + s * h) ** 2, 3 * (x + s * h)]) for s in (1, -1)]
    P.require(column(base, samples, h) == [4., 3.], "Analytic central derivative mismatch")
    P.require(column(base, samples[1:], h) == [4 - h, 3.], "Negative one-sided orientation mismatch")
    P.require(column(base, [], h) is None, "Unavailable derivative fabricated")
    try:
        column(base, [samples[0], samples[0]], h)
    except ValueError:
        pass
    else:
        raise ValueError("Duplicate stencil accepted")
    bound = affine_error_bound(10., 10.1, 9., .1, [[2.]], [.1], [1.])
    P.require(abs(bound["uniform_residual_error_lower_bound"] - .2 / 1.1) < 1e-14, "Affine error lower bound mismatch")
    rounded = affine_error_bound(10., 10.1, 9., .1, [[2.]], [.11], [1.])
    P.require(abs(rounded["coordinate_rounding_allowance"] - .02) < 1e-14
              and rounded["uniform_residual_error_lower_bound"] < bound["uniform_residual_error_lower_bound"], "Coordinate rounding not accounted for")
    return [dict(name=name, passed=True) for name in (
        "analytic-central-quadratic-and-linear", "negative-one-sided-orientation", "unavailable-is-not-zero",
        "duplicate-stencil-rejected", "affine-convexity-error-bound", "coordinate-rounding-allowance")]
