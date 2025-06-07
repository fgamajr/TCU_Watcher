using System;

// TimeProvider é uma classe base que vive no namespace System.
// Nós vamos criar a nossa própria implementação para testes.
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _currentTime;

    public FakeTimeProvider(DateTimeOffset? initialTime = null)
    {
        _currentTime = initialTime ?? DateTimeOffset.UtcNow;
    }

    // Este é o método que nosso código de produção chama.
    // Ele vai retornar qualquer tempo que a gente configurar.
    public override DateTimeOffset GetUtcNow() => _currentTime;

    // Este é o nosso "controle remoto" para o tempo.
    // Usaremos este método nos testes para avançar ou retroceder o relógio.
    public void SetUtcNow(DateTimeOffset time)
    {
        _currentTime = time;
    }

    // (Opcional) Um método útil para avançar o tempo.
    public void Advance(TimeSpan delta)
    {
        _currentTime += delta;
    }
}