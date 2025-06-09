using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace TCUWatcher.Infrastructure.Helpers
{
    public class FuzzyMatcherService
    {
        private readonly int _threshold;
        private readonly HashSet<string> _keywords;

        public FuzzyMatcherService(IEnumerable<string> keywords, int threshold = 2)
        {
            _threshold = threshold;
            _keywords = new HashSet<string>(keywords.Select(Normalize));
        }

        public bool Matches(string input, IEnumerable<string> patterns)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            string normalizedInput = Normalize(input);
            var inputWords = new HashSet<string>(normalizedInput.Split(' '));
            var matchedKeywords = inputWords.Count(word => _keywords.Any(keyword => Levenshtein(word, keyword) <= 1));
            if (matchedKeywords >= 2)
                return true;

            int dynamicThreshold = Math.Max(_threshold, normalizedInput.Length / 5);
            int averageTop2 = FindAverageOfTopTwoMatches(normalizedInput, patterns);
            return averageTop2 <= dynamicThreshold;
        }

        private int FindAverageOfTopTwoMatches(string normalizedInput, IEnumerable<string> patterns)
        {
            var distances = new List<int>();
            foreach (var pattern in patterns)
            {
                var normalizedPattern = Normalize(pattern);
                if (string.IsNullOrEmpty(normalizedPattern)) continue;

                if (normalizedInput.Contains(normalizedPattern) || normalizedPattern.Contains(normalizedInput))
                {
                    distances.Add(0);
                    continue;
                }

                int minDistanceForPattern = int.MaxValue;
                if (normalizedInput.Length >= normalizedPattern.Length)
                {
                    for (int i = 0; i <= normalizedInput.Length - normalizedPattern.Length; i++)
                    {
                        string substring = normalizedInput.Substring(i, normalizedPattern.Length);
                        minDistanceForPattern = Math.Min(minDistanceForPattern, Levenshtein(substring, normalizedPattern));
                    }
                }
                else
                {
                    minDistanceForPattern = Levenshtein(normalizedInput, normalizedPattern.Substring(0, normalizedInput.Length));
                }
                distances.Add(minDistanceForPattern);
            }
            return (int)distances.OrderBy(x => x).Take(2).DefaultIfEmpty(int.MaxValue).Average();
        }

        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            input = Regex.Replace(input, "\\b\\d{1,2}/\\d{1,2}/\\d{4}\\b", "");
            var lower = input.ToLowerInvariant();
            var noAccents = string.Concat(lower.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
            return Regex.Replace(noAccents.Replace("ª", "a").Replace("º", "o"), @"[^\w\s]", "").Trim();
        }

        private static int Levenshtein(string s, string t)
        {
            if (s == t) return 0;
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;
            var d = new int[s.Length + 1, t.Length + 1];
            for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= t.Length; j++) d[0, j] = j;
            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[s.Length, t.Length];
        }
    }
}
