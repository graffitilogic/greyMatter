using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace GreyMatter.Core
{
    /// <summary>
    /// LearningResourceManager: Manages a 37TB digital library for autonomous learning
    /// Provides structured access to knowledge resources based on developmental readiness
    /// </summary>
    public class LearningResourceManager
    {
        private readonly string _libraryPath;
        private readonly Dictionary<ResourceType, List<LearningResource>> _resourceCatalog;
        private readonly Dictionary<DevelopmentalStage, ResourceAccessPolicy> _accessPolicies;
        
        public LearningResourceManager(string libraryPath)
        {
            _libraryPath = libraryPath;
            _resourceCatalog = new Dictionary<ResourceType, List<LearningResource>>();
            _accessPolicies = InitializeAccessPolicies();
            
            InitializeResourceCatalog();
        }

        /// <summary>
        /// Get appropriate learning resources based on developmental stage and interests
        /// </summary>
        public Task<List<LearningResource>> GetRecommendedResourcesAsync(
            DevelopmentalStage stage, 
            List<string> interests, 
            Dictionary<string, double> masteryLevels)
        {
            var policy = _accessPolicies[stage];
            var availableResources = new List<LearningResource>();
            
            // Filter resources by developmental appropriateness
            foreach (var resourceType in policy.AllowedResourceTypes)
            {
                if (_resourceCatalog.ContainsKey(resourceType))
                {
                    var resources = _resourceCatalog[resourceType]
                        .Where(r => r.DifficultyLevel >= policy.MinDifficulty && 
                                   r.DifficultyLevel <= policy.MaxDifficulty)
                        .Where(r => interests.Any(i => r.Topics.Contains(i, StringComparer.OrdinalIgnoreCase)) || 
                                   interests.Count == 0)
                        .Take(policy.MaxResourcesPerSession)
                        .ToList();
                    
                    availableResources.AddRange(resources);
                }
            }
            
            // Sort by relevance and difficulty progression
            availableResources = availableResources
                .OrderBy(r => r.DifficultyLevel)
                .ThenByDescending(r => CalculateRelevanceScore(r, interests, masteryLevels))
                .ToList();
            
            Console.WriteLine($"📚 Found {availableResources.Count} appropriate resources for {stage} stage");
            
            return Task.FromResult(availableResources);
        }

        /// <summary>
        /// Scan the physical library and catalog resources
        /// </summary>
        public async Task ScanAndCatalogLibraryAsync()
        {
            if (!Directory.Exists(_libraryPath))
            {
                Console.WriteLine($"📁 Creating library directory: {_libraryPath}");
                Directory.CreateDirectory(_libraryPath);
            }
            
            Console.WriteLine("🔍 Scanning 37TB learning library...");
            
            // Scan for different resource types
            await ScanTextResources();
            await ScanCodeRepositories();
            await ScanMultimediaContent();
            await ScanInteractiveContent();
            
            // Save catalog
            await SaveResourceCatalog();
            
            var totalResources = _resourceCatalog.Values.Sum(list => list.Count);
            Console.WriteLine($" Library scan complete: {totalResources} resources cataloged");
        }

        private void InitializeResourceCatalog()
        {
            _resourceCatalog[ResourceType.TextBooks] = new List<LearningResource>();
            _resourceCatalog[ResourceType.Literature] = new List<LearningResource>();
            _resourceCatalog[ResourceType.ScientificPapers] = new List<LearningResource>();
            _resourceCatalog[ResourceType.Documentation] = new List<LearningResource>();
            _resourceCatalog[ResourceType.Code] = new List<LearningResource>();
            _resourceCatalog[ResourceType.Multimedia] = new List<LearningResource>();
            _resourceCatalog[ResourceType.Interactive] = new List<LearningResource>();
        }

        private Task ScanTextResources()
        {
            var textPath = Path.Combine(_libraryPath, "text");
            Directory.CreateDirectory(textPath);
            
            // Create subdirectories for different text types
            var subdirs = new[]
            {
                "textbooks/elementary", "textbooks/intermediate", "textbooks/advanced",
                "literature/classics", "literature/contemporary", "literature/poetry",
                "scientific_papers/computer_science", "scientific_papers/mathematics", 
                "scientific_papers/physics", "scientific_papers/biology",
                "documentation/programming", "documentation/systems", "documentation/apis",
                "reference/dictionaries", "reference/encyclopedias", "reference/manuals"
            };
            
            foreach (var subdir in subdirs)
            {
                Directory.CreateDirectory(Path.Combine(textPath, subdir));
                
                // Add placeholder resources
                var resourceType = subdir.Contains("textbook") ? ResourceType.TextBooks :
                                  subdir.Contains("literature") ? ResourceType.Literature :
                                  subdir.Contains("scientific") ? ResourceType.ScientificPapers :
                                  ResourceType.Documentation;
                
                var difficulty = subdir.Contains("elementary") ? 0.3 :
                               subdir.Contains("intermediate") ? 0.6 :
                               subdir.Contains("advanced") ? 0.9 : 0.5;
                
                _resourceCatalog[resourceType].Add(new LearningResource
                {
                    Title = $"Resources in {subdir}",
                    Path = Path.Combine(textPath, subdir),
                    Type = resourceType,
                    DifficultyLevel = difficulty,
                    Topics = ExtractTopicsFromPath(subdir),
                    EstimatedLearningTime = TimeSpan.FromHours(2)
                });
            }
            
            return Task.CompletedTask;
        }

        private Task ScanCodeRepositories()
        {
            var codePath = Path.Combine(_libraryPath, "code");
            Directory.CreateDirectory(codePath);
            
            var codeTypes = new[]
            {
                "examples/beginner", "examples/intermediate", "examples/advanced",
                "open_source/libraries", "open_source/frameworks", "open_source/tools",
                "algorithms/sorting", "algorithms/searching", "algorithms/graph",
                "projects/simple", "projects/medium", "projects/complex"
            };
            
            foreach (var codeType in codeTypes)
            {
                var fullPath = Path.Combine(codePath, codeType);
                Directory.CreateDirectory(fullPath);
                
                var difficulty = codeType.Contains("beginner") || codeType.Contains("simple") ? 0.3 :
                               codeType.Contains("intermediate") || codeType.Contains("medium") ? 0.6 :
                               0.9;
                
                _resourceCatalog[ResourceType.Code].Add(new LearningResource
                {
                    Title = $"Code examples: {codeType}",
                    Path = fullPath,
                    Type = ResourceType.Code,
                    DifficultyLevel = difficulty,
                    Topics = ExtractTopicsFromPath(codeType),
                    EstimatedLearningTime = TimeSpan.FromHours(1)
                });
            }
            
            return Task.CompletedTask;
        }

        private Task ScanMultimediaContent()
        {
            var mediaPath = Path.Combine(_libraryPath, "multimedia");
            Directory.CreateDirectory(mediaPath);
            
            var mediaTypes = new[]
            {
                "videos/educational", "videos/documentaries", "videos/tutorials",
                "audio/lectures", "audio/podcasts", "audio/audiobooks",
                "images/diagrams", "images/charts", "images/art"
            };
            
            foreach (var mediaType in mediaTypes)
            {
                Directory.CreateDirectory(Path.Combine(mediaPath, mediaType));
                
                _resourceCatalog[ResourceType.Multimedia].Add(new LearningResource
                {
                    Title = $"Multimedia: {mediaType}",
                    Path = Path.Combine(mediaPath, mediaType),
                    Type = ResourceType.Multimedia,
                    DifficultyLevel = 0.5,
                    Topics = ExtractTopicsFromPath(mediaType),
                    EstimatedLearningTime = TimeSpan.FromMinutes(30)
                });
            }
            
            return Task.CompletedTask;
        }

        private Task ScanInteractiveContent()
        {
            var interactivePath = Path.Combine(_libraryPath, "interactive");
            Directory.CreateDirectory(interactivePath);
            
            var interactiveTypes = new[]
            {
                "simulations/physics", "simulations/chemistry", "simulations/biology",
                "games/educational", "games/puzzle", "games/strategy",
                "exercises/math", "exercises/programming", "exercises/logic"
            };
            
            foreach (var interactiveType in interactiveTypes)
            {
                Directory.CreateDirectory(Path.Combine(interactivePath, interactiveType));
                
                _resourceCatalog[ResourceType.Interactive].Add(new LearningResource
                {
                    Title = $"Interactive: {interactiveType}",
                    Path = Path.Combine(interactivePath, interactiveType),
                    Type = ResourceType.Interactive,
                    DifficultyLevel = 0.6,
                    Topics = ExtractTopicsFromPath(interactiveType),
                    EstimatedLearningTime = TimeSpan.FromMinutes(45)
                });
            }
            
            return Task.CompletedTask;
        }

        private List<string> ExtractTopicsFromPath(string path)
        {
            return path.Split('/', '_')
                      .Where(part => !string.IsNullOrWhiteSpace(part))
                      .ToList();
        }

        private double CalculateRelevanceScore(LearningResource resource, List<string> interests, Dictionary<string, double> masteryLevels)
        {
            double score = 0.0;
            
            // Interest alignment
            foreach (var interest in interests)
            {
                if (resource.Topics.Any(t => t.Contains(interest, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 0.5;
                }
            }
            
            // Difficulty progression (prefer slightly challenging resources)
            var avgMastery = masteryLevels.Values.Any() ? masteryLevels.Values.Average() : 0.0;
            var difficultyMatch = 1.0 - Math.Abs(resource.DifficultyLevel - (avgMastery + 0.1));
            score += difficultyMatch * 0.3;
            
            // Resource type variety bonus
            score += 0.2;
            
            return score;
        }

        private Dictionary<DevelopmentalStage, ResourceAccessPolicy> InitializeAccessPolicies()
        {
            return new Dictionary<DevelopmentalStage, ResourceAccessPolicy>
            {
                [DevelopmentalStage.Guided] = new ResourceAccessPolicy
                {
                    AllowedResourceTypes = new[] { ResourceType.TextBooks, ResourceType.Interactive },
                    MinDifficulty = 0.0,
                    MaxDifficulty = 0.4,
                    MaxResourcesPerSession = 3,
                    RequiresSupervision = true
                },
                [DevelopmentalStage.Scaffolded] = new ResourceAccessPolicy
                {
                    AllowedResourceTypes = new[] { ResourceType.TextBooks, ResourceType.Literature, ResourceType.Interactive, ResourceType.Multimedia },
                    MinDifficulty = 0.2,
                    MaxDifficulty = 0.7,
                    MaxResourcesPerSession = 5,
                    RequiresSupervision = false
                },
                [DevelopmentalStage.SelfDirected] = new ResourceAccessPolicy
                {
                    AllowedResourceTypes = new[] { ResourceType.TextBooks, ResourceType.Literature, ResourceType.Code, ResourceType.Documentation, ResourceType.Multimedia, ResourceType.Interactive },
                    MinDifficulty = 0.3,
                    MaxDifficulty = 0.9,
                    MaxResourcesPerSession = 10,
                    RequiresSupervision = false
                },
                [DevelopmentalStage.Autonomous] = new ResourceAccessPolicy
                {
                    AllowedResourceTypes = Enum.GetValues<ResourceType>(),
                    MinDifficulty = 0.0,
                    MaxDifficulty = 1.0,
                    MaxResourcesPerSession = int.MaxValue,
                    RequiresSupervision = false
                }
            };
        }

        private async Task SaveResourceCatalog()
        {
            var catalogPath = Path.Combine(_libraryPath, "resource_catalog.json");
            var catalogData = new
            {
                LastUpdated = DateTime.UtcNow,
                TotalResources = _resourceCatalog.Values.Sum(list => list.Count),
                ResourcesByType = _resourceCatalog.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value.Count
                ),
                Catalog = _resourceCatalog
            };
            
            var json = JsonSerializer.Serialize(catalogData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(catalogPath, json);
        }

        /// <summary>
        /// Generate a learning resource collection guide
        /// </summary>
        public string GenerateCollectionGuide()
        {
            return @"
🏗️  **37TB DIGITAL LEARNING LIBRARY SETUP GUIDE**
==================================================

📚 **RECOMMENDED RESOURCES TO COLLECT**

🎯 **HIGH PRIORITY - Start with these:**

1. PROJECT GUTENBERG (60,000+ free books)
   - Download from: http://www.gutenberg.org/
   - Size: ~50GB
   - Target: /text/literature/classics/

2. OPENSTAX TEXTBOOKS (free college textbooks)
   - Download from: https://openstax.org/
   - Subjects: Math, Science, History, Economics
   - Target: /text/textbooks/

3. MIT OPENCOURSEWARE
   - Course materials from MIT courses
   - Download from: https://ocw.mit.edu/
   - Target: /text/textbooks/advanced/

4. WIKIPEDIA DUMPS
   - Full Wikipedia content (~20GB compressed)
   - Target: /text/reference/encyclopedias/

🔬 **SCIENTIFIC RESOURCES:**

5. ARXIV.ORG PAPERS
   - Open access scientific papers
   - Categories: cs.AI, cs.LG, math.*, physics.*
   - Size: ~2TB and growing
   - Target: /text/scientific_papers/

6. PUBMED CENTRAL
   - Biomedical literature
   - Target: /text/scientific_papers/biology/

🛠️  **CODE REPOSITORIES:**

7. GITHUB EDUCATIONAL REPOS
   - freeCodeCamp curriculum
   - TheAlgorithms repositories
   - Awesome lists
   - Target: /code/examples/

🎥 **MULTIMEDIA CONTENT:**

8. EDUCATIONAL YOUTUBE CHANNELS
   - 3Blue1Brown (Math visualization)
   - Khan Academy
   - TED-Ed
   - Crash Course
   - Target: /multimedia/videos/educational/

📊 **EXPECTED STORAGE USAGE:**
- Text content: 5-10 TB
- Code repositories: 2-5 TB
- Scientific papers: 10-15 TB
- Multimedia: 15-20 TB
- Total: ~37 TB

⚠️  **IMPORTANT NOTES:**
- Respect copyright and terms of service
- Use rate limiting to avoid overwhelming servers
- Consider using academic/institutional access
- Set up automated update scripts
- Implement deduplication to save space

🚀 **GETTING STARTED:**
1. Start with Project Gutenberg and OpenStax (free, no restrictions)
2. Set up automated arXiv paper downloads
3. Clone essential GitHub repositories
4. Download Wikipedia dumps
5. Gradually expand to other sources

💡 **PRO TIP:**
Create a simple web interface to browse your library.
This will help you understand what resources you have
and will be useful for integrating with the AI system.
";
        }
    }

    #region Supporting Classes

    public enum ResourceType
    {
        TextBooks,
        Literature,
        ScientificPapers,
        Documentation,
        Code,
        Multimedia,
        Interactive
    }

    public class LearningResource
    {
        public string Title { get; set; } = "";
        public string Path { get; set; } = "";
        public ResourceType Type { get; set; }
        public double DifficultyLevel { get; set; } = 0.5; // 0.0 = beginner, 1.0 = expert
        public List<string> Topics { get; set; } = new();
        public TimeSpan EstimatedLearningTime { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        public int TimesAccessed { get; set; } = 0;
        public double UserRating { get; set; } = 0.0;
    }

    public class ResourceAccessPolicy
    {
        public ResourceType[] AllowedResourceTypes { get; set; } = Array.Empty<ResourceType>();
        public double MinDifficulty { get; set; } = 0.0;
        public double MaxDifficulty { get; set; } = 1.0;
        public int MaxResourcesPerSession { get; set; } = 5;
        public bool RequiresSupervision { get; set; } = false;
    }

    #endregion
}
