using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AggilleDFe.Application.Services;

public class AutenticacaoService(IUsuarioRepository repository, IOptions<JwtOptions> jwtOptions) : IAutenticacaoService
{
    private static readonly PasswordHasher<Usuario> Hasher = new();

    public async Task<LoginResponseDto?> AutenticarAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await repository.ObterPorLoginAsync(dto.Login, cancellationToken);
        if (usuario is null || usuario.Inativo == "S")
        {
            return null;
        }

        var resultado = Hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dto.Senha);
        if (resultado == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var opcoes = jwtOptions.Value;
        var expiraEm = DateTime.UtcNow.AddMinutes(opcoes.ExpiraMinutos);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Login),
            new("administrador", usuario.Administrador == "S" ? "true" : "false")
        };

        AdicionarClaimPermissao(claims, "xmls-baixados", usuario.AcessoXmlsBaixados);
        AdicionarClaimPermissao(claims, "registros", usuario.AcessoRegistros);
        AdicionarClaimPermissao(claims, "empresas", usuario.AcessoEmpresas);
        AdicionarClaimPermissao(claims, "configuracao", usuario.AcessoConfiguracao);
        AdicionarClaimPermissao(claims, "importacao", usuario.AcessoImportacao);
        AdicionarClaimPermissao(claims, "baixar-xml", usuario.AcessoBaixarXml);

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcoes.Key));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiraEm,
            signingCredentials: credenciais);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponseDto
        {
            Token = tokenString,
            ExpiraEm = expiraEm,
            Login = usuario.Login,
            Nome = usuario.Nome,
            Administrador = usuario.Administrador == "S",
            AcessoXmlsBaixados = usuario.AcessoXmlsBaixados == "S",
            AcessoRegistros = usuario.AcessoRegistros == "S",
            AcessoEmpresas = usuario.AcessoEmpresas == "S",
            AcessoConfiguracao = usuario.AcessoConfiguracao == "S",
            AcessoImportacao = usuario.AcessoImportacao == "S",
            AcessoBaixarXml = usuario.AcessoBaixarXml == "S"
        };
    }

    private static void AdicionarClaimPermissao(List<Claim> claims, string valor, string? flag)
    {
        if (flag == "S")
        {
            claims.Add(new Claim("permissao", valor));
        }
    }
}
