using System.IO;
using Akwaba.Application.Interfaces;
using Akwaba.Domain.Interfaces;
using Akwaba.Infrastructure.Persistence;
using Akwaba.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Akwaba.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Convention LP2M : DATA_DIR pointe le dossier persistant (volume Docker).
        var dataDir = config["DATA_DIR"];
        var cs = config.GetConnectionString("Defaut")
                 ?? (string.IsNullOrWhiteSpace(dataDir)
                        ? "Data Source=akwaba.db"
                        : $"Data Source={Path.Combine(dataDir, "akwaba.db")}");

        services.AddDbContext<AkwabaDbContext>(opt => opt.UseSqlite(cs));
        services.AddScoped<IAkwabaDbContext>(sp => sp.GetRequiredService<AkwabaDbContext>());

        services.AddIdentity<AppliUtilisateur, AppliRole>(opt =>
        {
            opt.Password.RequiredLength = 8;
            opt.Password.RequireNonAlphanumeric = true;
            opt.User.RequireUniqueEmail = true;
            opt.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<AkwabaDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IServiceAudit, ServiceAudit>();
        services.AddScoped<ICinetPayService, CinetPayService>();

        return services;
    }
}
