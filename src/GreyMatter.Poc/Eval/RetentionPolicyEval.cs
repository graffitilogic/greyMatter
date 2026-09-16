using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Eval;

/// <summary>Bounded source-local learning experiment; resident storage, no R4 claims.</summary>
public static class RetentionPolicyEval
{
    public sealed record Query(int Chain, int Hops, int Correct, double[] Scores, double Top1);
    public sealed record Replacement(int Chain, double OldBefore, double OldAfter, double NewAfter, double NewTop1, double[] Scores);
    public sealed record Branch(int Chain, double OldCoverage, double NewCoverage);
    public sealed record Arm(string Policy, int Seed, int Chains, string CorpusHash, long Updates, long Pruned,
        Query[] Before, Query[] Anchors, Replacement[] Replacements, Branch[] Branches,
        long Records, long Edges, long PayloadBytes, string ModelHash, double Seconds);
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static int Run(Args args)
    {
        string output = Path.GetFullPath(args.Value("--output", null) ?? throw new ArgumentException("--output required"));
        if (File.Exists(output)) throw new IOException("Refusing overwrite");
        int seed = int.Parse(args.Value("--seed", "100")!), chains = seed == 100 ? 32 : 1024;
        if (seed != 100 && (seed < 301 || seed > 305)) throw new ArgumentException("Registered seeds100,301–305 only");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var arms = new List<Arm>();
        foreach (string policy in new[] { "global", "none", "local" })
        {
            Arm arm = Measure(seed, chains, policy, Path.ChangeExtension(output, policy + ".records"));
            arms.Add(arm);
            File.WriteAllText(Path.ChangeExtension(output, policy + ".json"), JsonSerializer.Serialize(arm, Json));
            Console.WriteLine($"{policy}: direct={Mean(arm.Before, 1):F4} composed={Mean(arm.Before, 2):F4} obsolete={arm.Replacements.Average(x => x.OldAfter):F4}");
        }
        var local = arms.Single(x => x.Policy == "local"); var none = arms.Single(x => x.Policy == "none");
        bool hashes = arms.Select(x => x.CorpusHash).Distinct().Count() == 1 && arms.Select(x => x.Updates).Distinct().Count() == 1;
        bool gate = hashes && Mean(local.Before, 1) >= .7 && Mean(local.Before, 2) >= .7 &&
            Mean(local.Before, 1) >= Mean(none.Before, 1) - .02 && Mean(local.Before, 2) >= Mean(none.Before, 2) - .02 &&
            Mean(local.Anchors, 1) >= .7 && Mean(local.Anchors, 2) >= .7 &&
            local.Replacements.Average(x => x.NewTop1) >= .7 && local.Replacements.Average(x => x.OldAfter) <= .1 &&
            local.Branches.Average(x => x.OldCoverage) >= .9 && local.Branches.Average(x => x.NewCoverage) >= .9;
        File.WriteAllText(output, JsonSerializer.Serialize(new { Seed = seed, Chains = chains, PairedInputs = hashes,
            PerSeedGate = gate, Arms = arms, Scope = "Resident intermediate-scale learning experiment; aggregate gate requires all five fresh seeds" }, Json));
        Console.WriteLine($"PER_SEED_GATE: {gate}"); return gate ? 0 : 1;
    }
    private static double Mean(Query[] rows, int mode) => rows.Where(x => mode == 1 ? x.Hops == 1 : x.Hops > 1).Average(x => x.Top1);

    private static Arm Measure(int seed, int chains, string policy, string export)
    {
        if (File.Exists(export)) throw new IOException("Refusing model overwrite");
        var clock = System.Diagnostics.Stopwatch.StartNew();
        using var records = new ResidentRelayRecords(CapacityEval.Space);
        var learner = new StoredRelayLearning(records) { DisableDecayForDiagnostic = policy == "none", SourceLocalForgetting = policy == "local" };
        var input = new CapacityEval.Input(seed);
        using var corpus = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] pair = new byte[8];
        void Teach(int from, int to)
        {
            BinaryPrimitives.WriteInt32LittleEndian(pair, from); BinaryPrimitives.WriteInt32LittleEndian(pair.AsSpan(4), to); corpus.AppendData(pair);
            learner.ObserveMembers(input.Members(from)); learner.ObserveMembers(input.Members(to)); learner.EndSequence();
        }
        int episodes = chains * 4 * 16, mask = episodes - 1;
        uint odd = (uint)Rng.Mix((uint)seed) | 1u, offset = (uint)Rng.Mix((uint)seed ^ 0xb519u);
        for (int i = 0; i < episodes; i++)
        {
            int relation = (int)(unchecked((uint)i * odd + offset) & (uint)mask) % (chains * 4);
            int concept = relation / 4 * 5 + relation % 4; Teach(concept, concept + 1);
        }
        var runtime = new StoredRelayRecall(records, 256, 4, 4 * 1024 * 1024);
        int count = Math.Min(64, chains), quarter = count / 4;
        int Chain(int q) => (seed + 17 * q) % chains;
        double Score(int concept) { double score = 0; foreach (uint id in input.Members(concept)) score += runtime.Value(id); return score; }
        Query[] Probe(int start, int end)
        {
            var rows = new List<Query>();
            for (int q = start; q < end; q++) foreach (int hops in new[] { 1, 2 + q % 3 })
            {
                int chain = Chain(q), correct = q % 32; runtime.Run(input.Members(chain * 5), hops);
                double[] scores = Enumerable.Range(0, 32).Select(c => Score(((chain + (c - correct + 32) % 32) % chains) * 5 + hops)).ToArray();
                rows.Add(new(chain, hops, correct, scores, RecoveryLearning.ScoreRank(scores, correct).Top1));
            }
            return rows.ToArray();
        }
        byte[] buffer = new byte[RelayRecord.Bytes];
        double Coverage(int from, int to)
        {
            uint[] sources = input.Members(from).ToArray(), targets = input.Members(to).ToArray();
            int found = 0, offered = 0;
            foreach (uint source in sources)
            {
                bool present = records.Read(source, buffer); int degree = present ? BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4)) : 0;
                foreach (uint target in targets)
                {
                    if (source == target) continue; offered++;
                    for (int e = 0; e < degree; e++) if (BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(8 + e * 9)) == target) { found++; break; }
                }
            }
            return offered == 0 ? 0 : (double)found / offered;
        }
        Query[] before = Probe(0, count);
        double[] oldBefore = Enumerable.Range(0, quarter).Select(q => Coverage(Chain(q) * 5, Chain(q) * 5 + 1)).ToArray();
        for (int repeat = 0; repeat < 64; repeat++)
        {
            for (int q = 0; q < quarter; q++) Teach(Chain(q) * 5, chains * 5 + q);
            if (repeat < 16) for (int q = quarter; q < 2 * quarter; q++)
            { Teach(Chain(q) * 5, chains * 5 + q); Teach(Chain(q) * 5, Chain(q) * 5 + 1); }
        }
        Query[] anchors = Probe(2 * quarter, count);
        var replacements = new List<Replacement>();
        for (int q = 0; q < quarter; q++)
        {
            int chain = Chain(q), correct = q % 32; runtime.Run(input.Members(chain * 5), 1);
            double[] scores = Enumerable.Range(0, 32).Select(c =>
            {
                int delta = (c - correct + 32) % 32;
                return Score(delta == 0 ? chains * 5 + q : ((chain + delta - 1) % chains) * 5 + 1);
            }).ToArray();
            replacements.Add(new(chain, oldBefore[q], Coverage(chain * 5, chain * 5 + 1), Coverage(chain * 5, chains * 5 + q),
                RecoveryLearning.ScoreRank(scores, correct).Top1, scores));
        }
        Branch[] branches = Enumerable.Range(quarter, quarter).Select(q => new Branch(Chain(q),
            Coverage(Chain(q) * 5, Chain(q) * 5 + 1), Coverage(Chain(q) * 5, chains * 5 + q))).ToArray();
        // Numeric evaluation export, concatenated self-checksummed records. Not a resumable checkpoint.
        long recordCount = 0, edgeCount = 0; using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using (var file = new FileStream(export, FileMode.CreateNew, FileAccess.Write)) records.VisitPresent(id =>
        {
            records.Read(id, buffer); file.Write(buffer); hash.AppendData(buffer); recordCount++;
            edgeCount += BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4));
        });
        return new(policy, seed, chains, Convert.ToHexString(corpus.GetHashAndReset()), learner.Updates, learner.LocallyPruned,
            before, anchors, replacements.ToArray(), branches, recordCount, edgeCount, recordCount * RelayRecord.Bytes,
            Convert.ToHexString(hash.GetHashAndReset()), clock.Elapsed.TotalSeconds);
    }
}
