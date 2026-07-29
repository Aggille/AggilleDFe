using System.Net.Http.Headers;

namespace AggilleDFe.Web.Services;

/// <summary>
/// Injeta o token JWT em toda chamada à API. A API ainda não exige esse
/// token nos endpoints internos (só o front-end bloqueia hoje), mas deixa a
/// estrutura pronta pra quando isso for ligado no futuro.
/// </summary>
public class TokenAuthorizationHandler(TokenAuthenticationStateProvider authStateProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await authStateProvider.GetAuthenticationStateAsync();
        var token = authStateProvider.Token;
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
