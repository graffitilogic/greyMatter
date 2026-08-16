using System.Diagnostics;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Engrams;
using GreyMatter.Poc.Runtime;

namespace GreyMatter.Poc.Pipeline;

/// <summary>
/// plan.md §4.6 — the streaming learn loop. P5 hardens this with checkpoint/resume;
/// P4 needs it to exist so <c>gm learn</c> and the recall gate are real.
/// </summary>
public sealed class Trainer
{
    private readonly Config _cfg;
    private readonly ActivationScope _scope;
    private readonly Cascade _cascade;
    private readonly Plasticity _plasticity;
    private readonly ContextEncoder _encoder;

    /// <summary>
    /// Words excluded from training. This is how the P4 control set is held out:
    /// a control word still ENCODES normally and still gets a valid regenerable
    /// assembly, it simply never learns. Skipping it entirely — rather than
    /// removing its sentences — keeps the rest of the corpus intact, so trained
    /// and control words differ by exactly one factor (rule 8).
    /// </summary>
    public HashSet<string> HeldOut { get; init; } = new();

    /// <summary>Sentences between checkpoints. 0 disables mid-run checkpointing.</summary>
    public int CheckpointEvery { get; init; }

    /// <summary>Invoked with the number of sentences consumed in THIS run.</summary>
    public Action<long>? OnCheckpoint { get; init; }

    public sealed record Stats(int Sentences, int Tokens, int Skipped, double Seconds,
                               long WithinCueUpdates, long SequenceUpdates,
                               long Truncations, long Consolidations, long DeviationsWritten,
                               int WorkingSetHighWater, long Synapses);

    public Trainer(Config cfg, ActivationScope scope, ContextEncoder encoder)
    {
        _cfg = cfg;
        _scope = scope;
        _encoder = encoder;
        _cascade = new Cascade(cfg, scope);
        _plasticity = new Plasticity(cfg, scope);
    }

    public Cascade Cascade => _cascade;
    public Plasticity Plasticity => _plasticity;

    public Stats Run(IEnumerable<string> sentences, bool quiet = false, int decayEvery = 500)
    {
        var sw = Stopwatch.StartNew();
        int sentenceCount = 0, tokenCount = 0, skipped = 0;

        foreach (var sentence in sentences)
        {
            var tokens = Corpus.Tokenize(sentence);
            if (tokens.Count == 0) continue;

            foreach (var token in tokens)
            {
                if (HeldOut.Contains(token)) { skipped++; continue; }

                var code = _encoder.Encode(token);
                var readout = _cascade.Run(code, learningMode: true);
                if (readout.WinnerCount > 0)
                    _plasticity.Learn(_cascade.Winners(readout.WinnerCount),
                                      _cascade.WinnerScores(readout.WinnerCount));
                tokenCount++;
            }

            // Sentence boundary: the trace must not span it, or the last word of
            // one sentence wires to the first of the next.
            _plasticity.EndSequence();
            sentenceCount++;

            if (decayEvery > 0 && sentenceCount % decayEvery == 0) _plasticity.Decay();

            if (CheckpointEvery > 0 && sentenceCount % CheckpointEvery == 0 && OnCheckpoint is not null)
            {
                // Consolidate before writing, or the checkpoint records recipes that
                // are missing everything still sitting in the working set.
                _scope.ConsolidateAll();
                OnCheckpoint(sentenceCount);
                if (!quiet) Console.WriteLine($"   ✔ checkpoint at {sentenceCount:N0} sentences");
            }

            if (!quiet && sentenceCount % 1000 == 0)
            {
                var elapsed = sw.Elapsed.TotalSeconds;
                var rate = sentenceCount / Math.Max(1e-9, elapsed);
                Console.WriteLine($"   {sentenceCount:N0} sentences   {tokenCount:N0} tokens   " +
                                  $"resident {_scope.Pool.Count:N0}   synapses {_scope.Synapses.TotalSynapses:N0}   " +
                                  $"{rate:F0} sent/s   elapsed {elapsed / 60:F1}m");
            }
        }

        _scope.ConsolidateAll();
        sw.Stop();

        return new Stats(sentenceCount, tokenCount, skipped, sw.Elapsed.TotalSeconds,
                         _plasticity.WithinCueUpdates, _plasticity.SequenceUpdates,
                         _cascade.Truncations, _scope.Consolidations, _scope.DeviationsWritten,
                         _scope.Pool.HighWaterMark, _scope.Synapses.TotalSynapses);
    }

    /// <summary>Accumulate distributional statistics before training (§4.2).</summary>
    public static void AccumulateContext(ContextEncoder encoder, IEnumerable<string> sentences,
                                         HashSet<string>? heldOut = null)
    {
        foreach (var sentence in sentences)
        {
            var tokens = Corpus.Tokenize(sentence);
            // Held-out words are removed from the context pass too. Leaving them in
            // would let the control set acquire distributional structure it was
            // never supposed to have, and the arms would differ by two factors.
            if (heldOut is { Count: > 0 }) tokens = tokens.Where(t => !heldOut.Contains(t)).ToList();
            encoder.Observe(tokens);
        }
    }

    /// <summary>Persist dirty recipes, partitioned by LSH bucket of their code.</summary>
    public static int Persist(Config cfg, ActivationScope scope, LshIndex lsh)
    {
        var store = new EngramStore(cfg.BrainDataPath);
        var byBucket = new Dictionary<uint, List<NeuronRecipe>>();
        var weights = new float[cfg.SurfaceDimensions];

        foreach (var recipe in scope.DirtyRecipes())
        {
            Regeneration.Regenerate(recipe, scope.Codebook, weights);
            uint bucket = lsh.PrimaryBucket(SparseCode.TopK(weights, cfg.Sparsity));
            if (!byBucket.TryGetValue(bucket, out var list)) byBucket[bucket] = list = new List<NeuronRecipe>();
            list.Add(recipe);
        }

        foreach (var (bucket, list) in byBucket.OrderBy(kv => kv.Key))
            store.Append(bucket, list);

        return byBucket.Count;
    }
}
