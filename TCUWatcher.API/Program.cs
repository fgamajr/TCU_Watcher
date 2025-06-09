using Serilog;
using TCUWatcher.API.Authentication;
using TCUWatcher.API.Extensions;
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Application.Users;
using TCUWatcher.Application.Monitoring;
using TCUWatcher.Domain.Repositories;
using TCUWatcher.Domain.Services;
using TCUWatcher.Infrastructure.BackgroundServices;
using TCUWatcher.Infrastructure.Monitoring;
using TCUWatcher.Infrastructure.SessionEvents;
using TCUWatcher.Infrastructure.Storage;
using TCUWatcher.Infrastructure.Users;
using TCUWatcher.Infrastructure.Video;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TCUWatcher.Infrastructure.Helpers;
using TCUWatcher.Infrastructure.Services;


Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(),
        "logs/tcuwatcher-structured.json",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Iniciando a aplicação TCUWatcher.API...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddSwaggerWithBearer();
    builder.Services.AddAutoMapper(typeof(ISessionEventService).Assembly);

    if (builder.Environment.IsDevelopment())
    {
        Log.Information("Ambiente de Desenvolvimento detectado. Usando serviços MOCK.");

        builder.Services.AddSingleton<ICurrentUserProvider, MockUserProvider>();
        builder.Services.AddScoped<IUserService, MockUserService>();
        builder.Services.AddScoped<ISessionEventRepository, MockSessionEventRepository>();
        builder.Services.AddSingleton<IStorageService, MockStorageService>();
        builder.Services.AddScoped<IMonitoringWindowRepository, MockMonitoringWindowRepository>();
        builder.Services.AddScoped<IVideoDiscoveryService, MockVideoDiscoveryService>();
        builder.Services.AddScoped<ISessionSyncService, MockSessionSyncService>();// ✅ Adicionado mock
        builder.Services.AddScoped<IAuthenticationService, MockAuthenticationService>();
        builder.Services.AddMockAuthentication();
    }
    else
    {
        Log.Warning("Ambiente de Produção ou Staging detectado. Configurando para serviços reais.");
        Log.Warning("ATENÇÃO: Implementações de produção ainda são necessárias.");

        builder.Services.AddSingleton<ICurrentUserProvider>(sp =>
            throw new NotImplementedException("ICurrentUserProvider de produção não implementado."));
        builder.Services.AddScoped<IUserService>(sp =>
            throw new NotImplementedException("IUserService de produção não implementado."));
        builder.Services.AddScoped<ISessionEventRepository>(sp =>
            throw new NotImplementedException("ISessionEventRepository de produção não implementado."));
        builder.Services.AddSingleton<IStorageService>(sp =>
            throw new NotImplementedException("IStorageService de produção não implementado."));
        builder.Services.AddScoped<IAuthenticationService>(sp =>
            throw new NotImplementedException("IAuthenticationService de produção não implementado."));
        builder.Services.AddScoped<IVideoDiscoveryService>(sp =>
            throw new NotImplementedException("IVideoDiscoveryService de produção não implementado."));
        builder.Services.AddScoped<ISessionSyncService>(sp =>
            throw new NotImplementedException("ISessionSyncService de produção não implementado."));
    }

    // Serviços compartilhados
    builder.Services.AddScoped<IMonitoringWindowService, MonitoringWindowService>();
    builder.Services.AddScoped<ILiveDetectionService, LiveDetectionService>();
    builder.Services.AddScoped<ISessionEventService, SessionEventService>();

    // Serviço de validação de título
    builder.Services.AddScoped<FuzzyMatcherService>();
    builder.Services.AddScoped<ITitleValidationService, TitleValidationService>();


    // Serviço de background (orquestrador)
    builder.Services.AddHostedService<SyncService>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

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
