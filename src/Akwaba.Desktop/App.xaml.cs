using System.IO;
using System.Net.Http;
using System.Windows;
using Akwaba.Desktop.Local;
using Akwaba.Desktop.Services;
using Akwaba.Desktop.ViewModels;
using Akwaba.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Akwaba.Desktop;

public partial class App : Application
{
    private IHost? _host;

    public const string UrlApiDefaut = "https://lp2medoune.com/gestionhotel/";

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dossier = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AkwabaDesktop");
        Directory.CreateDirectory(dossier);
        var cheminBase = Path.Combine(dossier, "akwaba-local.db");

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddDbContext<ContexteLocal>(o => o.UseSqlite($"Data Source={cheminBase}"),
                    ServiceLifetime.Singleton);

                services.AddHttpClient("akwaba", c => c.BaseAddress = new Uri(UrlApiDefaut));
                services.AddSingleton<SyncClient>(sp =>
                    new SyncClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("akwaba")));

                services.AddSingleton<SessionPoste>();
                services.AddSingleton<DepotLocal>();
                services.AddSingleton<ServiceAuth>();
                services.AddSingleton<ServiceSync>();
                services.AddSingleton<ServiceConnectivite>();
                services.AddSingleton<ServiceReceptionLocale>();
                services.AddSingleton<ServiceCaisseLocale>();
                services.AddSingleton<TableauBordViewModel>();
                services.AddSingleton<ReservationsViewModel>();
                services.AddSingleton<DetailSejourViewModel>();
                services.AddSingleton<CaisseViewModel>();
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        // Création de la base locale au premier lancement
        using (var scope = _host.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ContexteLocal>().Database.EnsureCreatedAsync();

        _host.Services.GetRequiredService<ServiceConnectivite>().Demarrer();
        _host.Services.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}
