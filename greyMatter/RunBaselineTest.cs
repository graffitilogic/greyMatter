using System;
using System.Threading.Tasks;
using greyMatter.Validation;

namespace greyMatter
{
    /// <summary>
    /// Simple runner to execute BaselineValidationTest directly
    /// Usage: dotnet run --project greyMatter.csproj -- --baseline-test
    /// </summary>
    class RunBaselineTest
    {
        static async Task RunTest()
        {
            await BaselineValidationTest.Main(new string[0]);
        }
    }
}
