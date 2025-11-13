// Stub namespace for deleted learner components - allows build to pass
namespace GreyMatter.Learning
{
    using System.Threading.Tasks;
    
    public class EnhancedLanguageLearner
    {
        public Task ProcessSentenceAsync(string sentence)
        {
            System.Console.WriteLine("EnhancedLanguageLearner deleted - replace with Cerebro");
            return Task.CompletedTask;
        }
    }

    public class RealLanguageLearner
    {
        public Task ProcessSentenceAsync(string sentence)
        {
            System.Console.WriteLine("RealLanguageLearner deleted - replace with Cerebro");
            return Task.CompletedTask;
        }
    }

    public class ContinuousLearner
    {
        public Task ProcessSentenceAsync(string sentence)
        {
            System.Console.WriteLine("ContinuousLearner deleted - replace with Cerebro");
            return Task.CompletedTask;
        }
    }
}
