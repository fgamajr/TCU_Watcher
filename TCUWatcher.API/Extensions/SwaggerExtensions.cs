using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;

namespace TCUWatcher.API.Extensions
{
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Configura o Swagger para suportar Bearer token (JWT) nos endpoints protegidos,
        /// usando o esquema HTTP (tipo bearer). Assim, basta fornecer o token sem digitar "Bearer ".
        /// </summary>
        public static IServiceCollection AddSwaggerWithBearer(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "TCUWatcher.API",
                    Version = "v1"
                });

                // Agora definimos um esquema de segurança do tipo HTTP Bearer:
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Informe o token JWT aqui (sem o prefixo 'Bearer ').\n" +
                                  "O Swagger irá adicionar automaticamente 'Bearer ' antes do valor.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,         // <<-- alterado para Http
                    Scheme = "bearer",                      // <<-- informamos "bearer" (minúsculo)
                    BearerFormat = "JWT"                    // (opcional: só para documentação)
                });

                // Ainda precisamos exigir o uso desse esquema nos endpoints que tenham [Authorize]:
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "bearer",
                            Name = "Bearer",
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });
            });

            return services;
        }
    }
}
