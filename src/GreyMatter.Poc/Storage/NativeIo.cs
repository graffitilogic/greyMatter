using GreyMatter.Poc.Eval;

namespace GreyMatter.Poc.Storage;

/// <summary>
/// Item 4 (2026-09-21): one process-resource sampler with the macOS <see cref="MacFileIo.Usage"/>
/// shape on both supported kernels. macOS uses proc_pid_rusage; Linux reads
/// /proc/self/io (kernel-attributed storage bytes), /proc/self/status (VmRSS, VmHWM) and,
/// when the process runs in a cgroup v2 with readable counters, memory.current. Missing
/// counters throw; nothing is reported as zero I/O by default.
/// </summary>
public static class NativeIo
{
    public static MacFileIo.Usage Sample()
    {
        if (OperatingSystem.IsMacOS()) return MacFileIo.Sample();
        if (OperatingSystem.IsLinux()) return Parse(File.ReadAllText("/proc/self/status"), File.ReadAllText("/proc/self/io"), CgroupValue("memory.current"));
        throw new PlatformNotSupportedException("No native resource instrument for this kernel");
    }
    /// <summary>Linux only: cgroup v2 memory.max/memory.peak and block-layer read bytes for the container, or null when not in a readable cgroup.</summary>
    public static object? Cgroup()
    {
        if (!OperatingSystem.IsLinux() || !Directory.Exists("/sys/fs/cgroup")) return null;
        string? Read(string name) { string p = Path.Combine("/sys/fs/cgroup", name); return File.Exists(p) ? File.ReadAllText(p).Trim() : null; }
        return new { MemoryMax = Read("memory.max"), MemoryPeak = Read("memory.peak"), MemoryCurrent = Read("memory.current"), IoStat = Read("io.stat"), MemoryEvents = Read("memory.events") };
    }
    private static ulong? CgroupValue(string name)
    {
        string p = Path.Combine("/sys/fs/cgroup", name);
        return File.Exists(p) && ulong.TryParse(File.ReadAllText(p).Trim(), out ulong v) ? v : null;
    }
    /// <summary>Pure parser so the Linux path is testable on any kernel.</summary>
    public static MacFileIo.Usage Parse(string status, string io, ulong? cgroupCurrent)
    {
        ulong Field(string text, string key, ulong multiplier)
        {
            foreach (string line in text.Split('\n'))
                if (line.StartsWith(key + ":", StringComparison.Ordinal))
                {
                    string value = line[(key.Length + 1)..].Trim(); int space = value.IndexOf(' ');
                    if (space > 0) value = value[..space];
                    return checked(ulong.Parse(value) * multiplier);
                }
            throw new InvalidDataException("Missing kernel counter " + key);
        }
        ulong rss = Field(status, "VmRSS", 1024), peak = Field(status, "VmHWM", 1024);
        return new(Field(io, "read_bytes", 1), Field(io, "write_bytes", 1), rss, cgroupCurrent ?? Math.Max(rss, peak));
    }
}
