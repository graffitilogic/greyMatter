using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace GreyMatter.DataIntegration
{
    public class EnhancedDataIntegrator
    {
        private readonly RealLanguageLearner _learner;
        private readonly string _trainDataPath = "/Volumes/jarvis/trainData";

        public EnhancedDataIntegrator(RealLanguageLearner learner)
        {
            _learner = learner;
        }

        public async Task IntegrateAllSourcesAsync()
        {
            Console.WriteLine("🚀 Starting Enhanced Data Integration...");

            // 1. Process SimpleWiki (largest source)
            await ProcessSimpleWikiAsync();

            // 2. Process News Headlines
            await ProcessNewsHeadlinesAsync();

            // 3. Process Scientific Abstracts
            await ProcessScientificAbstractsAsync();

            // 4. Process Children's Literature
            await ProcessChildrensLiteratureAsync();

            // 5. Process Idioms and Expressions
            await ProcessIdiomsAndExpressionsAsync();

            // 6. Process Technical Documentation
            await ProcessTechnicalDocsAsync();

            // 7. Process Social Media Data
            await ProcessSocialMediaAsync();

            // 8. Process Open Subtitles
            await ProcessOpenSubtitlesAsync();

            Console.WriteLine("✅ Enhanced Data Integration Complete!");
        }

        private async Task ProcessSimpleWikiAsync()
        {
            Console.WriteLine("📖 Processing SimpleWiki Articles...");

            string wikiPath = Path.Combine(_trainDataPath, "SimpleWiki", "simplewiki-latest-pages-articles.xml");

            if (!File.Exists(wikiPath))
            {
                Console.WriteLine("⚠️ SimpleWiki file not found, skipping...");
                return;
            }

            int articleCount = 0;
            try
            {
                using (StreamReader reader = new StreamReader(wikiPath))
                {
                    string line;
                    string currentTitle = "";
                    string currentText = "";

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (line.Contains("<title>"))
                        {
                            currentTitle = ExtractXmlContent(line, "title");
                        }
                        else if (line.Contains("<text"))
                        {
                            currentText = await ReadUntilClosingTagAsync(reader, "text");
                            currentText = CleanWikiText(currentText);

                            if (!string.IsNullOrWhiteSpace(currentText) && currentText.Length > 50)
                            {
                                await _learner.LearnFromTextAsync(currentText, "encyclopedic");
                                articleCount++;

                                if (articleCount % 100 == 0)
                                {
                                    Console.WriteLine($"📖 Processed {articleCount} SimpleWiki articles");
                                }

                                // Limit to prevent overwhelming the system
                                if (articleCount >= 1000) break;
                            }
                        }
                    }
                }

                Console.WriteLine($"✅ Processed {articleCount} SimpleWiki articles");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing SimpleWiki: {ex.Message}");
            }
        }

        private async Task ProcessNewsHeadlinesAsync()
        {
            Console.WriteLine("📰 Processing News Headlines...");

            string newsPath = Path.Combine(_trainDataPath, "enhanced_sources", "NewsData", "news_headlines.txt");

            if (!File.Exists(newsPath))
            {
                Console.WriteLine("⚠️ News headlines file not found, skipping...");
                return;
            }

            try
            {
                string[] headlines = await File.ReadAllLinesAsync(newsPath);
                int processed = 0;

                foreach (string headline in headlines)
                {
                    if (!string.IsNullOrWhiteSpace(headline) && headline.Length > 10)
                    {
                        await _learner.LearnFromTextAsync(headline, "news");
                        processed++;

                        if (processed % 500 == 0)
                        {
                            Console.WriteLine($"📰 Processed {processed} news headlines");
                        }
                    }
                }

                Console.WriteLine($"✅ Processed {processed} news headlines");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing news headlines: {ex.Message}");
            }
        }

        private async Task ProcessScientificAbstractsAsync()
        {
            Console.WriteLine("🔬 Processing Scientific Abstracts...");

            string sciencePath = Path.Combine(_trainDataPath, "enhanced_sources", "ScienceData");

            if (!Directory.Exists(sciencePath))
            {
                Console.WriteLine("⚠️ Science data directory not found, skipping...");
                return;
            }

            try
            {
                var scienceFiles = Directory.GetFiles(sciencePath, "*.txt");
                int totalProcessed = 0;

                foreach (string file in scienceFiles)
                {
                    string[] abstracts = await File.ReadAllLinesAsync(file);
                    int processed = 0;

                    foreach (string abstractText in abstracts)
                    {
                        if (!string.IsNullOrWhiteSpace(abstractText) && abstractText.Length > 20)
                        {
                            await _learner.LearnFromTextAsync(abstractText, "scientific");
                            processed++;
                            totalProcessed++;

                            if (processed % 100 == 0)
                            {
                                Console.WriteLine($"🔬 Processed {processed} abstracts from {Path.GetFileName(file)}");
                            }
                        }
                    }
                }

                Console.WriteLine($"✅ Processed {totalProcessed} scientific abstracts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing scientific abstracts: {ex.Message}");
            }
        }

        private async Task ProcessChildrensLiteratureAsync()
        {
            Console.WriteLine("📚 Processing Children's Literature...");

            string childrensPath = Path.Combine(_trainDataPath, "enhanced_sources", "ChildrensLiterature");

            if (!Directory.Exists(childrensPath))
            {
                Console.WriteLine("⚠️ Children's literature directory not found, skipping...");
                return;
            }

            try
            {
                var literatureFiles = Directory.GetFiles(childrensPath, "*.txt");
                int totalProcessed = 0;

                foreach (string file in literatureFiles)
                {
                    string content = await File.ReadAllTextAsync(file);
                    string[] sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

                    int processed = 0;
                    foreach (string sentence in sentences)
                    {
                        string cleanSentence = sentence.Trim();
                        if (!string.IsNullOrWhiteSpace(cleanSentence) && cleanSentence.Length > 10)
                        {
                            await _learner.LearnFromTextAsync(cleanSentence, "narrative");
                            processed++;
                            totalProcessed++;

                            if (processed % 200 == 0)
                            {
                                Console.WriteLine($"📚 Processed {processed} sentences from {Path.GetFileName(file)}");
                            }
                        }
                    }
                }

                Console.WriteLine($"✅ Processed {totalProcessed} children's literature sentences");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing children's literature: {ex.Message}");
            }
        }

        private async Task ProcessIdiomsAndExpressionsAsync()
        {
            Console.WriteLine("💬 Processing Idioms and Expressions...");

            string idiomsPath = Path.Combine(_trainDataPath, "enhanced_sources", "idioms_expressions.json");

            if (!File.Exists(idiomsPath))
            {
                Console.WriteLine("⚠️ Idioms file not found, skipping...");
                return;
            }

            try
            {
                string jsonContent = await File.ReadAllTextAsync(idiomsPath);
                // Parse JSON and extract idioms
                var idioms = ParseIdiomsJson(jsonContent);

                int processed = 0;
                foreach (string idiom in idioms)
                {
                    if (!string.IsNullOrWhiteSpace(idiom))
                    {
                        await _learner.LearnFromTextAsync(idiom, "idiomatic");
                        processed++;

                        if (processed % 50 == 0)
                        {
                            Console.WriteLine($"💬 Processed {processed} idioms and expressions");
                        }
                    }
                }

                Console.WriteLine($"✅ Processed {processed} idioms and expressions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing idioms: {ex.Message}");
            }
        }

        private async Task ProcessTechnicalDocsAsync()
        {
            Console.WriteLine("📋 Processing Technical Documentation...");

            string techPath = Path.Combine(_trainDataPath, "enhanced_sources", "TechnicalDocs");

            if (!Directory.Exists(techPath))
            {
                Console.WriteLine("⚠️ Technical docs directory not found, skipping...");
                return;
            }

            try
            {
                var techFiles = Directory.GetFiles(techPath, "*.txt");
                int totalProcessed = 0;

                foreach (string file in techFiles)
                {
                    string content = await File.ReadAllTextAsync(file);
                    string[] paragraphs = content.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

                    int processed = 0;
                    foreach (string paragraph in paragraphs)
                    {
                        string cleanParagraph = paragraph.Trim();
                        if (!string.IsNullOrWhiteSpace(cleanParagraph) && cleanParagraph.Length > 30)
                        {
                            await _learner.LearnFromTextAsync(cleanParagraph, "technical");
                            processed++;
                            totalProcessed++;

                            if (processed % 100 == 0)
                            {
                                Console.WriteLine($"📋 Processed {processed} technical paragraphs from {Path.GetFileName(file)}");
                            }
                        }
                    }
                }

                Console.WriteLine($"✅ Processed {totalProcessed} technical documentation paragraphs");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing technical docs: {ex.Message}");
            }
        }

        private async Task ProcessSocialMediaAsync()
        {
            Console.WriteLine("📱 Processing Social Media Data...");

            string socialPath = Path.Combine(_trainDataPath, "enhanced_sources", "SocialMedia");

            if (!Directory.Exists(socialPath))
            {
                Console.WriteLine("⚠️ Social media directory not found, skipping...");
                return;
            }

            try
            {
                var socialFiles = Directory.GetFiles(socialPath, "*.txt");
                int totalProcessed = 0;

                foreach (string file in socialFiles)
                {
                    string[] posts = await File.ReadAllLinesAsync(file);
                    int processed = 0;

                    foreach (string post in posts)
                    {
                        if (!string.IsNullOrWhiteSpace(post) && post.Length > 5)
                        {
                            await _learner.LearnFromTextAsync(post, "conversational");
                            processed++;
                            totalProcessed++;

                            if (processed % 200 == 0)
                            {
                                Console.WriteLine($"📱 Processed {processed} social media posts from {Path.GetFileName(file)}");
                            }
                        }
                    }
                }

                Console.WriteLine($"✅ Processed {totalProcessed} social media posts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing social media: {ex.Message}");
            }
        }

        private async Task ProcessOpenSubtitlesAsync()
        {
            Console.WriteLine("🎬 Processing Open Subtitles...");

            string subtitlesPath = Path.Combine(_trainDataPath, "enhanced_sources", "OpenSubtitles");

            if (!Directory.Exists(subtitlesPath))
            {
                Console.WriteLine("⚠️ Open subtitles directory not found, skipping...");
                return;
            }

            try
            {
                var subtitleFiles = Directory.GetFiles(subtitlesPath, "*.txt");
                int totalProcessed = 0;

                foreach (string file in subtitleFiles)
                {
                    string[] lines = await File.ReadAllLinesAsync(file);
                    int processed = 0;

                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && line.Length > 5 && !line.Contains("-->"))
                        {
                            await _learner.LearnFromTextAsync(line, "conversational");
                            processed++;
                            totalProcessed++;

                            if (processed % 300 == 0)
                            {
                                Console.WriteLine($"🎬 Processed {processed} subtitle lines from {Path.GetFileName(file)}");
                            }
                        }
                    }
                }

                Console.WriteLine($"✅ Processed {totalProcessed} subtitle lines");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing subtitles: {ex.Message}");
            }
        }

        // Helper methods
        private string ExtractXmlContent(string line, string tagName)
        {
            var match = Regex.Match(line, $"<{tagName}>(.*?)</{tagName}>");
            return match.Success ? match.Groups[1].Value : "";
        }

        private async Task<string> ReadUntilClosingTagAsync(StreamReader reader, string tagName)
        {
            string content = "";
            string line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                content += line + "\n";
                if (line.Contains($"</{tagName}>")) break;
            }

            return content;
        }

        private string CleanWikiText(string text)
        {
            // Remove wiki markup
            text = Regex.Replace(text, @"\[\[.*?\]\]", "");
            text = Regex.Replace(text, @"\{\{.*?\}\}", "");
            text = Regex.Replace(text, @"&lt;.*?&gt;", "");
            text = Regex.Replace(text, @"&amp;", "&");
            text = Regex.Replace(text, @"&quot;", "\"");
            text = Regex.Replace(text, @"&apos;", "'");

            // Remove references and external links
            text = Regex.Replace(text, @"\[http.*?\]", "");
            text = Regex.Replace(text, @"&lt;ref&gt;.*?&lt;/ref&gt;", "");

            return text.Trim();
        }

        private List<string> ParseIdiomsJson(string jsonContent)
        {
            var idioms = new List<string>();

            try
            {
                // Simple JSON parsing for idioms
                var matches = Regex.Matches(jsonContent, "\"expression\"\\s*:\\s*\"([^\"]+)\"");
                foreach (Match match in matches)
                {
                    idioms.Add(match.Groups[1].Value);
                }
            }
            catch
            {
                // Fallback: split by common separators
                idioms = jsonContent.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => s.Trim('"', ' ', '\t'))
                                   .Where(s => !string.IsNullOrWhiteSpace(s))
                                   .ToList();
            }

            return idioms;
        }
    }
}
