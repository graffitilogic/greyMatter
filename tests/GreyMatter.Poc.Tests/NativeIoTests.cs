using GreyMatter.Poc.Storage;
using Xunit;

namespace GreyMatter.Poc.Tests;
public sealed class NativeIoTests
{
    [Fact]
    public void LinuxCountersParseAndMissingFieldsFailExplicitly()
    {
        string status = "Name:\tgm\nVmHWM:\t   70000 kB\nVmRSS:\t   65000 kB\n"; string io = "rchar: 5\nwchar: 6\nread_bytes: 4096\nwrite_bytes: 512\n";
        var u = NativeIo.Parse(status, io, null);
        Assert.Equal(4096ul, u.DiskReadBytes); Assert.Equal(512ul, u.DiskWrittenBytes); Assert.Equal(65000ul * 1024, u.ResidentBytes); Assert.Equal(70000ul * 1024, u.PhysicalFootprintBytes);
        Assert.Equal(123ul, NativeIo.Parse(status, io, 123).PhysicalFootprintBytes);
        Assert.Throws<InvalidDataException>(() => NativeIo.Parse("VmRSS:\t1 kB\n", io, null));
        Assert.Throws<InvalidDataException>(() => NativeIo.Parse(status, "rchar: 5\n", null));
    }
    [Fact]
    public void HostSamplerWorksOnThisKernel()
    {
        var u = NativeIo.Sample(); Assert.True(u.ResidentBytes > 0);
    }
}
