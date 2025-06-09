#!/bin/bash

echo "🔧 Atualizando StringNormalizer.cs..."
cat > TCUWatcher.Infrastructure/Helpers/StringNormalizer.cs <<EOF
using System.Globalization;
using System.Linq;
using System.Text;

namespace TCUWatcher.Infrastructure.Helpers;

public static class StringNormalizer
{
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        string normalized = input.Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(chars).ToLowerInvariant();
    }
}
EOF

echo "🔧 Atualizando TitleValidationService.cs..."
cat > TCUWatcher.Infrastructure/Services/TitleValidationService.cs <<EOF
using FuzzySharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TCUWatcher.Infrastructure.Helpers;

namespace TCUWatcher.Infrastructure.Services;

public class TitleValidationService : ITitleValidationService
{
    private readonly List<string> _keywords;
    private readonly ILogger<TitleValidationService> _logger;

    public TitleValidationService(IConfiguration config, ILogger<TitleValidationService> logger)
    {
        _logger = logger;
        _keywords = config.GetSection("YouTube:TitleKeywords").Get<List<string>>() ?? new();
    }

    public bool IsRelevant(string title)
    {
        var normalizedTitle = StringNormalizer.Normalize(title);

        foreach (var keyword in _keywords)
        {
            var normalizedKeyword = StringNormalizer.Normalize(keyword);
            var score = Fuzz.Ratio(normalizedTitle, normalizedKeyword);

            if (score >= 75)
            {
                _logger.LogInformation($"Título '{title}' aceito (score {score} com keyword '{keyword}')");
                return true;
            }
        }

        _logger.LogInformation($"Título '{title}' rejeitado");
        return false;
    }
}
EOF

echo "🔧 Atualizando TitleValidationServiceTests.cs..."
cat > TCUWatcher.Tests/Infraestructure/TitleValidationServiceTests.cs <<EOF
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TCUWatcher.Infrastructure.Services;
using Xunit;

namespace TCUWatcher.Tests.Infrastructure;

public class TitleValidationServiceTests
{
    private readonly TitleValidationService _service;

    public TitleValidationServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"YouTube:TitleKeywords:0", "câmara"},
            {"YouTube:TitleKeywords:1", "camara"},
            {"YouTube:TitleKeywords:2", "plenário"},
            {"YouTube:TitleKeywords:3", "plenario"},
            {"YouTube:TitleKeywords:4", "tcu"},
            {"YouTube:TitleKeywords:5", "sessão"},
            {"YouTube:TitleKeywords:6", "sessao"},
            {"YouTube:TitleKeywords:7", "tribunal de contas"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var logger = Mock.Of<ILogger<TitleValidationService>>();
        _service = new TitleValidationService(configuration, logger);
    }

    [Theory]
    [InlineData("Plenário do Tribunal", true)]
    [InlineData("Plenario de julgamentos", true)]
    [InlineData("1a Camara de julgamento", true)]
    [InlineData("Sessão do TCU ao vivo", true)]
    [InlineData("Live aleatória qualquer", false)]
    [InlineData("Show de Rock em Brasília", false)]
    public void IsRelevant_ShouldValidateVariousTitles(string title, bool expected)
    {
        var result = _service.IsRelevant(title);
        Assert.Equal(expected, result);
    }
}
EOF

echo "🚀 Rodando testes..."
dotnet build && dotnet test
