using System;
using System.Threading.Tasks;
using TCUWatcher.Infrastructure.Monitoring;
using Xunit;

namespace TCUWatcher.Tests;

public class MonitoringWindowServiceTests
{
    private readonly MockMonitoringWindowRepository _mockRepo;
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly MonitoringWindowService _sut; // System Under Test

    public MonitoringWindowServiceTests()
    {
        _mockRepo = new MockMonitoringWindowRepository();
        _fakeTimeProvider = new FakeTimeProvider(); // Usando o nosso relógio falso!
        _sut = new MonitoringWindowService(_mockRepo, _fakeTimeProvider);
    }

    [Fact]
    public async Task IsCurrentlyInActiveWindow_ShouldReturnTrue_WhenTimeIsInWindow()
    {
        // Arrange: Configure o relógio para uma terça-feira às 15:00 UTC
        var tuesdayAfternoon = new DateTimeOffset(2025, 6, 10, 15, 0, 0, TimeSpan.Zero);
        _fakeTimeProvider.SetUtcNow(tuesdayAfternoon);

        // Act: Pergunte ao "porteiro"
        var result = await _sut.IsCurrentlyInActiveWindowAsync();

        // Assert: A resposta deve ser 'true'
        Assert.True(result);
    }

    [Fact]
    public async Task IsCurrentlyInActiveWindow_ShouldReturnFalse_WhenTimeIsOutsideWindow()
    {
        // Arrange: Configure o relógio para uma terça-feira às 20:00 UTC (fora da janela)
        var tuesdayNight = new DateTimeOffset(2025, 6, 10, 20, 0, 0, TimeSpan.Zero);
        _fakeTimeProvider.SetUtcNow(tuesdayNight);

        // Act
        var result = await _sut.IsCurrentlyInActiveWindowAsync();

        // Assert: A resposta deve ser 'false'
        Assert.False(result);
    }

    [Fact]
    public async Task IsCurrentlyInActiveWindow_ShouldReturnFalse_WhenOnWrongDay()
    {
        // Arrange: Configure o relógio para uma quarta-feira às 15:00 UTC
        var wednesdayAfternoon = new DateTimeOffset(2025, 6, 11, 15, 0, 0, TimeSpan.Zero);
        _fakeTimeProvider.SetUtcNow(wednesdayAfternoon);

        // Act
        var result = await _sut.IsCurrentlyInActiveWindowAsync();

        // Assert: A resposta deve ser 'false'
        Assert.False(result);
    }
}