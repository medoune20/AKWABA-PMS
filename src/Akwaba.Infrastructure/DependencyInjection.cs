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
        var cs = config.GetConnectionString("Defaut")
                 ?? "Data Source=akwaba.db";

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
