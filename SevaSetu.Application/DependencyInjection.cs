using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SevaSetu.Application.Services;

namespace SevaSetu.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AuthService>();
        services.AddScoped<AuthService>();
        services.AddScoped<CareService>();
        return services;
    }
}
