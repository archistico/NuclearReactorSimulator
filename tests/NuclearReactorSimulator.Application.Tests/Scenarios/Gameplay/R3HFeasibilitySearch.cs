namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>Test-only implementation of the frozen H search; never a runtime correction.</summary>
internal sealed class R3HFeasibilitySearch(int dimensions, int evaluationLimit = 4096)
{
    internal sealed record Sample(double[]? Residuals, string Reason, string[]? Phases = null)
    {
        internal static Sample Invalid(string reason) => new(null, reason);
    }
    internal sealed record Event(int Id, int Iteration, string Purpose, double[] Coordinates,
        double[]? Residuals, double? Score, string Reason, string[]? Phases)
    {
        public bool Accepted { get; set; }
    }
    internal sealed record Result(double[] Coordinates, double[]? Residuals, bool Root, string Stop, int Evaluations);
    internal List<Event> Trace { get; } = [];

    internal Result Run(Func<double[], Sample> evaluate)
    {
        if (dimensions <= 0 || evaluationLimit <= 0) throw new ArgumentOutOfRangeException(nameof(dimensions));
        if (Trace.Count != 0) throw new InvalidOperationException("Search instances are single-use.");
        var coordinates = new double[dimensions];
        var current = Evaluate(coordinates, 0, "initial", evaluate);
        if (current is null || current.Residuals is null) return Finish(coordinates, current, false, "INVALID-INITIAL-EVALUATION");
        current.Accepted = true;
        var damping = 1e-3;
        for (var iteration = 1; iteration <= 64; iteration++)
        {
            if (current.Score <= 1) return Finish(coordinates, current, true, "FIRST-SCALED-ROOT");
            var jacobian = new double[current.Residuals.Length, dimensions];
            for (var column = 0; column < dimensions; column++)
            {
                var plus = (double[])coordinates.Clone();
                var minus = (double[])coordinates.Clone();
                plus[column] += 1e-6;
                minus[column] -= 1e-6;
                var forward = Evaluate(plus, iteration, $"difference+/{column}", evaluate);
                var backward = Evaluate(minus, iteration, $"difference-/{column}", evaluate);
                if (forward?.Residuals is null || backward?.Residuals is null)
                    return Finish(coordinates, current, false, Trace.Count >= evaluationLimit ? "RECIPE-BUDGET" : "INVALID-DIFFERENCE-PROBE");
                if (forward.Residuals.Length != current.Residuals.Length || backward.Residuals.Length != current.Residuals.Length)
                    throw new InvalidOperationException("Residual dimension changed within a search.");
                for (var row = 0; row < current.Residuals.Length; row++)
                {
                    jacobian[row, column] = (forward.Residuals[row] - backward.Residuals[row]) / 2e-6;
                    if (!double.IsFinite(jacobian[row, column])) return Finish(coordinates, current, false, "NONFINITE-JACOBIAN");
                }
            }
            var accepted = false;
            for (var attempt = 0; attempt < 6 && !accepted; attempt++)
            {
                var delta = Solve(jacobian, current.Residuals, damping);
                if (delta is not null)
                {
                    // A full step followed by at most twelve successive halvings.
                    for (var halving = 0; halving <= 12; halving++)
                    {
                        var alpha = Math.ScaleB(1d, -halving);
                        var trial = coordinates.Select((value, index) => value + alpha * delta[index]).ToArray();
                        var candidate = Evaluate(trial, iteration, $"trial/{attempt}/{halving}", evaluate);
                        if (candidate is null) return Finish(coordinates, current, false, "RECIPE-BUDGET");
                        if (candidate.Residuals is null || !(candidate.Score < current.Score)) continue;
                        candidate.Accepted = true;
                        coordinates = trial;
                        current = candidate;
                        damping = Math.Max(1e-12, damping / 10d);
                        accepted = true;
                        break;
                    }
                }
                if (!accepted) damping = Math.Min(1e12, damping * 10d);
            }
            if (!accepted) return Finish(coordinates, current, false, "DAMPING-OR-LINE-SEARCH-EXHAUSTED");
        }
        return Finish(coordinates, current, current.Score <= 1, current.Score <= 1 ? "FIRST-SCALED-ROOT" : "ITERATION-BUDGET");
    }

    private Event? Evaluate(double[] coordinates, int iteration, string purpose, Func<double[], Sample> evaluate)
    {
        if (Trace.Count >= evaluationLimit) return null;
        var sample = coordinates.All(static x => double.IsFinite(x) && Math.Abs(x) <= 0.02)
            ? evaluate((double[])coordinates.Clone()) : Sample.Invalid("OUTSIDE-FROZEN-BOUNDS");
        if (sample.Residuals is { Length: 0 }) throw new InvalidOperationException("Empty residual vector.");
        if (sample.Residuals is not null && !sample.Residuals.All(double.IsFinite)) sample = Sample.Invalid("NONFINITE-RESIDUAL");
        var result = new Event(Trace.Count + 1, iteration, purpose, (double[])coordinates.Clone(), sample.Residuals,
            sample.Residuals?.Max(static x => Math.Abs(x)), sample.Reason, sample.Phases);
        Trace.Add(result);
        return result;
    }

    private Result Finish(double[] coordinates, Event? current, bool root, string stop)
        => new((double[])coordinates.Clone(), current?.Residuals, root, stop, Trace.Count);

    internal static double[]? Solve(double[,] jacobian, double[] residuals, double damping)
    {
        var columns = jacobian.GetLength(1);
        var rows = jacobian.GetLength(0);
        if (rows != residuals.Length) throw new ArgumentException("Jacobian row mismatch.");
        var matrix = new double[columns, columns + 1];
        var rowOrder = Enumerable.Range(0, columns).ToArray();
        var magnitude = 0d;
        for (var i = 0; i < columns; i++)
        {
            for (var j = 0; j < columns; j++)
            {
                var sum = i == j ? damping : 0d;
                for (var row = 0; row < rows; row++) sum += jacobian[row, i] * jacobian[row, j];
                matrix[i, j] = sum;
                magnitude = Math.Max(magnitude, Math.Abs(sum));
            }
            for (var row = 0; row < rows; row++) matrix[i, columns] -= jacobian[row, i] * residuals[row];
        }
        if (!(magnitude > 0) || !double.IsFinite(magnitude)) return null;
        for (var column = 0; column < columns; column++)
        {
            var pivot = column;
            for (var row = column + 1; row < columns; row++)
                if (Math.Abs(matrix[row, column]) > Math.Abs(matrix[pivot, column])
                    || (Math.Abs(matrix[row, column]) == Math.Abs(matrix[pivot, column]) && rowOrder[row] < rowOrder[pivot])) pivot = row;
            if (!double.IsFinite(matrix[pivot, column]) || Math.Abs(matrix[pivot, column]) / magnitude <= 1e-14) return null;
            if (pivot != column)
            {
                for (var j = column; j <= columns; j++) (matrix[column, j], matrix[pivot, j]) = (matrix[pivot, j], matrix[column, j]);
                (rowOrder[column], rowOrder[pivot]) = (rowOrder[pivot], rowOrder[column]);
            }
            for (var row = column + 1; row < columns; row++)
            {
                var ratio = matrix[row, column] / matrix[column, column];
                for (var j = column; j <= columns; j++) matrix[row, j] -= ratio * matrix[column, j];
            }
        }
        var solution = new double[columns];
        for (var row = columns - 1; row >= 0; row--)
        {
            var value = matrix[row, columns];
            for (var column = row + 1; column < columns; column++) value -= matrix[row, column] * solution[column];
            solution[row] = value / matrix[row, row];
        }
        return solution.All(double.IsFinite) ? solution : null;
    }
}
