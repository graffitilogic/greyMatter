using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace GreyMatter.Poc.Storage;

/// <summary>macOS-only measurement hooks; never changes workstation-wide cache policy.</summary>
public static class MacFileIo
{
    public readonly record struct Usage(ulong DiskReadBytes, ulong DiskWrittenBytes, ulong ResidentBytes, ulong PhysicalFootprintBytes);
    // Verified against local SDK: rusage_info_v2 sizeof160; offsets144/152/64/72.
    [DllImport("libproc", EntryPoint = "proc_pid_rusage", SetLastError = true)]
    private static extern int Rusage(int pid, int flavor, [Out] byte[] buffer);
    [DllImport("libc", EntryPoint = "fcntl", SetLastError = true)]
    private static extern int Fcntl(SafeFileHandle file, int command, int value);
    public static Usage Sample()
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException("macOS resource instrument");
        var buffer = new byte[160];
        if (Rusage(Environment.ProcessId, 2, buffer) != 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
        return new(BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(144)), BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(152)),
            BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(64)), BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(72)));
    }
    internal static void SetNoCache(SafeFileHandle file, bool enabled)
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException("F_NOCACHE is macOS-only");
        if (Fcntl(file, 48, enabled ? 1 : 0) != 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
    }
}
