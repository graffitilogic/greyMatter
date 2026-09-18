using GreyMatter.Poc.Utility;
using GreyMatter.Poc.Storage;
using Xunit;
namespace GreyMatter.Poc.Tests;
public sealed class LocalModelTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"gm-text-"+Guid.NewGuid().ToString("N"));
    public LocalModelTests()=>Directory.CreateDirectory(root);
    public void Dispose()=>Directory.Delete(root,true);
    private string P(string p)=>Path.Combine(root,p);
    [Fact] public void RestartUsesSavedEncoderAndNoSourceAndMatchesResident()
    {
        File.WriteAllText(P("source"),"amber birch\nbirch cedar\ncedar dogwood\n");
        var d=LocalModel.Train(P("source"),"text",P("model"),201,128);Assert.Equal(3,d.Sentences);
        File.Delete(P("source")); var before=Eval.PolicyIntegrationEval.PhysicalHash(P("model"));
        var saved=LocalModel.Open(P("model"),128);using var disk=saved.Store;using var resident=new ResidentRelayRecords(LocalText.Space);
        var buffer=new byte[RelayRecord.Bytes];disk.VisitPresent(id=>{disk.Read(id,buffer);resident.Write(id,buffer);});disk.ClearCache();
        foreach(int hops in new[]{1,2,3})
        {
            string[] candidates={"birch","cedar","dogwood","quartz"};
            var a=LocalModel.Query(disk,saved.Description.Seed,"AMBER!",candidates,hops);var b=LocalModel.Query(resident,201,"amber",candidates,hops);
            Assert.Equal(a.Results,b.Results);Assert.True(a.Results[hops-1].Score>0);Assert.Equal(0,a.Results[3].Score);
        }
        Assert.Equal(before,Eval.PolicyIntegrationEval.PhysicalHash(P("model")));Assert.Equal(0,disk.BytesWritten);Assert.Equal(0,disk.IndexBytesWritten);
        _=LocalModel.Audit(P("model"),128);
    }
    [Fact] public void CorruptionExtraWordlistAndOverwriteAreRefused()
    {
        File.WriteAllText(P("source"),"amber birch\n");LocalModel.Train(P("source"),"text",P("model"),42,128);
        Assert.Throws<IOException>(()=>LocalModel.Train(P("source"),"text",P("model"),42,128));
        File.WriteAllText(P("model/words.txt"),"amber birch");Assert.Throws<InvalidDataException>(()=>LocalModel.Audit(P("model"),128));File.Delete(P("model/words.txt"));
        byte[] bytes=File.ReadAllBytes(P("model/text.bin"));bytes[8]^=1;File.WriteAllBytes(P("model/text.bin"),bytes);
        Assert.Throws<InvalidDataException>(()=>LocalModel.Open(P("model"),128));
    }
    [Fact] public void BoundariesDoNotCreateEdgesAndMissingCuesRemainZero()
    {
        File.WriteAllText(P("source"),"amber\nbirch\n");LocalModel.Train(P("source"),"text",P("model"),201,128);
        var saved=LocalModel.Open(P("model"),128);using var store=saved.Store;
        Assert.All(LocalModel.Query(store,201,"amber",new[]{"birch","quartz"},1).Results,h=>Assert.Equal(0,h.Score));
        Assert.All(LocalModel.Query(store,201,"unseen",new[]{"birch"},4).Results,h=>Assert.Equal(0,h.Score));
        Assert.Throws<ArgumentException>(()=>LocalModel.Query(store,201,"two words",new[]{"birch"},1));
    }
    [Fact] public void MalformedInputsFailWithoutPublishingAndCandidatesAreBounded()
    {
        File.WriteAllText(P("source"),new string('a',LocalText.MaxToken+1));
        Assert.Throws<InvalidDataException>(()=>LocalModel.Train(P("source"),"text",P("model"),201,128));Assert.False(Directory.Exists(P("model")));Assert.Empty(Directory.GetDirectories(root));
        File.WriteAllText(P("long"),new string('x',LocalText.MaxLine+1));Assert.Throws<InvalidDataException>(()=>LocalText.Lines(P("long")).ToArray());
        File.WriteAllLines(P("candidates"),Enumerable.Range(0,4097).Select(i=>"candidate"+i));Assert.Throws<ArgumentException>(()=>LocalText.Candidates(P("candidates")));
        Assert.Equal(new[]{"a","café","b","2"},LocalText.Tokens("Ａ café B-2"));
    }
}
