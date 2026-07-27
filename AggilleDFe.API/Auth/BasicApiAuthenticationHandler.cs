using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using AggilleDFe.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AggilleDFe.API.Auth;

public class BasicApiAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguracaoRepository configuracaoRepository)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var valoresHeader) ||
            !AuthenticationHeaderValue.TryParse(valoresHeader.ToString(), out var authHeader) ||
            !"Basic".Equals(authHeader.Scheme, StringComparison.OrdinalIgnoreCase) ||
            authHeader.Parameter is null)
        {
            return AuthenticateResult.Fail("Credenciais Basic ausentes.");
        }

        string usuario;
        string senha;
        try
        {
            var bytes = Convert.FromBase64String(authHeader.Parameter);
            var partes = Encoding.UTF8.GetString(bytes).Split(':', 2);
            if (partes.Length != 2)
            {
                return AuthenticateResult.Fail("Formato de credenciais inválido.");
            }

            usuario = partes[0];
            senha = partes[1];
        }
        catch (FormatException)
        {
            return AuthenticateResult.Fail("Formato de credenciais inválido.");
        }

        var configuracao = await configuracaoRepository.ObterAsync();
        if (configuracao is null || configuracao.ApiAtiva != "S")
        {
            return AuthenticateResult.Fail("API de integração desativada.");
        }

        if (configuracao.UsuarioApi != usuario || configuracao.SenhaApi != senha)
        {
            return AuthenticateResult.Fail("Usuário ou senha inválidos.");
        }

        var claims = new[] { new Claim(ClaimTypes.Name, usuario) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"AggilleDFe\"";
        return base.HandleChallengeAsync(properties);
    }
}
