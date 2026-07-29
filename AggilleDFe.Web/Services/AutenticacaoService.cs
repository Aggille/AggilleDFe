using System.Net.Http.Json;
using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Web.Services;

public class AutenticacaoService(HttpClient http, TokenAuthenticationStateProvider authStateProvider)
{
    public async Task<bool> EntrarAsync(string login, string senha)
    {
        var resposta = await http.PostAsJsonAsync("api/v1/auth/login", new LoginRequestDto { Login = login, Senha = senha });
        if (!resposta.IsSuccessStatusCode)
        {
            return false;
        }

        var resultado = await resposta.Content.ReadFromJsonAsync<LoginResponseDto>();
        if (resultado is null)
        {
            return false;
        }

        await authStateProvider.EntrarAsync(resultado.Token);
        return true;
    }

    public Task SairAsync() => authStateProvider.SairAsync();
}
