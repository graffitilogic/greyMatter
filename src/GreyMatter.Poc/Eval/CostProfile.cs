using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
namespace GreyMatter.Poc.Eval;

/// <summary>Opt-in single-thread attribution. Fixed stack, exclusive nested time/allocation.</summary>
public sealed class CostProfile : IDisposable
{
    public enum Kind { Tokenization, Encoding, Learning, Traversal, Serialization, CacheIndex, FileApi, SourceHash, Publication }
    public sealed record Row(string Category,long Calls,double ExclusiveSeconds,long ExclusiveAllocatedBytes);
    [ThreadStatic] private static CostProfile? _active;
    private readonly long[] _start=new long[32],_alloc=new long[32],_child=new long[32],_childAlloc=new long[32];
    private readonly Kind[] _kind=new Kind[32];
    private readonly long[] _ticks=new long[9],_bytes=new long[9],_calls=new long[9];
    private int _depth; private readonly long _rootStart,_rootAlloc; private readonly int _thread;
    public CostProfile()
    {
        if(_active!=null)throw new InvalidOperationException("Profile already active");
        _thread=Environment.CurrentManagedThreadId;_rootStart=Stopwatch.GetTimestamp();_rootAlloc=GC.GetAllocatedBytesForCurrentThread();_active=this;
    }
    public readonly struct Scope(CostProfile? owner):IDisposable { public void Dispose()=>owner?.Exit(); }
    public static Scope Enter(Kind kind)
    {
        var p=_active;if(p==null)return default;
        if(p._depth==32)throw new InvalidOperationException("Profile stack overflow");int n=p._depth++;
        p._kind[n]=kind;p._child[n]=p._childAlloc[n]=0;p._start[n]=Stopwatch.GetTimestamp();p._alloc[n]=GC.GetAllocatedBytesForCurrentThread();return new(p);
    }
    private void Exit()
    {
        int n=--_depth;long ticks=Stopwatch.GetTimestamp()-_start[n],bytes=GC.GetAllocatedBytesForCurrentThread()-_alloc[n];int k=(int)_kind[n];
        _ticks[k]+=ticks-_child[n];_bytes[k]+=bytes-_childAlloc[n];_calls[k]++;
        if(n>0){_child[n-1]+=ticks;_childAlloc[n-1]+=bytes;}
    }
    public object Finish()
    {
        if(_depth!=0)throw new InvalidOperationException("Unclosed profile scope");
        long total=Stopwatch.GetTimestamp()-_rootStart,allocated=GC.GetAllocatedBytesForCurrentThread()-_rootAlloc;
        return new { WallSeconds=(double)total/Stopwatch.Frequency,ThreadAllocatedBytes=allocated,
            UnattributedSeconds=(double)(total-_ticks.Sum())/Stopwatch.Frequency,UnattributedAllocatedBytes=allocated-_bytes.Sum(),
            Rows=Enum.GetValues<Kind>().Select(k=>new Row(k.ToString(),_calls[(int)k],(double)_ticks[(int)k]/Stopwatch.Frequency,_bytes[(int)k])).ToArray() };
    }
    public void Dispose(){if(_thread!=Environment.CurrentManagedThreadId||_depth!=0)throw new InvalidOperationException("Profile thread/scope mismatch");_active=null;}
    public static void Write(SafeFileHandle handle,ReadOnlySpan<byte> bytes,long offset)
    {using var measure=Enter(Kind.FileApi);RandomAccess.Write(handle,bytes,offset);}
    public static void Flush(SafeFileHandle handle)
    {using var measure=Enter(Kind.FileApi);RandomAccess.FlushToDisk(handle);}
}
