using Akwaba.Sync;

namespace Akwaba.Desktop.Services;

/// <summary>Surveille la joignabilité de l'API et expose l'état en ligne / hors ligne.</summary>
public class ServiceConnectivite(SyncClient client) : IDisposable
{
    private readonly System.Timers.Timer _timer = new(15_000) { AutoReset = true };
    public bool EstEnLigne { get; private set; }
    public event Action<bool>? EtatChange;

    public void Demarrer()
    {
        _timer.Elapsed += async (_, _) => await VerifierAsync();
        _timer.Start();
        _ = VerifierAsync();
    }

    public async Task VerifierAsync()
    {
        var avant = EstEnLigne;
        EstEnLigne = await client.EstJoignableAsync();
        if (avant != EstEnLigne) EtatChange?.Invoke(EstEnLigne);
    }

    public void Dispose() => _timer.Dispose();
}
