namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;

/// <summary>
/// Minimal test-only implementation of the IAPWS-IF97 basic equations needed by M10 Final VR2.
/// Source authority: IAPWS R7-97(2012), Regions 1, 2 and 4. This helper is independent from
/// SimplifiedWaterSteamThermodynamicModel and is never referenced by production code.
/// </summary>
internal static class IapwsIf97Reference
{
    private const double SpecificGasConstantKilojoulesPerKilogramKelvin = 0.461526d;

    private static readonly GibbsCoefficient[] Region1Coefficients =
    [
        new(0, -2, 0.14632971213167d),
        new(0, -1, -0.84548187169114d),
        new(0, 0, -3.7563603672040d),
        new(0, 1, 3.3855169168385d),
        new(0, 2, -0.95791963387872d),
        new(0, 3, 0.15772038513228d),
        new(0, 4, -0.16616417199501e-1d),
        new(0, 5, 0.81214629983568e-3d),
        new(1, -9, 0.28319080123804e-3d),
        new(1, -7, -0.60706301565874e-3d),
        new(1, -1, -0.18990068218419e-1d),
        new(1, 0, -0.32529748770505e-1d),
        new(1, 1, -0.21841717175414e-1d),
        new(1, 3, -0.52838357969930e-4d),
        new(2, -3, -0.47184321073267e-3d),
        new(2, 0, -0.30001780793026e-3d),
        new(2, 1, 0.47661393906987e-4d),
        new(2, 3, -0.44141845330846e-5d),
        new(2, 17, -0.72694996297594e-15d),
        new(3, -4, -0.31679644845054e-4d),
        new(3, 0, -0.28270797985312e-5d),
        new(3, 6, -0.85205128120103e-9d),
        new(4, -5, -0.22425281908000e-5d),
        new(4, -2, -0.65171222895601e-6d),
        new(4, 10, -0.14341729937924e-12d),
        new(5, -8, -0.40516996860117e-6d),
        new(8, -11, -0.12734301741641e-8d),
        new(8, -6, -0.17424871230634e-9d),
        new(21, -29, -0.68762131295531e-18d),
        new(23, -31, 0.14478307828521e-19d),
        new(29, -38, 0.26335781662795e-22d),
        new(30, -39, -0.11947622640071e-22d),
        new(31, -40, 0.18228094581404e-23d),
        new(32, -41, -0.93537087292458e-25d),
    ];

    private static readonly IdealCoefficient[] Region2IdealCoefficients =
    [
        new(0, -0.96927686500217e1d),
        new(1, 0.10086655968018e2d),
        new(-5, -0.56087911283020e-2d),
        new(-4, 0.71452738081455e-1d),
        new(-3, -0.40710498223928d),
        new(-2, 0.14240819171444e1d),
        new(-1, -0.43839511319450e1d),
        new(2, -0.28408632460772d),
        new(3, 0.21268463753307e-1d),
    ];

    private static readonly GibbsCoefficient[] Region2ResidualCoefficients =
    [
        new(1, 0, -0.17731742473213e-2d),
        new(1, 1, -0.17834862292358e-1d),
        new(1, 2, -0.45996013696365e-1d),
        new(1, 3, -0.57581259083432e-1d),
        new(1, 6, -0.50325278727930e-1d),
        new(2, 1, -0.33032641670203e-4d),
        new(2, 2, -0.18948987516315e-3d),
        new(2, 4, -0.39392777243355e-2d),
        new(2, 7, -0.43797295650573e-1d),
        new(2, 36, -0.26674547914087e-4d),
        new(3, 0, 0.20481737692309e-7d),
        new(3, 1, 0.43870667284435e-6d),
        new(3, 3, -0.32277677238570e-4d),
        new(3, 6, -0.15033924542148e-2d),
        new(3, 35, -0.40668253562649e-1d),
        new(4, 1, -0.78847309559367e-9d),
        new(4, 2, 0.12790717852285e-7d),
        new(4, 3, 0.48225372718507e-6d),
        new(5, 7, 0.22922076337661e-5d),
        new(6, 3, -0.16714766451061e-10d),
        new(6, 16, -0.21171472321355e-2d),
        new(6, 35, -0.23895741934104e2d),
        new(7, 0, -0.59059564324270e-17d),
        new(7, 11, -0.12621808899101e-5d),
        new(7, 25, -0.38946842435739e-1d),
        new(8, 8, 0.11256211360459e-10d),
        new(8, 36, -0.82311340897998e1d),
        new(9, 13, 0.19809712802088e-7d),
        new(10, 4, 0.10406965210174e-18d),
        new(10, 10, -0.10234747095929e-12d),
        new(10, 14, -0.10018179379511e-8d),
        new(16, 29, -0.80882908646985e-10d),
        new(16, 50, 0.10693031879409d),
        new(18, 57, -0.33662250574171d),
        new(20, 20, 0.89185845355421e-24d),
        new(20, 35, 0.30629316876232e-12d),
        new(20, 48, -0.42002467698208e-5d),
        new(21, 21, -0.59056029685639e-25d),
        new(22, 53, 0.37826947613457e-5d),
        new(23, 39, -0.12768608934681e-14d),
        new(24, 26, 0.73087610595061e-28d),
        new(24, 40, 0.55414715350778e-16d),
        new(24, 58, -0.94369707241210e-6d),
    ];

    private static readonly double[] Region4Coefficients =
    [
        0d,
        0.11670521452767e4d,
        -0.72421316703206e6d,
        -0.17073846940092e2d,
        0.12020824702470e5d,
        -0.32325550322333e7d,
        0.14915108613530e2d,
        -0.48232657361591e4d,
        0.40511340542057e6d,
        -0.23855557567849d,
        0.65017534844798e3d,
    ];

    public static IapwsReferenceState Region1(double temperatureKelvins, double pressureMegapascals)
    {
        if (!double.IsFinite(temperatureKelvins) || temperatureKelvins <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(temperatureKelvins));
        }

        if (!double.IsFinite(pressureMegapascals) || pressureMegapascals <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(pressureMegapascals));
        }

        var pi = pressureMegapascals / 16.53d;
        var tau = 1386d / temperatureKelvins;
        var piBase = 7.1d - pi;
        var tauBase = tau - 1.222d;
        var gammaPi = 0d;
        var gammaTau = 0d;

        foreach (var coefficient in Region1Coefficients)
        {
            if (coefficient.I != 0)
            {
                gammaPi -= coefficient.N
                    * coefficient.I
                    * Math.Pow(piBase, coefficient.I - 1)
                    * Math.Pow(tauBase, coefficient.J);
            }

            if (coefficient.J != 0)
            {
                gammaTau += coefficient.N
                    * Math.Pow(piBase, coefficient.I)
                    * coefficient.J
                    * Math.Pow(tauBase, coefficient.J - 1);
            }
        }

        return CreateState(temperatureKelvins, pressureMegapascals, pi, tau, gammaPi, gammaTau);
    }

    public static IapwsReferenceState Region2(double temperatureKelvins, double pressureMegapascals)
    {
        if (!double.IsFinite(temperatureKelvins) || temperatureKelvins <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(temperatureKelvins));
        }

        if (!double.IsFinite(pressureMegapascals) || pressureMegapascals <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(pressureMegapascals));
        }

        var pi = pressureMegapascals;
        var tau = 540d / temperatureKelvins;
        var residualTauBase = tau - 0.5d;
        var gammaPi = 1d / pi;
        var gammaTau = 0d;

        foreach (var coefficient in Region2IdealCoefficients)
        {
            if (coefficient.J != 0)
            {
                gammaTau += coefficient.N * coefficient.J * Math.Pow(tau, coefficient.J - 1);
            }
        }

        foreach (var coefficient in Region2ResidualCoefficients)
        {
            if (coefficient.I != 0)
            {
                gammaPi += coefficient.N
                    * coefficient.I
                    * Math.Pow(pi, coefficient.I - 1)
                    * Math.Pow(residualTauBase, coefficient.J);
            }

            if (coefficient.J != 0)
            {
                gammaTau += coefficient.N
                    * Math.Pow(pi, coefficient.I)
                    * coefficient.J
                    * Math.Pow(residualTauBase, coefficient.J - 1);
            }
        }

        return CreateState(temperatureKelvins, pressureMegapascals, pi, tau, gammaPi, gammaTau);
    }

    public static double SaturationPressureMegapascals(double temperatureKelvins)
    {
        if (!double.IsFinite(temperatureKelvins) || temperatureKelvins < 273.15d || temperatureKelvins > 647.096d)
        {
            throw new ArgumentOutOfRangeException(nameof(temperatureKelvins));
        }

        var n = Region4Coefficients;
        var theta = temperatureKelvins + (n[9] / (temperatureKelvins - n[10]));
        var a = (theta * theta) + (n[1] * theta) + n[2];
        var b = (n[3] * theta * theta) + (n[4] * theta) + n[5];
        var c = (n[6] * theta * theta) + (n[7] * theta) + n[8];
        return Math.Pow((2d * c) / (-b + Math.Sqrt((b * b) - (4d * a * c))), 4d);
    }

    public static double SaturationTemperatureKelvins(double pressureMegapascals)
    {
        if (!double.IsFinite(pressureMegapascals) || pressureMegapascals < 0.000611213d || pressureMegapascals > 22.064d)
        {
            throw new ArgumentOutOfRangeException(nameof(pressureMegapascals));
        }

        var n = Region4Coefficients;
        var beta = Math.Pow(pressureMegapascals, 0.25d);
        var e = (beta * beta) + (n[3] * beta) + n[6];
        var f = (n[1] * beta * beta) + (n[4] * beta) + n[7];
        var g = (n[2] * beta * beta) + (n[5] * beta) + n[8];
        var d = (2d * g) / (-f - Math.Sqrt((f * f) - (4d * e * g)));
        return (n[10] + d - Math.Sqrt(Math.Pow(n[10] + d, 2d) - (4d * (n[9] + (n[10] * d))))) / 2d;
    }

    public static bool TryResolveRegion1FromSpecificVolumeAndInternalEnergy(
        double specificVolumeCubicMetresPerKilogram,
        double specificInternalEnergyJoulesPerKilogram,
        out IapwsInverseReferenceState state)
    {
        const int scanSegments = 400;
        const int bisectionIterations = 90;

        if (!double.IsFinite(specificVolumeCubicMetresPerKilogram)
            || specificVolumeCubicMetresPerKilogram <= 0d
            || !double.IsFinite(specificInternalEnergyJoulesPerKilogram))
        {
            state = default;
            return false;
        }

        const double minimumTemperatureKelvins = 273.15d;
        const double maximumTemperatureKelvins = 623.15d;
        var energyTolerance = Math.Max(1e-5d, Math.Abs(specificInternalEnergyJoulesPerKilogram) * 1e-10d);
        var previousFound = false;
        var previousTemperature = 0d;
        var previousResidual = 0d;
        double? lastUnreachableTemperature = null;

        for (var index = 0; index <= scanSegments; index++)
        {
            var temperature = minimumTemperatureKelvins
                + ((maximumTemperatureKelvins - minimumTemperatureKelvins) * index / scanSegments);
            if (!TryRegion1StateAtSpecificVolume(temperature, specificVolumeCubicMetresPerKilogram, out var candidate))
            {
                previousFound = false;
                lastUnreachableTemperature = temperature;
                continue;
            }

            var residual = candidate.SpecificInternalEnergyJoulesPerKilogram - specificInternalEnergyJoulesPerKilogram;
            if (Math.Abs(residual) <= energyTolerance)
            {
                state = ToInverseState(candidate, "REGION-1", null);
                return true;
            }

            if (!previousFound
                && lastUnreachableTemperature.HasValue
                && TryFindFirstReachableRegion1Temperature(
                    lastUnreachableTemperature.Value,
                    temperature,
                    specificVolumeCubicMetresPerKilogram,
                    out var boundaryTemperature,
                    out var boundaryState))
            {
                var boundaryResidual = boundaryState.SpecificInternalEnergyJoulesPerKilogram
                    - specificInternalEnergyJoulesPerKilogram;
                if (Math.Abs(boundaryResidual) <= energyTolerance)
                {
                    state = ToInverseState(boundaryState, "REGION-1", null);
                    return true;
                }

                previousFound = true;
                previousTemperature = boundaryTemperature;
                previousResidual = boundaryResidual;
            }

            if (previousFound && Math.Sign(previousResidual) != Math.Sign(residual))
            {
                var lowerTemperature = previousTemperature;
                var upperTemperature = temperature;
                var lowerResidual = previousResidual;
                var middleState = candidate;

                for (var iteration = 0; iteration < bisectionIterations; iteration++)
                {
                    var middleTemperature = 0.5d * (lowerTemperature + upperTemperature);
                    if (!TryRegion1StateAtSpecificVolume(middleTemperature, specificVolumeCubicMetresPerKilogram, out middleState))
                    {
                        state = default;
                        return false;
                    }

                    var middleResidual = middleState.SpecificInternalEnergyJoulesPerKilogram - specificInternalEnergyJoulesPerKilogram;
                    if (Math.Abs(middleResidual) <= energyTolerance)
                    {
                        state = ToInverseState(middleState, "REGION-1", null);
                        return true;
                    }

                    if (Math.Sign(lowerResidual) == Math.Sign(middleResidual))
                    {
                        lowerTemperature = middleTemperature;
                        lowerResidual = middleResidual;
                    }
                    else
                    {
                        upperTemperature = middleTemperature;
                    }
                }

                state = ToInverseState(middleState, "REGION-1", null);
                return true;
            }

            previousFound = true;
            previousTemperature = temperature;
            previousResidual = residual;
            lastUnreachableTemperature = null;
        }

        state = default;
        return false;
    }

    private static bool TryFindFirstReachableRegion1Temperature(
        double unreachableTemperatureKelvins,
        double reachableTemperatureKelvins,
        double targetSpecificVolume,
        out double boundaryTemperatureKelvins,
        out IapwsReferenceState state)
    {
        const int boundaryBisectionIterations = 60;
        var lowerTemperature = unreachableTemperatureKelvins;
        var upperTemperature = reachableTemperatureKelvins;

        if (IsRegion1SpecificVolumeReachable(lowerTemperature, targetSpecificVolume)
            || !IsRegion1SpecificVolumeReachable(upperTemperature, targetSpecificVolume))
        {
            boundaryTemperatureKelvins = double.NaN;
            state = default;
            return false;
        }

        for (var iteration = 0; iteration < boundaryBisectionIterations; iteration++)
        {
            var middleTemperature = 0.5d * (lowerTemperature + upperTemperature);
            if (IsRegion1SpecificVolumeReachable(middleTemperature, targetSpecificVolume))
            {
                upperTemperature = middleTemperature;
            }
            else
            {
                lowerTemperature = middleTemperature;
            }
        }

        boundaryTemperatureKelvins = upperTemperature;
        return TryRegion1StateAtSpecificVolume(boundaryTemperatureKelvins, targetSpecificVolume, out state);
    }

    private static bool IsRegion1SpecificVolumeReachable(double temperatureKelvins, double targetSpecificVolume)
    {
        var minimumPressure = SaturationPressureMegapascals(temperatureKelvins);
        var lowState = Region1(temperatureKelvins, minimumPressure);
        var highState = Region1(temperatureKelvins, 100d);
        var volumeTolerance = Math.Max(1e-15d, targetSpecificVolume * 1e-10d);
        return targetSpecificVolume <= lowState.SpecificVolumeCubicMetresPerKilogram + volumeTolerance
            && targetSpecificVolume >= highState.SpecificVolumeCubicMetresPerKilogram - volumeTolerance;
    }

    public static bool TryResolveSaturatedMixtureFromSpecificVolumeAndInternalEnergy(
        double specificVolumeCubicMetresPerKilogram,
        double specificInternalEnergyJoulesPerKilogram,
        out IapwsInverseReferenceState state)
    {
        const int scanSegments = 500;
        const int bisectionIterations = 90;

        if (!double.IsFinite(specificVolumeCubicMetresPerKilogram)
            || specificVolumeCubicMetresPerKilogram <= 0d
            || !double.IsFinite(specificInternalEnergyJoulesPerKilogram))
        {
            state = default;
            return false;
        }

        const double minimumTemperatureKelvins = 273.15d;
        const double maximumTemperatureKelvins = 623.15d;
        var energyTolerance = Math.Max(1e-5d, Math.Abs(specificInternalEnergyJoulesPerKilogram) * 1e-10d);
        var previousFound = false;
        var previousTemperature = 0d;
        var previousResidual = 0d;

        for (var index = 0; index <= scanSegments; index++)
        {
            var temperature = minimumTemperatureKelvins
                + ((maximumTemperatureKelvins - minimumTemperatureKelvins) * index / scanSegments);
            if (!TrySaturatedMixtureAtTemperature(
                    temperature,
                    specificVolumeCubicMetresPerKilogram,
                    specificInternalEnergyJoulesPerKilogram,
                    out var candidate,
                    out var residual))
            {
                previousFound = false;
                continue;
            }

            if (Math.Abs(residual) <= energyTolerance)
            {
                state = candidate;
                return true;
            }

            if (previousFound && Math.Sign(previousResidual) != Math.Sign(residual))
            {
                var lowerTemperature = previousTemperature;
                var upperTemperature = temperature;
                var lowerResidual = previousResidual;
                var middleState = candidate;

                for (var iteration = 0; iteration < bisectionIterations; iteration++)
                {
                    var middleTemperature = 0.5d * (lowerTemperature + upperTemperature);
                    if (!TrySaturatedMixtureAtTemperature(
                            middleTemperature,
                            specificVolumeCubicMetresPerKilogram,
                            specificInternalEnergyJoulesPerKilogram,
                            out middleState,
                            out var middleResidual))
                    {
                        state = default;
                        return false;
                    }

                    if (Math.Abs(middleResidual) <= energyTolerance)
                    {
                        state = middleState;
                        return true;
                    }

                    if (Math.Sign(lowerResidual) == Math.Sign(middleResidual))
                    {
                        lowerTemperature = middleTemperature;
                        lowerResidual = middleResidual;
                    }
                    else
                    {
                        upperTemperature = middleTemperature;
                    }
                }

                state = middleState;
                return true;
            }

            previousFound = true;
            previousTemperature = temperature;
            previousResidual = residual;
        }

        state = default;
        return false;
    }

    private static bool TryRegion1StateAtSpecificVolume(
        double temperatureKelvins,
        double targetSpecificVolume,
        out IapwsReferenceState state)
    {
        const int pressureBisectionIterations = 90;
        var minimumPressure = SaturationPressureMegapascals(temperatureKelvins);
        const double maximumPressure = 100d;
        var lowState = Region1(temperatureKelvins, minimumPressure);
        var highState = Region1(temperatureKelvins, maximumPressure);
        var volumeTolerance = Math.Max(1e-15d, targetSpecificVolume * 1e-10d);

        if (targetSpecificVolume > lowState.SpecificVolumeCubicMetresPerKilogram + volumeTolerance
            || targetSpecificVolume < highState.SpecificVolumeCubicMetresPerKilogram - volumeTolerance)
        {
            state = default;
            return false;
        }

        if (Math.Abs(targetSpecificVolume - lowState.SpecificVolumeCubicMetresPerKilogram) <= volumeTolerance)
        {
            state = lowState;
            return true;
        }

        if (Math.Abs(targetSpecificVolume - highState.SpecificVolumeCubicMetresPerKilogram) <= volumeTolerance)
        {
            state = highState;
            return true;
        }

        var lowerPressure = minimumPressure;
        var upperPressure = maximumPressure;
        var middleState = lowState;
        for (var iteration = 0; iteration < pressureBisectionIterations; iteration++)
        {
            var middlePressure = 0.5d * (lowerPressure + upperPressure);
            middleState = Region1(temperatureKelvins, middlePressure);
            var difference = middleState.SpecificVolumeCubicMetresPerKilogram - targetSpecificVolume;
            if (Math.Abs(difference) <= volumeTolerance)
            {
                state = middleState;
                return true;
            }

            if (difference > 0d)
            {
                lowerPressure = middlePressure;
            }
            else
            {
                upperPressure = middlePressure;
            }
        }

        state = middleState;
        return true;
    }

    private static bool TrySaturatedMixtureAtTemperature(
        double temperatureKelvins,
        double targetSpecificVolume,
        double targetSpecificInternalEnergy,
        out IapwsInverseReferenceState state,
        out double energyResidual)
    {
        var pressure = SaturationPressureMegapascals(temperatureKelvins);
        var liquid = Region1(temperatureKelvins, pressure);
        var vapor = Region2(temperatureKelvins, pressure);
        var denominator = vapor.SpecificVolumeCubicMetresPerKilogram - liquid.SpecificVolumeCubicMetresPerKilogram;
        if (!(denominator > 0d))
        {
            state = default;
            energyResidual = double.NaN;
            return false;
        }

        var quality = (targetSpecificVolume - liquid.SpecificVolumeCubicMetresPerKilogram) / denominator;
        const double qualityTolerance = 1e-10d;
        if (quality < -qualityTolerance || quality > 1d + qualityTolerance)
        {
            state = default;
            energyResidual = double.NaN;
            return false;
        }

        quality = Math.Clamp(quality, 0d, 1d);
        var predictedInternalEnergy = liquid.SpecificInternalEnergyJoulesPerKilogram
            + (quality * (vapor.SpecificInternalEnergyJoulesPerKilogram - liquid.SpecificInternalEnergyJoulesPerKilogram));
        energyResidual = predictedInternalEnergy - targetSpecificInternalEnergy;
        state = new IapwsInverseReferenceState(
            "REGION-4-MIXTURE",
            temperatureKelvins,
            pressure,
            targetSpecificVolume,
            1d / targetSpecificVolume,
            targetSpecificInternalEnergy,
            quality);
        return true;
    }

    private static IapwsInverseReferenceState ToInverseState(
        IapwsReferenceState state,
        string region,
        double? vaporQuality)
        => new(
            region,
            state.TemperatureKelvins,
            state.PressureMegapascals,
            state.SpecificVolumeCubicMetresPerKilogram,
            state.DensityKilogramsPerCubicMetre,
            state.SpecificInternalEnergyJoulesPerKilogram,
            vaporQuality);

    private static IapwsReferenceState CreateState(
        double temperatureKelvins,
        double pressureMegapascals,
        double pi,
        double tau,
        double gammaPi,
        double gammaTau)
    {
        var specificVolume = (pi * gammaPi * SpecificGasConstantKilojoulesPerKilogramKelvin * temperatureKelvins)
            / (pressureMegapascals * 1_000d);
        var internalEnergyKilojoulesPerKilogram = SpecificGasConstantKilojoulesPerKilogramKelvin
            * temperatureKelvins
            * ((tau * gammaTau) - (pi * gammaPi));

        return new IapwsReferenceState(
            temperatureKelvins,
            pressureMegapascals,
            specificVolume,
            1d / specificVolume,
            internalEnergyKilojoulesPerKilogram * 1_000d);
    }

    private readonly record struct GibbsCoefficient(int I, int J, double N);

    private readonly record struct IdealCoefficient(int J, double N);
}

internal readonly record struct IapwsReferenceState(
    double TemperatureKelvins,
    double PressureMegapascals,
    double SpecificVolumeCubicMetresPerKilogram,
    double DensityKilogramsPerCubicMetre,
    double SpecificInternalEnergyJoulesPerKilogram);

internal readonly record struct IapwsInverseReferenceState(
    string Region,
    double TemperatureKelvins,
    double PressureMegapascals,
    double SpecificVolumeCubicMetresPerKilogram,
    double DensityKilogramsPerCubicMetre,
    double SpecificInternalEnergyJoulesPerKilogram,
    double? VaporQuality);
