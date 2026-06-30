using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Akwaba.Sync;

/// <summary>
/// Client HTTP de synchronisation, utilisé par le poste bureau.
/// Encapsule l'authentification (JWT) et les échanges push/pull avec l'API web.
/// </summary>
public class SyncClient(HttpClient http)
{
    private string? _token;

    public void DefinirJeton(string token)
    {
        _token = token;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public bool EstAuthentifie => !string.IsNullOrEmpty(_token);

    public async Task<LoginResponse?> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var rep = await http.PostAsJsonAsync("api/auth/login", req, ct);
        if (!rep.IsSuccessStatusCode) return null;
        var res = await rep.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
        if (res is not null) DefinirJeton(res.AccessToken);
        return res;
    }

    public async Task<PushResponse?> PushAsync(PushRequest req, CancellationToken ct = default)
    {
        var rep = await http.PostAsJsonAsync("api/sync/push", req, ct);
        rep.EnsureSuccessStatusCode();
        return await rep.Content.ReadFromJsonAsync<PushResponse>(cancellationToken: ct);
    }

    public async Task<PullResponse?> PullAsync(DateTime depuisUtc, CancellationToken ct = default)
    {
        var url = $"api/sync/pull?depuis={Uri.EscapeDataString(depuisUtc.ToString("O"))}";
        var rep = await http.GetAsync(url, ct);
        rep.EnsureSuccessStatusCode();
        return await rep.Content.ReadFromJsonAsync<PullResponse>(cancellationToken: ct);
    }

    /// <summary>Teste rapidement la connectivité de l'API (ping non authentifié).</summary>
    public async Task<bool> EstJoignableAsync(CancellationToken ct = default)
    {
        try { var r = await http.GetAsync("api/sync/ping", ct); return r.IsSuccessStatusCode; }
        catch { return false; }
    }
}
