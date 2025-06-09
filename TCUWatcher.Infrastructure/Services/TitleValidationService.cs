using System; 
using System.Collections.Generic; 
using System.Linq; 
using Microsoft.Extensions.Configuration; 
using Microsoft.Extensions.Logging;
using TCUWatcher.Infrastructure.Helpers;
using TCUWatcher.Domain.Services;


namespace TCUWatcher.Infrastructure.Services { 
    public class TitleValidationService : ITitleValidationService{ 
        private readonly FuzzyMatcherService _matcher; 
        private readonly ILogger _logger; 
        private readonly string[] _relevancePatterns; 
        private readonly string[] _keywords; 
        private readonly string[] _primaryKeywords; 
        private readonly string[] _secondaryKeywords; 
        private readonly string[] _negativeKeywords;

    public TitleValidationService(IConfiguration config, ILogger<TitleValidationService> logger, FuzzyMatcherService matcher)
    {
        _logger = logger;


        _matcher = matcher;

        // Load configuration from appsettings.json
        var titleValidationSection = config.GetSection("TitleValidation");
        _relevancePatterns = titleValidationSection.GetSection("RelevancePatterns").Get<string[]>() ?? Array.Empty<string>();
        _keywords = titleValidationSection.GetSection("Keywords").Get<string[]>() ?? Array.Empty<string>();
        _primaryKeywords = titleValidationSection.GetSection("PrimaryKeywords").Get<string[]>() ?? Array.Empty<string>();
        _secondaryKeywords = titleValidationSection.GetSection("SecondaryKeywords").Get<string[]>() ?? Array.Empty<string>();
        _negativeKeywords = titleValidationSection.GetSection("NegativeKeywords").Get<string[]>() ?? Array.Empty<string>();

        // Validate configuration
        if (!_relevancePatterns.Any() || !_keywords.Any() || !_primaryKeywords.Any() || !_secondaryKeywords.Any() || !_negativeKeywords.Any())
        {
            _logger.LogWarning("One or more configuration arrays are empty. Title validation may not work as expected.");
        }
    }

    public bool IsRelevant(string title)
    {
        var normalized = FuzzyMatcherService.Normalize(title);
        if (_negativeKeywords.Any(k => normalized.Contains(FuzzyMatcherService.Normalize(k))))
        {
            _logger.LogDebug("Rejected by negative keyword: {title}", title);
            return false;
        }

        var normalizedWords = new HashSet<string>(normalized.Split(' '));
        _logger.LogDebug("Normalized: {normalized}", normalized);

        bool hasPrimary = false, hasSecondary = false;
        foreach (var word in normalizedWords)
        {
            if (_primaryKeywords.Any(k => Levenshtein(word, FuzzyMatcherService.Normalize(k)) <= 1))
                hasPrimary = true;
            if (_secondaryKeywords.Any(k => Levenshtein(word, FuzzyMatcherService.Normalize(k)) <= 1))
                hasSecondary = true;
            if (hasPrimary && hasSecondary)
            {
                _logger.LogDebug("Match by primary+secondary keywords: {title}", title);
                return true;
            }
        }

        int keywordMatches = 0;
        foreach (var keyword in _keywords)
        {
            var normalizedKeyword = FuzzyMatcherService.Normalize(keyword);
            if (normalizedWords.Any(word => Levenshtein(word, normalizedKeyword) <= 1))
            {
                keywordMatches++;
                _logger.LogDebug("Keyword match: {keyword} in {title}", keyword, title);
            }
        }

        if (keywordMatches >= 2)
        {
            _logger.LogDebug("Match por palavras-chave: {title} (matched {keywordMatches} keywords)", title, keywordMatches);
            return true;
        }

        bool fuzzyMatch = _matcher.Matches(title, _relevancePatterns);
        if (fuzzyMatch)
            _logger.LogDebug("Match fuzzy: {title}", title);
        else
            _logger.LogDebug("No fuzzy match for {title}", title);

        return fuzzyMatch;
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