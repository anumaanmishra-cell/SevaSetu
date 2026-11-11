using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevaSetu.Application;
using SevaSetu.Application.Common;
using SevaSetu.Infrastructure.Persistence;
using SevaSetu.Infrastructure.Workers;

namespace SevaSetu.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o =>
        {
            o.UseSqlServer(config.GetConnectionString("DefaultConnection"), sql =>
                sql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(30), null));
            o.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequireNonAlphanumeric = false;
                o.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
        services.AddApplication();
        services.AddHttpClient();
        services.AddHostedService<VisitReminderWorker>();
        return services;
    }
}
