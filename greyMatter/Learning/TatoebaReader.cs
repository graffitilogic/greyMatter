using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GreyMatter.Learning
{
    /// <summary>
    /// Reader for Tatoeba sentence exports.
    /// Expected format (sentences.csv): id\tlang\ttext
    /// </summary>
    public class TatoebaReader
    {
        public IEnumerable<string> ReadEnglishSentences(string sentencesCsvPath)
        {
            if (!File.Exists(sentencesCsvPath))
                yield break;

            using var fs = new FileStream(sentencesCsvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs, DetectEncoding(fs) ?? new UTF8Encoding(false));

            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                // Robustly split first 3 tab-separated columns; text may contain tabs rarely, but exports usually escape
                var parts = SplitTsv(line, 3);
                if (parts.Count < 3) continue;

                var lang = parts[1];
                if (!string.Equals(lang, "eng", StringComparison.OrdinalIgnoreCase))
                    continue;

                var text = parts[2];
                text = text.Trim();
                if (text.Length == 0) continue;
                yield return text;
            }
        }

        private static Encoding? DetectEncoding(FileStream fs)
        {
            // Peek BOM
            var bom = new byte[4];
            var read = fs.Read(bom, 0, 4);
            fs.Position = 0;
            if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) return new UTF8Encoding(true);
            if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE) return Encoding.Unicode; // UTF-16 LE
            if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF) return Encoding.BigEndianUnicode; // UTF-16 BE
            return null; // default later
        }

        private static List<string> SplitTsv(string line, int expected)
        {
            var result = new List<string>(expected);
            int start = 0;
            for (int i = 0; i < expected - 1; i++)
            {
                int idx = line.IndexOf('\t', start);
                if (idx < 0) { result.Add(line.Substring(start)); return result; }
                result.Add(line.Substring(start, idx - start));
                start = idx + 1;
            }
            result.Add(start <= line.Length ? line.Substring(start) : string.Empty);
            return result;
        }
    }
}
