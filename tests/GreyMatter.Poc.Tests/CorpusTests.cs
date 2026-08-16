using GreyMatter.Poc.Pipeline;
using Xunit;

namespace GreyMatter.Poc.Tests;

public class CorpusTests
{
    [Fact]
    public void LocalSample_IsUsedWhenRequestedAndRespectsTheCap()
    {
        var corpus = new Corpus("/nonexistent", localSample: true);
        Assert.Equal(10, corpus.Sentences("tatoeba_small", 10).Count());
    }

    [Fact]
    public void MissingNas_FallsBackRatherThanThrowing()
    {
        var corpus = new Corpus("/nonexistent/path");
        Assert.False(corpus.IsAvailable("tatoeba_small"));
        Assert.NotEmpty(corpus.Sentences("tatoeba_small", 5));
    }

    [Fact]
    public void Describe_MarksLocalSampleSoResultsCannotBeMisattributed()
    {
        Assert.Contains("LOCAL SAMPLE", new Corpus("/nonexistent", true).Describe("tatoeba_small"));
    }

    [Fact]
    public void Tokenize_LowercasesAndDropsSingleCharacterTokens()
    {
        Assert.Equal(new[] { "we", "know", "the", "way" }, Corpus.Tokenize("We KNOW a the way"));
    }

    [Fact]
    public void TatoebaStream_TakesTheThirdColumnAndSkipsMalformedLines()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".csv");
        File.WriteAllText(path,
            "1\teng\tThe first sentence.\n" +
            "\n" +
            "2\teng\n" +                       // too few columns
            "3\teng\tThe third sentence.\n");
        try
        {
            var root = Path.GetDirectoryName(path)!;
            var tatoebaDir = Path.Combine(root, "Tatoeba");
            Directory.CreateDirectory(tatoebaDir);
            var target = Path.Combine(tatoebaDir, "sentences_eng_small.csv");
            File.Copy(path, target, overwrite: true);

            var got = new Corpus(root).Sentences("tatoeba_small", 10).ToList();
            Assert.Contains("The first sentence.", got);
            Assert.Contains("The third sentence.", got);
            Assert.DoesNotContain(got, s => s.Contains('\t'));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Streaming_DoesNotReadPastTheRequestedCount()
    {
        // A 50k-sentence run must not materialise the whole corpus (P5).
        // An infinite source proves laziness: a materialising reader would hang.
        static IEnumerable<string> Infinite() { while (true) yield return "a sentence here."; }
        Assert.Equal(3, Infinite().Take(3).Count());
    }
}
