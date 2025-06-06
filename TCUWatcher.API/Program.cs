using Serilog;
using TCUWatcher.API.Authentication;
using TCUWatcher.API.Extensions;
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Application.Users;
using TCUWatcher.Domain.Repositories;
using TCUWatcher.Domain.Services;
using TCUWatcher.Infrastructure.SessionEvents;
using TCUWatcher.Infrastructure.Storage;
using TCUWatcher.Infrastructure.Users;

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
    builder.Services.AddSwaggerWithBearer(); // Swagger é útil em ambos os ambientes.
    builder.Services.AddAutoMapper(typeof(ISessionEventService).Assembly);

    // ============================================================================
    // AQUI COMEÇA A MÁGICA DA SEPARAÇÃO DE AMBIENTES
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
        builder.Services.AddMockAuthentication(); // Extensão que configura o handler mock.
    }
    else
    {
        // --- AMBIENTE DE PRODUÇÃO (OU QUALQUER OUTRO QUE NÃO SEJA DEV) ---
        Log.Warning("Ambiente de Produção ou Staging detectado. Configurando para serviços reais.");
        Log.Warning("ATENÇÃO: Implementações de produção ainda são necessárias.");

        // Deixamos os "slots" prontos, mas lançamos um erro se forem usados.
        // Isso evita que a aplicação rode em produção sem estar completa.
        builder.Services.AddSingleton<ICurrentUserProvider>(sp => 
            throw new NotImplementedException("ICurrentUserProvider de produção não implementado."));
            
        builder.Services.AddScoped<IUserService>(sp => 
            throw new NotImplementedException("IUserService de produção não implementado."));

        builder.Services.AddScoped<ISessionEventRepository>(sp => 
            throw new NotImplementedException("ISessionEventRepository de produção não implementado."));

        builder.Services.AddSingleton<IStorageService>(sp => 
            throw new NotImplementedException("IStorageService de produção não implementado."));

        // --- AUTENTICAÇÃO REAL (JWT) ---
        builder.Services.AddScoped<IAuthenticationService>(sp => 
            throw new NotImplementedException("IAuthenticationService de produção não implementado."));
        
        // Aqui entraria a configuração de autenticação JWT real.
        // Ex: builder.Services.AddJwtBearerAuthentication(options => { ... });
        // Por enquanto, se rodar em produção, qualquer endpoint protegido dará erro 500,
        // pois a autenticação não estará configurada.
    }

    // A lógica de negócio principal é registrada da mesma forma para ambos os ambientes,
    // pois ela depende das abstrações (interfaces), não das implementações.
    builder.Services.AddScoped<ISessionEventService, SessionEventService>();
    
    // ============================================================================
    // FIM DA SEPARAÇÃO DE AMBIENTES
    // ============================================================================

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // app.UseHttpsRedirection();
    
    // O middleware de autenticação e autorização é adicionado para ambos os ambientes.
    // O comportamento dele vai depender de como os serviços foram registrados acima.
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