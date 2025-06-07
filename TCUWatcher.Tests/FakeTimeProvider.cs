using System;

// O namespace deve ser o do projeto de testes
namespace TCUWatcher.Tests;

public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _currentTime;

    public FakeTimeProvider(DateTimeOffset? initialTime = null)
    {
        _currentTime = initialTime ?? DateTimeOffset.UtcNow;
    }

    public override DateTimeOffset GetUtcNow() => _currentTime;

    public void SetUtcNow(DateTimeOffset time)
    {
        _currentTime = time;
    }

    public void Advance(TimeSpan delta)
    {
        _currentTime += delta;
    }
}