using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GreyMatter.Core
{
    /// <summary>
    /// LLM Teacher: Uses local Ollama API to guide learning and provide dynamic responses
    /// Acts as a "teacher" for the procedural neural network, deciding what to learn next
    /// and providing classification/conceptual mapping fallbacks
    /// </summary>
    public class LLMTeacher
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly string _model;
        
        public LLMTeacher(string apiUrl = "http://192.168.69.138:11434/api/chat", string model = "deepseek-r1:1.5b")
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10) // 10 second timeout for all requests
            };
            _apiUrl = apiUrl;
            _model = model;
        }

        /// <summary>
        /// Generate educational content on a specific topic as training data
        /// </summary>
        public async Task<EducationalContentResponse> GenerateEducationalContentAsync(ContentRequest request)
        {
            var prompt = $@"Generate educational content on the topic: {request.Topic}

Target audience: {request.TargetAudience}
Difficulty level: {request.DifficultyLevel}
Learning objectives: {string.Join(", ", request.LearningObjectives)}
Target content length: {request.ContentLength} words

Create comprehensive educational material that includes:
1. Key vocabulary words relevant to the topic
2. Core concepts that should be understood
3. Well-structured content explaining the topic
4. Clear learning objectives
5. Estimated time to complete

Provide response in JSON format with exact structure.";

            var response = await QueryLLM(prompt, new
            {
                type = "object",
                properties = new
                {
                    content = new { type = "string" },
                    key_vocabulary = new { type = "array", items = new { type = "string" } },
                    concepts = new { type = "array", items = new { type = "string" } },
                    difficulty_level = new { type = "string" },
                    word_count = new { type = "number" },
                    topic_category = new { type = "string" }
                },
                required = new[] { "content", "key_vocabulary", "concepts", "difficulty_level" }
            });

            if (response != null)
            {
                var contentResponse = JsonSerializer.Deserialize<EducationalContentResponse>(response);
                return contentResponse ?? new EducationalContentResponse
                {
                    Topic = request.Topic,
                    DifficultyLevel = request.DifficultyLevel,
                    Content = "Educational content could not be generated at this time.",
                    KeyVocabulary = new List<string>(),
                    CoreConcepts = new List<string>(),
                    LearningObjectives = request.LearningObjectives,
                    EstimatedDuration = "Unknown"
                };
            }

            return new EducationalContentResponse { 
                Topic = request.Topic, 
                Content = "Educational content generation temporarily unavailable.", 
                DifficultyLevel = request.DifficultyLevel,
                KeyVocabulary = new List<string>(),
                CoreConcepts = new List<string>(),
                LearningObjectives = request.LearningObjectives,
                EstimatedDuration = "Unknown"
            };
        }

        /// <summary>
        /// Generate a curriculum of topics based on current learning state
        /// </summary>
        public async Task<List<string>> GenerateLearningCurriculumAsync(LearningContext context, int topicCount = 10)
        {
            var prompt = $@"You are a curriculum designer. Based on the student's current learning state, generate {topicCount} educational topics they should learn next.

Current State:
- Vocabulary size: {context.VocabularySize}
- Recent words: {string.Join(", ", context.RecentWords.Take(10))}
- Performance: {context.PerformanceMetrics}

Generate a progressive curriculum of topics that build on current knowledge while introducing new concepts.
Topics should be specific enough to generate focused educational content.

Examples of good topics:
- 'The water cycle and precipitation'
- 'Basic principles of democracy'
- 'How photosynthesis works in plants'
- 'Introduction to the solar system'
- 'Economic concepts: supply and demand'";

            var response = await QueryLLM(prompt, new
            {
                type = "object",
                properties = new
                {
                    topics = new { type = "array", items = new { type = "string" } },
                    learning_sequence = new { type = "array", items = new { type = "string" } },
                    rationale = new { type = "string" }
                },
                required = new[] { "topics", "learning_sequence" }
            });

            if (!string.IsNullOrEmpty(response))
            {
                var curriculumResponse = JsonSerializer.Deserialize<CurriculumResponse>(response);
                return curriculumResponse?.Phases?.Select(p => p.Name).ToList() ?? new List<string>();
            }

            return new List<string>();
        }

        /// <summary>
        /// Answer specific questions and provide learning content
        /// </summary>
        public async Task<AnswerResponse> AnswerQuestionAsync(string question, BrainState brainState)
        {
            var prompt = $@"You are an educational teacher. A student has asked: '{question}'

Provide a comprehensive, educational answer that includes:
- Clear explanation of the concept
- Key vocabulary and terms
- Related concepts they should understand
- Examples where appropriate

Student's current level: {brainState.ActiveVocabulary} words learned
Recent focus: {brainState.RecentFocus}

Question: {question}

Provide an educational response:";

            var response = await QueryLLM(prompt, new
            {
                type = "object",
                properties = new
                {
                    answer = new { type = "string" },
                    key_vocabulary = new { type = "array", items = new { type = "string" } },
                    related_concepts = new { type = "array", items = new { type = "string" } },
                    follow_up_topics = new { type = "array", items = new { type = "string" } },
                    difficulty_level = new { type = "string" }
                },
                required = new[] { "answer", "key_vocabulary", "related_concepts" }
            });

            if (response != null)
            {
                var answerResponse = JsonSerializer.Deserialize<AnswerResponse>(response);
                return answerResponse ?? new AnswerResponse
                {
                    Answer = "I'm unable to provide an answer at this time.",
                    KeyConcepts = new List<string>(),
                    RelatedTopics = new List<string>(),
                    Vocabulary = new List<string>(),
                    ConfidenceLevel = "low",
                    FollowUpQuestions = new List<string>()
                };
            }

            return new AnswerResponse { 
                Answer = "I'm unable to provide an answer at this time.", 
                KeyConcepts = new List<string>(),
                RelatedTopics = new List<string>(),
                Vocabulary = new List<string>(),
                ConfidenceLevel = "low",
                FollowUpQuestions = new List<string>()
            };
        }

        public async Task<TeacherResponse> AnalyzeLearningState(LearningContext context)
        {
            var prompt = $@"You are a cognitive learning teacher analyzing a student's progress.

Current State:
- Vocabulary size: {context.VocabularySize}
- Recent words learned: {string.Join(", ", context.RecentWords)}
- Learning sources: {string.Join(", ", context.Sources)}
- Performance metrics: {context.PerformanceMetrics}

Analyze this learning state and provide guidance on what to focus on next.
Consider vocabulary gaps, concept relationships, and optimal learning progression.";

            var response = await QueryLLM(prompt, new
            {
                type = "object",
                properties = new
                {
                    next_focus = new { type = "string" },
                    suggested_topics = new { type = "array", items = new { type = "string" } },
                    learning_priority = new { type = "string", @enum = new[] { "vocabulary", "grammar", "concepts", "relationships" } },
                    confidence = new { type = "number", minimum = 0, maximum = 1 }
                },
                required = new[] { "next_focus", "suggested_topics", "learning_priority", "confidence" }
            });

            return JsonSerializer.Deserialize<TeacherResponse>(response);
        }

        public async Task<ConceptMapping> ProvideConceptualMapping(string word, List<string> context)
        {
            var prompt = $@"Analyze the word '{word}' in the context of these related words: {string.Join(", ", context)}.

Provide a structured conceptual mapping including:
- Semantic category
- Related concepts
- Abstract/concrete classification
- Difficulty level for learning
- Prerequisites (simpler concepts needed first)";

            var response = await QueryLLM(prompt, new
            {
                type = "object",
                properties = new
                {
                    semantic_category = new { type = "string" },
                    related_concepts = new { type = "array", items = new { type = "string" } },
                    is_abstract = new { type = "boolean" },
                    difficulty_level = new { type = "integer", minimum = 1, maximum = 10 },
                    prerequisites = new { type = "array", items = new { type = "string" } },
                    learning_strategy = new { type = "string" }
                },
                required = new[] { "semantic_category", "related_concepts", "is_abstract", "difficulty_level" }
            });

            return JsonSerializer.Deserialize<ConceptMapping>(response);
        }

        public async Task<CurriculumGuidance> SuggestCurriculum(LearningGoals goals)
        {
            var prompt = $@"Design a learning curriculum for an AI system with these goals:
- Target vocabulary size: {goals.TargetVocabularySize}
- Focus areas: {string.Join(", ", goals.FocusAreas)}
- Current level: {goals.CurrentLevel}
- Available data sources: {string.Join(", ", goals.AvailableDataSources)}

Create a structured curriculum with progressive phases, specific data sources to use,
and learning milestones. Consider both breadth and depth of knowledge acquisition.";

            var response = await QueryLLM(prompt, new
            {
                type = "object",
                properties = new
                {
                    phases = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                name = new { type = "string" },
                                description = new { type = "string" },
                                data_sources = new { type = "array", items = new { type = "string" } },
                                target_words = new { type = "integer" },
                                duration_estimate = new { type = "string" },
                                prerequisites = new { type = "array", items = new { type = "string" } }
                            },
                            required = new[] { "name", "description", "data_sources", "target_words" }
                        }
                    },
                    recommended_start = new { type = "string" },
                    success_metrics = new { type = "array", items = new { type = "string" } }
                },
                required = new[] { "phases", "recommended_start" }
            });

            Console.WriteLine($"🔍 Raw curriculum response: {response}");
            return JsonSerializer.Deserialize<CurriculumGuidance>(response);
        }

        public async Task<InteractionResponse> HandleUserQuery(string userInput, BrainState currentState)
        {
            var prompt = $@"A user is interacting with an AI brain system and asks: '{userInput}'

Current brain state:
- Active vocabulary: {currentState.ActiveVocabulary} words
- Recent learning focus: {currentState.RecentFocus}
- Available knowledge domains: {string.Join(", ", currentState.KnowledgeDomains)}

Provide a helpful response that demonstrates the AI's current capabilities while being honest about limitations.
Also suggest how this interaction could guide future learning priorities.";

            var response = await QueryLLM(prompt, new
            {
                type = "object",
                properties = new
                {
                    response_text = new { type = "string" },
                    confidence = new { type = "number", minimum = 0, maximum = 1 },
                    learning_suggestions = new { type = "array", items = new { type = "string" } },
                    knowledge_gaps = new { type = "array", items = new { type = "string" } }
                },
                required = new[] { "response_text", "confidence" }
            });

            return JsonSerializer.Deserialize<InteractionResponse>(response);
        }

        private async Task<string> QueryLLM(string prompt, object format)
        {
            var request = new
            {
                model = _model,
                messages = new[] { new { role = "user", content = prompt } },
                stream = false,
                format = format,
                options = new { temperature = 0.1 }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(_apiUrl, content);
            response.EnsureSuccessStatusCode();
            
            var responseText = await response.Content.ReadAsStringAsync();
            var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseText);
            
            return ollamaResponse.message.content;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // Response Models
    public class TeacherResponse
    {
        public string next_focus { get; set; } = "";
        public List<string> suggested_topics { get; set; } = new();
        public string learning_priority { get; set; } = "";
        public double confidence { get; set; }
    }

    public class ConceptMapping
    {
        public string semantic_category { get; set; } = "";
        public List<string> related_concepts { get; set; } = new();
        public bool is_abstract { get; set; }
        public int difficulty_level { get; set; }
        public List<string> prerequisites { get; set; } = new();
        public string learning_strategy { get; set; } = "";
    }

    public class CurriculumGuidance
    {
        public List<CurriculumPhase> phases { get; set; } = new();
        public string recommended_start { get; set; } = "";
        public List<string> success_metrics { get; set; } = new();
    }

    public class InteractionResponse
    {
        public string response_text { get; set; } = "";
        public double confidence { get; set; }
        public List<string> learning_suggestions { get; set; } = new();
        public List<string> knowledge_gaps { get; set; } = new();
    }

    // Context Models
    public class LearningContext
    {
        public int VocabularySize { get; set; }
        public List<string> RecentWords { get; set; } = new();
        public List<string> Sources { get; set; } = new();
        public string PerformanceMetrics { get; set; } = "";
    }

    public class ContentRequest
    {
        public string Topic { get; set; } = "";
        public string TargetAudience { get; set; } = "";
        public string DifficultyLevel { get; set; } = "";
        public List<string> LearningObjectives { get; set; } = new();
        public int ContentLength { get; set; } = 200;
    }

    public class CurriculumGoals
    {
        public string Topic { get; set; } = "";
        public int TargetChunks { get; set; }
        public string CurrentLevel { get; set; } = "";
        public List<string> FocusAreas { get; set; } = new();
        public List<string> AvailableDataSources { get; set; } = new();
    }

    public class LearningGoals
    {
        public int TargetVocabularySize { get; set; }
        public List<string> FocusAreas { get; set; } = new();
        public string CurrentLevel { get; set; } = "";
        public List<string> AvailableDataSources { get; set; } = new();
    }

    public class BrainState
    {
        public int ActiveVocabulary { get; set; }
        public string RecentFocus { get; set; } = "";
        public List<string> KnowledgeDomains { get; set; } = new();
    }

    // Educational Content Response Classes
    public class EducationalContentResponse
    {
        public string Topic { get; set; } = "";
        public string DifficultyLevel { get; set; } = "";
        public List<string> KeyVocabulary { get; set; } = new();
        public List<string> CoreConcepts { get; set; } = new();
        public string Content { get; set; } = "";
        public List<string> LearningObjectives { get; set; } = new();
        public string EstimatedDuration { get; set; } = "";
    }

    public class CurriculumResponse
    {
        public List<CurriculumPhase> Phases { get; set; } = new();
        public string TotalEstimatedTime { get; set; } = "";
        public List<string> Prerequisites { get; set; } = new();
    }

    public class CurriculumPhase
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        
        [JsonPropertyName("topics")]
        public List<string> Topics { get; set; } = new();
        
        [JsonPropertyName("data_sources")]
        public List<string> DataSources { get; set; } = new();
        
        [JsonPropertyName("target_words")]
        public List<string> TargetWords { get; set; } = new();
        
        [JsonPropertyName("milestones")]
        public List<string> Milestones { get; set; } = new();
        
        [JsonPropertyName("duration_estimate")]
        public string Duration { get; set; } = "";
        
        [JsonPropertyName("prerequisites")]
        public List<string> Prerequisites { get; set; } = new();
        
        [JsonPropertyName("difficulty_level")]
        public string DifficultyLevel { get; set; } = "";
    }

    public class AnswerResponse
    {
        public string Answer { get; set; } = "";
        public List<string> KeyConcepts { get; set; } = new();
        public List<string> RelatedTopics { get; set; } = new();
        public List<string> Vocabulary { get; set; } = new();
        public string ConfidenceLevel { get; set; } = "";
        public List<string> FollowUpQuestions { get; set; } = new();
    }

    // Ollama API Response
    public class OllamaResponse
    {
        public OllamaMessage message { get; set; } = new();
    }

    public class OllamaMessage
    {
        public string content { get; set; } = "";
    }
}
