
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace GreyMatter.Learning
{
    /// <summary>
    /// A stream-based reader for processing large Wikipedia XML dumps without loading the entire file into memory.
    /// </summary>
    public class WikipediaStreamReader
    {
        private readonly string _filePath;

        public WikipediaStreamReader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Wikipedia XML dump file not found.", filePath);
            }
            _filePath = filePath;
        }

        /// <summary>
        /// Reads the XML dump and yields the text content of each article.
        /// </summary>
        public IEnumerable<string> ReadArticles()
        {
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                DtdProcessing = DtdProcessing.Parse,
                IgnoreWhitespace = true
            };

            using (var reader = XmlReader.Create(_filePath, settings))
            {
                while (reader.ReadToFollowing("page"))
                {
                    if (reader.ReadToDescendant("text"))
                    {
                        yield return reader.ReadElementContentAsString();
                    }
                }
            }
        }
    }
}
