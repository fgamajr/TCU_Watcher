using Serilog;
using TCUWatcher.API.Authentication;
using TCUWatcher.API.Extensions;
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Application.Users;
using TCUWatcher.Domain.Repositories;
using TCUWatcher.Domain.Services;
using TCUWatcher.Infrastructure.Monitoring; // Adicionado para os novos serviços
using TCUWatcher.Infrastructure.SessionEvents;
using TCUWatcher.Infrastructure.Storage;
using TCUWatcher.Infrastructure.Users;
// using TCUWatcher.Infrastructure.Workers; // Adicionar quando o Worker for criado

// A configuração do Serilog continua a mesma, fora de qualquer ambiente.
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/tcuwatcher-.log", rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Iniciando a aplicação TCUWatcher.API...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    builder.Services.AddControllers();
    builder.Services.AddSwaggerWithBearer(); 
    builder.Services.AddAutoMapper(typeof(ISessionEventService).Assembly);

    // ============================================================================
    // REGISTRO DE SERVIÇOS DA APLICAÇÃO
    // ============================================================================
    
    // Serviço principal de negócio
    builder.Services.AddScoped<ISessionEventService, SessionEventService>();
    
    // Adicionando os novos serviços de monitoramento
    builder.Services.AddSingleton(TimeProvider.System); // Provedor de tempo para lógica testável
    builder.Services.AddScoped<IMonitoringWindowRepository, MockMonitoringWindowRepository>();
    builder.Services.AddScoped<IMonitoringWindowService, MonitoringWindowService>();

    // Adicionar o Worker quando ele for criado e implementado
    // builder.Services.AddHostedService<VideoDiscoveryWorker>();


    // ============================================================================
    // SEPARAÇÃO DE AMBIENTES (MOCKS VS REAL)
    // ============================================================================
    if (builder.Environment.IsDevelopment())
    {
        Log.Information("Ambiente de Desenvolvimento detectado. Usando serviços MOCK.");

        // --- SERVIÇOS DE DESENVOLVIMENTO (MOCKS) ---
        builder.Services.AddSingleton<ICurrentUserProvider, MockUserProvider>();
        builder.Services.AddScoped<IUserService, MockUserService>();
        builder.Services.AddScoped<ISessionEventRepository, MockSessionEventRepository>();
        builder.Services.AddSingleton<IStorageService, MockStorageService>();
        
        // --- AUTENTICAÇÃO MOCK ---
        builder.Services.AddScoped<IAuthenticationService, MockAuthenticationService>();
        builder.Services.AddMockAuthentication();
    }
    else
    {
        // --- AMBIENTE DE PRODUÇÃO ---
        Log.Warning("Ambiente de Produção ou Staging detectado. Configurando para serviços reais.");
        // ... (lógica de produção com NotImplementedException mantida) ...
    }

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // app.UseHttpsRedirection(); // Mantido comentado para facilitar testes locais
    
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação falhou ao iniciar");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;