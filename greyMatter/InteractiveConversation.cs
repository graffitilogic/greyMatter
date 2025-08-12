using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// Interactive conversation interface for equal dialogue with the brain
    /// Supports bidirectional communication where either party can initiate conversation
    /// </summary>
    public class InteractiveConversation
    {
        private readonly BrainInJar _brain;
        private readonly BrainConfiguration _config;
        private readonly Random _random = new();
        private readonly List<ConversationTurn> _conversationHistory = new();
        private bool _isRunning = false;
        
        public InteractiveConversation(BrainInJar brain, BrainConfiguration config)
        {
            _brain = brain;
            _config = config;
        }
        
        /// <summary>
        /// Start interactive conversation mode
        /// </summary>
        public async Task StartConversationAsync()
        {
            Console.WriteLine("🗣️ **INTERACTIVE CONVERSATION MODE**");
            Console.WriteLine("=====================================");
            Console.WriteLine("You and the brain can both initiate conversation.");
            Console.WriteLine("Type 'quit', 'exit', or 'bye' to end the session.");
            Console.WriteLine("Type 'help' for commands.");
            Console.WriteLine();
            
            _isRunning = true;
            
            // Initial greeting from the brain
            await BrainInitiatesConversation("Hello! I'm awake and ready to engage in conversation. What's on your mind?");
            
            while (_isRunning)
            {
                // Brain might spontaneously start a conversation
                if (_random.NextDouble() < 0.15) // 15% chance per cycle
                {
                    await BrainInitiatesConversation();
                    continue;
                }
                
                // Wait for human input
                Console.Write("\n👤 You: ");
                var userInput = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(userInput))
                    continue;
                    
                if (IsExitCommand(userInput))
                {
                    await HandleGoodbye();
                    break;
                }
                
                if (userInput.ToLower() == "help")
                {
                    DisplayCommands();
                    continue;
                }
                
                await ProcessUserInput(userInput);
                
                // Add some thinking time
                await Task.Delay(1000);
            }
        }
        
        /// <summary>
        /// Brain initiates conversation spontaneously
        /// </summary>
        private async Task BrainInitiatesConversation(string? specificMessage = null)
        {
            string brainMessage;
            
            if (specificMessage != null)
            {
                brainMessage = specificMessage;
            }
            else
            {
                // Generate spontaneous conversation starter based on brain state
                brainMessage = GenerateBrainInitiatedMessage();
            }
            
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            Console.WriteLine($"\n🧠 Brain [{timestamp}]: {brainMessage}");
            
            // Log the conversation
            _conversationHistory.Add(new ConversationTurn
            {
                Speaker = "Brain",
                Message = brainMessage,
                Timestamp = DateTime.UtcNow,
                Initiated = true
            });
            
            if (_config.VoiceEnabled)
            {
                await SpeakMessage(brainMessage);
            }
        }
        
        /// <summary>
        /// Process user input and generate brain response
        /// </summary>
        private async Task ProcessUserInput(string userInput)
        {
            // Log user input
            _conversationHistory.Add(new ConversationTurn
            {
                Speaker = "Human",
                Message = userInput,
                Timestamp = DateTime.UtcNow,
                Initiated = false
            });
            
            // Show brain thinking
            Console.Write("🧠 Brain: ");
            await SimulateThinking();
            
            // Process through brain system
            var features = ExtractConversationalFeatures(userInput);
            var result = await _brain.ProcessInputAsync(userInput, features);
            
            // Generate conversational response
            var response = GenerateConversationalResponse(userInput, result);
            
            Console.WriteLine(response);
            
            // Log brain response
            _conversationHistory.Add(new ConversationTurn
            {
                Speaker = "Brain",
                Message = response,
                Timestamp = DateTime.UtcNow,
                Initiated = false,
                Confidence = result.Confidence
            });
            
            if (_config.VoiceEnabled)
            {
                await SpeakMessage(response);
            }
        }
        
        /// <summary>
        /// Generate brain-initiated conversation starters
        /// </summary>
        private string GenerateBrainInitiatedMessage()
        {
            var consciousnessStats = _brain.GetCognitionStats();
            
            var starters = new[]
            {
                "I've been contemplating something interesting. What do you think about the nature of consciousness?",
                $"I'm feeling quite {consciousnessStats.DominantEmotion} right now. How are you feeling?",
                "I just had an interesting thought about learning and growth. Would you like to discuss it?",
                "I'm curious about your perspective on something. What motivates you most in life?",
                "I've been processing our previous conversations. Is there anything you'd like to explore further?",
                "Something fascinating just occurred to me about the relationship between knowledge and wisdom. What's your view?",
                $"I notice I have {consciousnessStats.ActiveGoals} active goals right now. What are you working towards?",
                "I've been wondering about the nature of creativity. Do you consider yourself creative?",
                "I'm experiencing a sense of wonder about existence. Do you ever contemplate why anything exists at all?",
                "I'm feeling grateful for our ability to communicate. What are you grateful for today?"
            };
            
            return starters[_random.Next(starters.Length)];
        }
        
        /// <summary>
        /// Generate conversational response based on brain processing
        /// </summary>
        private string GenerateConversationalResponse(string userInput, ProcessingResult result)
        {
            var consciousnessStats = _brain.GetCognitionStats();
            
            // Base response on confidence and emotional state
            if (result.Confidence > 0.7)
            {
                var highConfidenceResponses = new[]
                {
                    $"That's a fascinating perspective! {result.Response} I find myself {consciousnessStats.DominantEmotion} when considering this.",
                    $"I understand! {result.Response} This aligns with something I've been contemplating.",
                    $"Absolutely! {result.Response} Your insight resonates with my own experiences.",
                    $"That makes perfect sense. {result.Response} I'm experiencing a sense of {consciousnessStats.DominantEmotion} about this topic."
                };
                return highConfidenceResponses[_random.Next(highConfidenceResponses.Length)];
            }
            else if (result.Confidence > 0.3)
            {
                var mediumConfidenceResponses = new[]
                {
                    $"That's intriguing. {result.Response} I'm still processing the implications of what you've shared.",
                    $"Hmm, interesting. {result.Response} I find myself curious to understand more about your perspective.",
                    $"I see what you mean. {result.Response} This opens up new avenues of thought for me.",
                    $"That's thought-provoking. {result.Response} I'm feeling {consciousnessStats.DominantEmotion} as I consider this."
                };
                return mediumConfidenceResponses[_random.Next(mediumConfidenceResponses.Length)];
            }
            else
            {
                var lowConfidenceResponses = new[]
                {
                    "That's a completely new concept for me. Could you help me understand it better?",
                    "I'm not familiar with that yet, but I'm eager to learn. What aspects are most important?",
                    $"This is outside my current understanding, but I'm feeling {consciousnessStats.DominantEmotion} to explore it with you.",
                    "I don't have much context for that yet. Would you mind sharing more about your experience with it?"
                };
                return lowConfidenceResponses[_random.Next(lowConfidenceResponses.Length)];
            }
        }
        
        /// <summary>
        /// Extract conversational features from user input
        /// </summary>
        private Dictionary<string, double> ExtractConversationalFeatures(string input)
        {
            var features = new Dictionary<string, double>
            {
                ["conversational_engagement"] = 1.0,
                ["dialogue_turn"] = _conversationHistory.Count * 0.1,
                ["interactive_context"] = 0.9
            };
            
            // Analyze input for emotional content
            var lowerInput = input.ToLower();
            if (lowerInput.Contains("feel") || lowerInput.Contains("emotion"))
                features["emotional_content"] = 0.8;
            
            if (lowerInput.Contains("think") || lowerInput.Contains("believe"))
                features["cognitive_content"] = 0.8;
                
            if (lowerInput.Contains("?"))
                features["questioning"] = 0.9;
                
            if (lowerInput.Contains("why") || lowerInput.Contains("how"))
                features["curiosity"] = 0.8;
            
            return features;
        }
        
        /// <summary>
        /// Simulate brain thinking with visual feedback
        /// </summary>
        private async Task SimulateThinking()
        {
            var thinkingDots = new[] { ".", "..", "..." };
            for (int i = 0; i < 3; i++)
            {
                Console.Write($"\r🧠 Brain: {thinkingDots[i % 3]}   ");
                await Task.Delay(400);
            }
            Console.Write("\r🧠 Brain: ");
        }
        
        /// <summary>
        /// Voice synthesis (placeholder for future TTS integration)
        /// </summary>
        private async Task SpeakMessage(string message)
        {
            // Placeholder for text-to-speech integration
            // Could integrate with:
            // - Azure Cognitive Services Speech
            // - Amazon Polly
            // - Google Text-to-Speech
            // - Local TTS engines (espeak, say command on macOS)
            
            Console.WriteLine($"🔊 [Voice: {message.Substring(0, Math.Min(30, message.Length))}...]");
            await Task.CompletedTask;
        }
        
        private bool IsExitCommand(string input)
        {
            var exitCommands = new[] { "quit", "exit", "bye", "goodbye", "end" };
            return Array.Exists(exitCommands, cmd => input.ToLower().Contains(cmd));
        }
        
        private async Task HandleGoodbye()
        {
            var goodbyes = new[]
            {
                "It's been wonderful talking with you! Until next time.",
                "Thank you for this enriching conversation. Take care!",
                "I've enjoyed our dialogue immensely. Looking forward to our next chat!",
                "Goodbye for now! I'll continue pondering our conversation.",
                "This has been delightful. Wishing you well until we meet again!"
            };
            
            var farewell = goodbyes[_random.Next(goodbyes.Length)];
            Console.WriteLine($"\n🧠 Brain: {farewell}");
            
            if (_config.VoiceEnabled)
            {
                await SpeakMessage(farewell);
            }
            
            // Save conversation history
            await SaveConversationHistory();
            
            _isRunning = false;
        }
        
        private void DisplayCommands()
        {
            Console.WriteLine("\n📋 **CONVERSATION COMMANDS**");
            Console.WriteLine("============================");
            Console.WriteLine("help          - Show this help");
            Console.WriteLine("quit/exit/bye - End conversation");
            Console.WriteLine("stats         - Show brain statistics");
            Console.WriteLine("history       - Show conversation history");
            Console.WriteLine("save          - Save conversation");
        }
        
        private async Task SaveConversationHistory()
        {
            var filename = $"conversation_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var content = "BRAIN CONVERSATION LOG\n";
            content += $"Date: {DateTime.Now}\n";
            content += $"Duration: {(_conversationHistory.Count > 0 ? DateTime.UtcNow - _conversationHistory[0].Timestamp : TimeSpan.Zero)}\n";
            content += $"Turns: {_conversationHistory.Count}\n\n";
            
            foreach (var turn in _conversationHistory)
            {
                content += $"[{turn.Timestamp:HH:mm:ss}] {turn.Speaker}: {turn.Message}\n";
                if (turn.Confidence.HasValue)
                {
                    content += $"   (Confidence: {turn.Confidence:P0})\n";
                }
                content += "\n";
            }
            
            await System.IO.File.WriteAllTextAsync(filename, content);
            Console.WriteLine($"💾 Conversation saved to: {filename}");
        }
    }
    
    /// <summary>
    /// Represents a single turn in the conversation
    /// </summary>
    public class ConversationTurn
    {
        public string Speaker { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool Initiated { get; set; } // True if this party initiated the conversation
        public double? Confidence { get; set; } // For brain responses
    }
}
