using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using MessagePack;

namespace SimpleBenchmark
{
    [MessagePackObject]
    public class TestData
    {
        [Key(0)]
        public string Name { get; set; } = string.Empty;

        [Key(1)]
        public int Age { get; set; }

        [Key(2)]
        public List<string> Tags { get; set; } = new();

        [Key(3)]
        public Dictionary<string, double> Scores { get; set; } = new();

        [Key(4)]
        public DateTime Created { get; set; }
    }

    class Program
    {
        // static void Main(string[] args)
        static void Main(string[] args)
        {
            Console.WriteLine("=== MessagePack Performance Benchmark ===\n");

            // Generate test data
            var testData = GenerateTestData(1000);

            // Run benchmarks
            Console.WriteLine("Running benchmarks with 1000 objects...\n");

            var jsonResults = TestJson(testData);
            var msgpackResults = TestMessagePack(testData);

            // Display results
            Console.WriteLine("=== RESULTS ===");
            Console.WriteLine($"JSON Serialization:   {jsonResults.serializationTime:F2} ms");
            Console.WriteLine($"JSON Deserialization: {jsonResults.deserializationTime:F2} ms");
            Console.WriteLine($"JSON Size:            {jsonResults.size} bytes");
            Console.WriteLine();
            Console.WriteLine($"MessagePack Serialization:   {msgpackResults.serializationTime:F2} ms");
            Console.WriteLine($"MessagePack Deserialization: {msgpackResults.deserializationTime:F2} ms");
            Console.WriteLine($"MessagePack Size:            {msgpackResults.size} bytes");
            Console.WriteLine();

            // Calculate improvements
            var speedImprovement = jsonResults.serializationTime / msgpackResults.serializationTime;
            var sizeReduction = ((double)(jsonResults.size - msgpackResults.size) / jsonResults.size) * 100;

            Console.WriteLine("=== PERFORMANCE COMPARISON ===");
            Console.WriteLine($"Serialization Speed: {speedImprovement:F2}x faster");
            Console.WriteLine($"Size Reduction:      {sizeReduction:F1}% smaller");
            Console.WriteLine();

            // Verify data integrity
            Console.WriteLine("=== DATA INTEGRITY CHECK ===");
            var original = testData[0];
            var jsonRoundTrip = JsonSerializer.Deserialize<TestData>(
                JsonSerializer.Serialize(original));
            var msgpackRoundTrip = MessagePackSerializer.Deserialize<TestData>(
                MessagePackSerializer.Serialize(original));

            Console.WriteLine($"JSON Round-trip:     {(original.Name == jsonRoundTrip?.Name ? "PASS" : "FAIL")}");
            Console.WriteLine($"MessagePack Round-trip: {(original.Name == msgpackRoundTrip?.Name ? "PASS" : "FAIL")}");
        }

        static List<TestData> GenerateTestData(int count)
        {
            var data = new List<TestData>();
            var random = new Random(42); // Fixed seed for reproducible results

            for (int i = 0; i < count; i++)
            {
                data.Add(new TestData
                {
                    Name = $"TestObject_{i}",
                    Age = random.Next(18, 80),
                    Tags = new List<string> { "tag1", "tag2", "tag3" },
                    Scores = new Dictionary<string, double>
                    {
                        ["math"] = random.NextDouble() * 100,
                        ["science"] = random.NextDouble() * 100,
                        ["english"] = random.NextDouble() * 100
                    },
                    Created = DateTime.Now.AddDays(-random.Next(365))
                });
            }

            return data;
        }

        static (double serializationTime, double deserializationTime, int size) TestJson(List<TestData> data)
        {
            var stopwatch = new Stopwatch();

            // Serialization test
            stopwatch.Start();
            var jsonStrings = new List<string>();
            foreach (var item in data)
            {
                jsonStrings.Add(JsonSerializer.Serialize(item));
            }
            stopwatch.Stop();
            var serializationTime = stopwatch.Elapsed.TotalMilliseconds;

            // Size calculation
            var totalSize = jsonStrings.Sum(s => Encoding.UTF8.GetBytes(s).Length);

            // Deserialization test
            stopwatch.Restart();
            foreach (var json in jsonStrings)
            {
                JsonSerializer.Deserialize<TestData>(json);
            }
            stopwatch.Stop();
            var deserializationTime = stopwatch.Elapsed.TotalMilliseconds;

            return (serializationTime, deserializationTime, totalSize);
        }

        static (double serializationTime, double deserializationTime, int size) TestMessagePack(List<TestData> data)
        {
            var stopwatch = new Stopwatch();

            // Serialization test
            stopwatch.Start();
            var msgpackData = new List<byte[]>();
            foreach (var item in data)
            {
                msgpackData.Add(MessagePackSerializer.Serialize(item));
            }
            stopwatch.Stop();
            var serializationTime = stopwatch.Elapsed.TotalMilliseconds;

            // Size calculation
            var totalSize = msgpackData.Sum(bytes => bytes.Length);

            // Deserialization test
            stopwatch.Restart();
            foreach (var bytes in msgpackData)
            {
                MessagePackSerializer.Deserialize<TestData>(bytes);
            }
            stopwatch.Stop();
            var deserializationTime = stopwatch.Elapsed.TotalMilliseconds;

            return (serializationTime, deserializationTime, totalSize);
        }
    }
}
