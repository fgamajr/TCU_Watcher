using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace TCUWatcher.API.Authentication
{
    public static class MockAuthenticationExtensions
    {
        public static IServiceCollection AddMockAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication("Bearer")
                    .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>(
                        "Bearer", options => { });

            services.AddAuthorization();
            return services;
        }
    }
}
