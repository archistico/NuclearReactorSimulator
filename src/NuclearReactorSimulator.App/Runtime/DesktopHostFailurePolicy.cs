namespace NuclearReactorSimulator.App.Runtime;

/// <summary>
/// Desktop-host exception classification. Only failures that represent expected fail-closed runtime/session boundaries
/// are converted into operator-visible status. Programming defects and unknown exceptions remain unhandled on purpose.
/// </summary>
internal static class DesktopHostFailurePolicy
{
    public static bool IsExpectedDeterministicStepFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is InvalidOperationException or ArithmeticException;
    }

    public static bool IsExpectedRuntimeConstructionFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is InvalidOperationException or ArgumentException or ArithmeticException;
    }

    public static bool IsExpectedArchiveOperationFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is InvalidOperationException
            or InvalidDataException
            or IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException
            or KeyNotFoundException
            or ArithmeticException;
    }
}
