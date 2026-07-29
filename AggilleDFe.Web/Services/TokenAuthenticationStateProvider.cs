using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace AggilleDFe.Web.Services;

public class TokenAuthenticationStateProvider(IJSRuntime jsRuntime) : AuthenticationStateProvider
{
    private const string StorageKey = "aggilledfe-token";
    private static readonly AuthenticationState EstadoAnonimo = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private string? _token;

    public string? Token => _token;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        _token ??= await jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);

        if (string.IsNullOrWhiteSpace(_token))
        {
            return EstadoAnonimo;
        }

        var claims = LerClaims(_token);
        if (claims is null)
        {
            await SairAsync();
            return EstadoAnonimo;
        }

        var identidade = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identidade));
    }

    public async Task EntrarAsync(string token)
    {
        _token = token;
        await jsRuntime.InvokeVoidAsync("sessionStorage.setItem", StorageKey, token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SairAsync()
    {
        _token = null;
        await jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
        NotifyAuthenticationStateChanged(Task.FromResult(EstadoAnonimo));
    }

    private static List<Claim>? LerClaims(string token)
    {
        try
        {
            var partes = token.Split('.');
            if (partes.Length != 3)
            {
                return null;
            }

            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(partes[1]));
            using var documento = JsonDocument.Parse(payloadJson);

            var claims = new List<Claim>();
            foreach (var propriedade in documento.RootElement.EnumerateObject())
            {
                if (propriedade.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in propriedade.Value.EnumerateArray())
                    {
                        claims.Add(new Claim(propriedade.Name, item.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(propriedade.Name, propriedade.Value.ToString()));
                }
            }

            var expUnix = documento.RootElement.TryGetProperty("exp", out var expElement) ? expElement.GetInt64() : 0;
            var expiraEm = DateTimeOffset.FromUnixTimeSeconds(expUnix);
            if (expiraEm <= DateTimeOffset.UtcNow)
            {
                return null;
            }

            return claims;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var texto = input.Replace('-', '+').Replace('_', '/');
        switch (texto.Length % 4)
        {
            case 2: texto += "=="; break;
            case 3: texto += "="; break;
        }

        return Convert.FromBase64String(texto);
    }
}
