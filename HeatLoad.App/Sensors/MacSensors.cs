using System.Runtime.InteropServices;

namespace HeatLoad;

/// <summary>
/// macOS: CPU and RAM via Mach/sysctl. Temperatures and GPU load need IOKit/SMC access, so they stay empty for now.
/// </summary>
internal sealed class MacSensors : ISensorSource
{
    private readonly ulong _totalRam;
    private readonly uint _pageSize;
    private bool _primed;
    private ulong _prevUser, _prevSystem, _prevIdle, _prevNice;

    public MacSensors()
    {
        _totalRam = SysctlUlong("hw.memsize") ?? 0;
        _pageSize = (uint)(SysctlUlong("hw.pagesize") ?? 4096);
    }

    public Reading Sample()
    {
        double? cpuUsage = null;
        var info = new HostCpuLoadInfo();
        uint count = 4;
        if (host_statistics(mach_host_self(), HOST_CPU_LOAD_INFO, ref info, ref count) == 0)
        {
            var dUser = (ulong)info.User - _prevUser;
            var dSystem = (ulong)info.System - _prevSystem;
            var dIdle = (ulong)info.Idle - _prevIdle;
            var dNice = (ulong)info.Nice - _prevNice;
            _prevUser = info.User;
            _prevSystem = info.System;
            _prevIdle = info.Idle;
            _prevNice = info.Nice;

            var busy = dUser + dSystem + dNice;
            var total = busy + dIdle;
            if (_primed && total > 0) cpuUsage = Math.Clamp(100.0 * busy / total, 0, 100);
            _primed = true;
        }

        double? ramUsage = null, ramUsed = null;
        if (_totalRam > 0 && _pageSize > 0)
        {
            var free = SysctlInt("vm.page_free_count");
            var inactive = SysctlInt("vm.page_inactive_count");
            var speculative = SysctlInt("vm.page_speculative_count");
            if (free is { } f && inactive is { } i && speculative is { } s)
            {
                var available = ((ulong)(f + i + s)) * _pageSize;
                if (available < _totalRam)
                {
                    ramUsage = 100.0 * (double)(_totalRam - available) / _totalRam;
                    ramUsed = (_totalRam - available) / 1073741824.0;
                }
            }
        }

        return new Reading(cpuUsage, null, ramUsage, null, null, null, ramUsed);
    }

    public void Dispose() { }

    private static ulong? SysctlUlong(string name)
    {
        ulong value = 0;
        nuint size = sizeof(ulong);
        return sysctlbyname(name, ref value, ref size, IntPtr.Zero, 0) == 0 ? value : null;
    }

    private static int? SysctlInt(string name)
    {
        int value = 0;
        nuint size = sizeof(int);
        return sysctlbyname(name, ref value, ref size, IntPtr.Zero, 0) == 0 ? value : null;
    }

    private const int HOST_CPU_LOAD_INFO = 3;

    [StructLayout(LayoutKind.Sequential)]
    private struct HostCpuLoadInfo
    {
        public uint User;
        public uint System;
        public uint Idle;
        public uint Nice;
    }

    [DllImport("libSystem.B.dylib")]
    private static extern IntPtr mach_host_self();

    [DllImport("libSystem.B.dylib")]
    private static extern int host_statistics(IntPtr host, int flavor, ref HostCpuLoadInfo info, ref uint count);

    [DllImport("libSystem.B.dylib")]
    private static extern int sysctlbyname(string name, ref ulong oldp, ref nuint oldlenp, IntPtr newp, nuint newlen);

    [DllImport("libSystem.B.dylib")]
    private static extern int sysctlbyname(string name, ref int oldp, ref nuint oldlenp, IntPtr newp, nuint newlen);
}
