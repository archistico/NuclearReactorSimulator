namespace NuclearReactorSimulator.Application.Validation;

public enum ReferenceSensitivityDirection
{
    Increase = 0,
    Decrease = 1,
    AnyNonZero = 2,
    NoMaterialChange = 3,
}
