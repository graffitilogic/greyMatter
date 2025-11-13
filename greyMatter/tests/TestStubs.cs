// Stub namespace for deleted test components - allows build to pass
namespace GreyMatter.Tests
{
    using System.Threading.Tasks;
    
    public class RealStoragePerformanceTest
    {
        public Task RunAsync()
        {
            System.Console.WriteLine("RealStoragePerformanceTest deleted - used old storage architecture");
            return Task.CompletedTask;
        }
    }
}
