using TCUWatcher.Application.Users;
using TCUWatcher.API.Authentication;      // para MockAuthenticationHandler e AddMockAuthentication
using TCUWatcher.Infrastructure.Users;
using TCUWatcher.API.Extensions;          // para AddSwaggerWithBearer
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Domain.Repositories;
using TCUWatcher.Infrastructure.SessionEvents;

var builder = WebApplication.CreateBuilder(args);

// 1) Serviços fundamentais
builder.Services.AddControllers();


// 2) Registrar mocks
builder.Services.AddSingleton<ISessionEventRepository, MockSessionEventRepository>();
builder.Services.AddScoped<ISessionEventService, SessionEventService>();
builder.Services.AddSingleton<ICurrentUserProvider, MockUserProvider>();
builder.Services.AddScoped<IUserService, MockUserService>();
builder.Services.AddScoped<IAuthenticationService, MockAuthenticationService>();


// 3) Registrar esquema “Bearer mock”
builder.Services.AddMockAuthentication();

// 4) Configurar Swagger/OpenAPI com Bearer
builder.Services.AddSwaggerWithBearer();

var app = builder.Build();

// 5) Middleware de Swagger (apenas em Dev)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 6) Ativar autenticação/ autorização
app.UseAuthentication();
app.UseAuthorization();

// 7) Mapear controllers
app.MapControllers();

app.Run();
