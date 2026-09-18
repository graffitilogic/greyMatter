using System.Text.Json;
using GreyMatter.Poc.Eval;
using GreyMatter.Poc.Utility;
using Xunit;
namespace GreyMatter.Poc.Tests;
public class CostProfileTests
{
    [Fact] public void NestedScopesPartitionTimeAndAllocation()
    {
        using var profile=new CostProfile();
        using(CostProfile.Enter(CostProfile.Kind.Learning))
        {
            using(CostProfile.Enter(CostProfile.Kind.Serialization)){GC.KeepAlive(new byte[4096]);}
            GC.KeepAlive(new byte[2048]);
        }
        var report=JsonSerializer.SerializeToElement(profile.Finish());var rows=report.GetProperty("Rows").EnumerateArray().ToArray();
        Assert.Equal(1,rows[2].GetProperty("Calls").GetInt64());Assert.Equal(1,rows[4].GetProperty("Calls").GetInt64());
        long total=rows.Sum(r=>r.GetProperty("ExclusiveAllocatedBytes").GetInt64())+report.GetProperty("UnattributedAllocatedBytes").GetInt64();
        Assert.Equal(report.GetProperty("ThreadAllocatedBytes").GetInt64(),total);
        Assert.All(rows,r=>Assert.True(r.GetProperty("ExclusiveSeconds").GetDouble()>=0));
        Assert.True(report.GetProperty("UnattributedSeconds").GetDouble()>=0);
    }
    [Fact] public void InstrumentedLearningPublishesIdenticalModel()
    {
        string root=Path.Combine(Path.GetTempPath(),"gm-profile-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            string source=Path.Combine(root,"source");File.WriteAllText(source,"amber birch\nbirch cedar\namber birch\n");
            LocalModel.Train(source,"text",Path.Combine(root,"plain"),201,128);
            using(var profile=new CostProfile()){LocalModel.Train(source,"text",Path.Combine(root,"profile"),201,128);_=profile.Finish();}
            Assert.Equal(PolicyIntegrationEval.PhysicalHash(Path.Combine(root,"plain")),PolicyIntegrationEval.PhysicalHash(Path.Combine(root,"profile")));
        }
        finally{Directory.Delete(root,true);}
    }
}
