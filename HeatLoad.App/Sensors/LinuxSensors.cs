using System.Runtime.InteropServices;

namespace HeatLoad;

/// <summary>CPU/RAM from /proc, temperatures from hwmon, GPU from NVML or the amdgpu sysfs node.</summary>
internal sealed class LinuxSensors : ISensorSource
{
    private static readonly string[] CpuNames = ["coretemp", "k10temp", "zenpower", "cpu_thermal", "soc_thermal", "acpitz"];
    private static readonly string[] GpuNames = ["amdgpu", "nouveau", "nvidia"];

    private readonly List<string> _cpuHwmon = [];
    private readonly List<string> _gpuHwmon = [];
    private readonly string? _amdBusy;
    private IntPtr _nvmlDevice;
    private bool _nvmlReady;
    private ulong _prevIdle, _prevTotal;
    private bool _primed;

    public LinuxSensors()
    {
        try
        {
            foreach (var dir in Directory.EnumerateDirectories("/sys/class/hwmon"))
            {
                var name = Read(Path.Combine(dir, "name"))?.Trim();
                if (name is null) continue;
                if (CpuNames.Contains(name)) _cpuHwmon.Add(dir);
                else if (GpuNames.Contains(name)) _gpuHwmon.Add(dir);
            }
        }
        catch
        {
            // no hwmon: usage still works
        }

        try
        {
            _amdBusy = Directory.EnumerateFiles("/sys/class/drm", "gpu_busy_percent", SearchOption.AllDirectories)
                .FirstOrDefault();
        }
        catch
        {
            _amdBusy = null;
        }

        try
        {
            if (nvmlInit_v2() == 0)
            {
                _nvmlReady = true;
                if (nvmlDeviceGetCount_v2(out var count) == 0 && count > 0 &&
                    nvmlDeviceGetHandleByIndex_v2(0, out var device) == 0)
                    _nvmlDevice = device;
            }
        }
        catch
        {
            _nvmlReady = false; // no NVIDIA driver, that is not an error
        }
    }

    public Reading Sample()
    {
        var cpuUsage = Cpu();
        var (ramUsage, ramUsed) = Ram();
        var (gpuUsage, gpuTemp) = Gpu();
        var cpuTemp = MaxTemp(_cpuHwmon);
        var ramTemp = (double?)null;

        return new Reading(cpuUsage, gpuUsage, ramUsage, cpuTemp, gpuTemp, ramTemp, ramUsed);
    }

    private double? Cpu()
    {
        var line = Read("/proc/stat")?.Split('\n')
            .FirstOrDefault(l => l.StartsWith("cpu ", StringComparison.Ordinal));
        if (line is null) return null;

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5) return null;

        ulong total = 0;
        for (var i = 1; i < parts.Length && i <= 8; i++) // user..steal; guest times are already counted
        {
            if (ulong.TryParse(parts[i], out var v)) total += v;
        }
        ulong idle = ulong.TryParse(parts[4], out var i1) ? i1 : 0;
        if (ulong.TryParse(parts[5], out var iowait)) idle += iowait;

        var dTotal = total - _prevTotal;
        var dIdle = idle - _prevIdle;
        _prevTotal = total;
        _prevIdle = idle;

        if (!_primed) { _primed = true; return null; }
        if (dTotal == 0) return null;
        return Math.Clamp(100.0 * (dTotal - dIdle) / dTotal, 0, 100);
    }

    private static (double? Usage, double? UsedGb) Ram()
    {
        var text = Read("/proc/meminfo");
        if (text is null) return (null, null);

        ulong? total = null, available = null;
        foreach (var line in text.Split('\n'))
        {
            if (line.StartsWith("MemTotal:", StringComparison.Ordinal)) total = Kb(line);
            else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal)) available = Kb(line);
        }

        if (total is not > 0 || available is null) return (null, null);
        return (100.0 * (double)(total.Value - available.Value) / total.Value,
                (total.Value - available.Value) / 1048576.0);

        static ulong? Kb(string line)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 && ulong.TryParse(parts[1], out var v) ? v : null;
        }
    }

    private (double? Usage, double? Temp) Gpu()
    {
        if (_nvmlDevice != IntPtr.Zero)
        {
            double? usage = nvmlDeviceGetUtilizationRates(_nvmlDevice, out var rates) == 0 ? rates.gpu : null;
            double? temp = nvmlDeviceGetTemperature(_nvmlDevice, 0, out var t) == 0 ? t : null;
            return (usage, temp);
        }

        double? amdUsage = null;
        if (_amdBusy is not null && int.TryParse(Read(_amdBusy)?.Trim(), out var busy))
            amdUsage = busy;

        return (amdUsage, MaxTemp(_gpuHwmon));
    }

    private static double? MaxTemp(List<string> dirs)
    {
        double? max = null;
        foreach (var dir in dirs)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir, "temp*_input"))
                {
                    if (!long.TryParse(Read(file)?.Trim(), out var milli) || milli <= 0) continue;
                    var c = milli / 1000.0;
                    if (c is > 0 and < 150) max = Math.Max(max ?? 0, c);
                }
            }
            catch
            {
                // one broken node must not stop the rest
            }
        }
        return max;
    }

    private static string? Read(string path)
    {
        try { return File.ReadAllText(path); }
        catch { return null; }
    }

    public void Dispose()
    {
        if (_nvmlReady) nvmlShutdown();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlUtilization
    {
        public uint gpu;
        public uint memory;
    }

    [DllImport("libnvidia-ml.so.1")]
    private static extern int nvmlInit_v2();

    [DllImport("libnvidia-ml.so.1")]
    private static extern int nvmlShutdown();

    [DllImport("libnvidia-ml.so.1")]
    private static extern int nvmlDeviceGetCount_v2(out uint deviceCount);

    [DllImport("libnvidia-ml.so.1")]
    private static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

    [DllImport("libnvidia-ml.so.1")]
    private static extern int nvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtilization utilization);

    [DllImport("libnvidia-ml.so.1")]
    private static extern int nvmlDeviceGetTemperature(IntPtr device, uint sensorType, out uint temp);
}
