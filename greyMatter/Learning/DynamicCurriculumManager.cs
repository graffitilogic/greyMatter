using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Learning
{
    /// <summary>
    /// Dynamic Curriculum Manager: Combines LLM teacher guidance with available training data
    /// Downloads and manages curriculum data from NAS based on LLM recommendations
    /// Implements progressive learning phases with teacher oversight
    /// </summary>
    public class DynamicCurriculumManager
    {
        private readonly LLMTeacher _teacher;
        private readonly string _nasDataPath;
        private readonly string _localDataPath;
        private readonly Dictionary<string, DataSource> _availableDataSources;

        public DynamicCurriculumManager(string nasDataPath = "/Volumes/jarvis/trainData", 
                                      string localDataPath = "/tmp/cerebro_curriculum")
        {
            _teacher = new LLMTeacher();
            _nasDataPath = nasDataPath;
            _localDataPath = localDataPath;
            _availableDataSources = InitializeDataSources();
            
            Directory.CreateDirectory(_localDataPath);
        }

        public async Task<LearningPlan> CreatePersonalizedLearningPlan(LearningProfile profile)
        {
            Console.WriteLine("🧠 Consulting LLM teacher for personalized curriculum...");
            
            var goals = new LearningGoals
            {
                TargetVocabularySize = profile.TargetVocabularySize,
                FocusAreas = profile.Interests,
                CurrentLevel = DetermineCurrentLevel(profile),
                AvailableDataSources = _availableDataSources.Keys.ToList()
            };

            var guidance = await _teacher.SuggestCurriculum(goals);
            
            var learningPlan = new LearningPlan
            {
                StudentProfile = profile,
                TeacherGuidance = guidance,
                Phases = await PreparePhases(guidance),
                CreatedAt = DateTime.Now
            };

            Console.WriteLine($"📚 Created {learningPlan.Phases.Count} learning phases");
            foreach (var phase in learningPlan.Phases)
            {
                Console.WriteLine($"   Phase: {phase.Name} ({phase.EstimatedDuration})");
                Console.WriteLine($"   Data Sources: {string.Join(", ", phase.DataSources)}");
                Console.WriteLine($"   Target: {phase.TargetWords} words");
                Console.WriteLine();
            }

            return learningPlan;
        }

        public async Task<CurriculumLearningSession> StartLearningSession(LearningPlan plan, int phaseIndex = 0)
        {
            if (phaseIndex >= plan.Phases.Count)
                throw new ArgumentException("Phase index out of range");

            var phase = plan.Phases[phaseIndex];
            Console.WriteLine($"🚀 Starting learning phase: {phase.Name}");

            // Download required data sources to local cache
            var dataPaths = await EnsureDataAvailable(phase.DataSources);
            
            // Get teacher's initial guidance for this phase
            var context = new LearningContext
            {
                VocabularySize = plan.StudentProfile.CurrentVocabularySize,
                RecentWords = plan.StudentProfile.RecentWords,
                Sources = phase.DataSources,
                PerformanceMetrics = $"Phase {phaseIndex + 1} of {plan.Phases.Count}"
            };

            var teacherResponse = await _teacher.AnalyzeLearningState(context);

            var session = new CurriculumLearningSession
            {
                Plan = plan,
                CurrentPhase = phase,
                PhaseIndex = phaseIndex,
                LocalDataPaths = dataPaths,
                TeacherGuidance = teacherResponse,
                StartTime = DateTime.Now,
                Status = LearningSessionStatus.Active
            };

            Console.WriteLine($"📖 Teacher recommends focusing on: {teacherResponse.next_focus}");
            Console.WriteLine($"🎯 Learning priority: {teacherResponse.learning_priority}");
            Console.WriteLine($"💡 Suggested topics: {string.Join(", ", teacherResponse.suggested_topics)}");

            return session;
        }

        public async Task<ProgressEvaluation> EvaluateProgress(CurriculumLearningSession session, 
                                                              List<string> newWordsLearned,
                                                              Dictionary<string, double> performanceMetrics)
        {
            Console.WriteLine("📊 Evaluating learning progress with teacher...");

            var context = new LearningContext
            {
                VocabularySize = session.Plan.StudentProfile.CurrentVocabularySize + newWordsLearned.Count,
                RecentWords = newWordsLearned.TakeLast(20).ToList(),
                Sources = session.CurrentPhase.DataSources,
                PerformanceMetrics = FormatMetrics(performanceMetrics)
            };

            var teacherResponse = await _teacher.AnalyzeLearningState(context);
            
            var evaluation = new ProgressEvaluation
            {
                Session = session,
                NewWordsLearned = newWordsLearned,
                PerformanceMetrics = performanceMetrics,
                TeacherFeedback = teacherResponse,
                ShouldAdvancePhase = ShouldAdvanceToNextPhase(session, newWordsLearned.Count, teacherResponse),
                Timestamp = DateTime.Now
            };

            Console.WriteLine($"🎓 Teacher confidence: {teacherResponse.confidence:P1}");
            Console.WriteLine($"📈 Words learned this session: {newWordsLearned.Count}");
            Console.WriteLine($"🔄 Ready for next phase: {evaluation.ShouldAdvancePhase}");

            return evaluation;
        }

        public async Task<string> GetTeacherResponse(string userInput, BrainState brainState)
        {
            var response = await _teacher.HandleUserQuery(userInput, brainState);
            
            Console.WriteLine($"🤖 Teacher response (confidence: {response.confidence:P1}):");
            Console.WriteLine($"💬 {response.response_text}");
            
            if (response.learning_suggestions.Any())
            {
                Console.WriteLine("💡 Learning suggestions:");
                foreach (var suggestion in response.learning_suggestions)
                {
                    Console.WriteLine($"   - {suggestion}");
                }
            }

            return response.response_text;
        }

        private async Task<List<LearningPhase>> PreparePhases(CurriculumGuidance guidance)
        {
            var phases = new List<LearningPhase>();

            foreach (var phase in guidance.phases)
            {
                var learningPhase = new LearningPhase
                {
                    Name = phase.Name,
                    Description = phase.Description,
                    DataSources = phase.DataSources.Where(ds => _availableDataSources.ContainsKey(ds)).ToList(),
                    TargetWords = phase.TargetWords.Count,
                    EstimatedDuration = phase.Duration,
                    Prerequisites = phase.Prerequisites
                };

                phases.Add(learningPhase);
            }

            return phases;
        }

        private async Task<Dictionary<string, string>> EnsureDataAvailable(List<string> dataSources)
        {
            var localPaths = new Dictionary<string, string>();

            foreach (var source in dataSources)
            {
                if (_availableDataSources.TryGetValue(source, out var dataSource))
                {
                    var localPath = Path.Combine(_localDataPath, source);
                    
                    if (!File.Exists(localPath) || IsStale(localPath))
                    {
                        Console.WriteLine($"📥 Downloading {source} from NAS...");
                        await CopyFromNAS(dataSource.NasPath, localPath);
                    }
                    
                    localPaths[source] = localPath;
                }
            }

            return localPaths;
        }

        private Dictionary<string, DataSource> InitializeDataSources()
        {
            return new Dictionary<string, DataSource>
            {
                ["tatoeba_sentences"] = new DataSource
                {
                    Name = "Tatoeba Sentences",
                    NasPath = Path.Combine(_nasDataPath, "tatoeba", "sentences.csv"),
                    Description = "Multi-language sentence pairs",
                    EstimatedSize = "717MB"
                },
                ["simple_wikipedia"] = new DataSource
                {
                    Name = "Simple English Wikipedia",
                    NasPath = Path.Combine(_nasDataPath, "wikipedia", "simple_wiki.txt"),
                    Description = "Simplified Wikipedia articles",
                    EstimatedSize = "1.5GB"
                },
                ["childrens_books"] = new DataSource
                {
                    Name = "Children's Book Test",
                    NasPath = Path.Combine(_nasDataPath, "cbt", "cbt_data.txt"),
                    Description = "Children's stories for comprehension",
                    EstimatedSize = "800MB"
                },
                ["common_crawl"] = new DataSource
                {
                    Name = "Common Crawl Sample",
                    NasPath = Path.Combine(_nasDataPath, "common_crawl", "sample.txt"),
                    Description = "Web text sample",
                    EstimatedSize = "2GB"
                },
                ["vocabulary_lists"] = new DataSource
                {
                    Name = "Frequency Vocabulary Lists",
                    NasPath = Path.Combine(_nasDataPath, "vocab", "frequency_lists.json"),
                    Description = "Word frequency lists",
                    EstimatedSize = "50MB"
                }
            };
        }

        private async Task CopyFromNAS(string nasPath, string localPath)
        {
            if (File.Exists(nasPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localPath));
                await Task.Run(() => File.Copy(nasPath, localPath, overwrite: true));
            }
            else
            {
                Console.WriteLine($"⚠️  Warning: {nasPath} not found on NAS");
            }
        }

        private bool IsStale(string localPath)
        {
            var fileInfo = new FileInfo(localPath);
            return DateTime.Now - fileInfo.LastWriteTime > TimeSpan.FromDays(7);
        }

        private string DetermineCurrentLevel(LearningProfile profile)
        {
            return profile.CurrentVocabularySize switch
            {
                < 1000 => "beginner",
                < 5000 => "intermediate",
                < 20000 => "advanced",
                _ => "expert"
            };
        }

        private string FormatMetrics(Dictionary<string, double> metrics)
        {
            return string.Join(", ", metrics.Select(kvp => $"{kvp.Key}: {kvp.Value:F2}"));
        }

        private bool ShouldAdvanceToNextPhase(CurriculumLearningSession session, int wordsLearned, TeacherResponse feedback)
        {
            return wordsLearned >= session.CurrentPhase.TargetWords * 0.8 && 
                   feedback.confidence > 0.7;
        }

        public void Dispose()
        {
            _teacher?.Dispose();
        }
    }

    // Data Models
    public class LearningProfile
    {
        public string Name { get; set; } = "";
        public int CurrentVocabularySize { get; set; }
        public int TargetVocabularySize { get; set; }
        public List<string> Interests { get; set; } = new();
        public List<string> RecentWords { get; set; } = new();
        public string PreferredLearningStyle { get; set; } = "";
    }

    public class LearningPlan
    {
        public LearningProfile StudentProfile { get; set; } = new();
        public CurriculumGuidance TeacherGuidance { get; set; } = new();
        public List<LearningPhase> Phases { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class LearningPhase
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> DataSources { get; set; } = new();
        public int TargetWords { get; set; }
        public string EstimatedDuration { get; set; } = "";
        public List<string> Prerequisites { get; set; } = new();
    }

    public class CurriculumLearningSession
    {
        public LearningPlan Plan { get; set; } = new();
        public LearningPhase CurrentPhase { get; set; } = new();
        public int PhaseIndex { get; set; }
        public Dictionary<string, string> LocalDataPaths { get; set; } = new();
        public TeacherResponse TeacherGuidance { get; set; } = new();
        public DateTime StartTime { get; set; }
        public LearningSessionStatus Status { get; set; }
    }

    public class ProgressEvaluation
    {
        public CurriculumLearningSession Session { get; set; } = new();
        public List<string> NewWordsLearned { get; set; } = new();
        public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
        public TeacherResponse TeacherFeedback { get; set; } = new();
        public bool ShouldAdvancePhase { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DataSource
    {
        public string Name { get; set; } = "";
        public string NasPath { get; set; } = "";
        public string Description { get; set; } = "";
        public string EstimatedSize { get; set; } = "";
    }

    public enum LearningSessionStatus
    {
        Active,
        Paused,
        Completed,
        Failed
    }
}
