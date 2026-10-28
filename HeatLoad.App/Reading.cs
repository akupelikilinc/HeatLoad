namespace HeatLoad;

internal readonly record struct Reading(
    double? CpuUsage,
    double? GpuUsage,
    double? RamUsage,
    double? CpuTemp,
    double? GpuTemp,
    double? RamTemp,
    double? RamUsedGb);

internal interface ISensorSource : IDisposable
{
    Reading Sample();
}

internal static class Sensors
{
    public static ISensorSource Create() =>
        OperatingSystem.IsWindows() ? new WindowsSensors()
        : OperatingSystem.IsLinux() ? new LinuxSensors()
        : OperatingSystem.IsMacOS() ? new MacSensors()
        : new EmptySensors();
}

internal sealed class EmptySensors : ISensorSource
{
    public Reading Sample() => default;
    public void Dispose() { }
}
