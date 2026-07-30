using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AggilleDFe.Application.Services;

public class UsuarioService(IUsuarioRepository repository) : IUsuarioService
{
    private static readonly PasswordHasher<Usuario> Hasher = new();

    public async Task<IReadOnlyList<UsuarioDto>> PesquisarAsync(string? busca, CancellationToken cancellationToken = default)
    {
        var usuarios = await repository.PesquisarAsync(busca, cancellationToken);
        return usuarios.Select(ParaDto).ToList();
    }

    public async Task<UsuarioDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await repository.ObterPorIdAsync(id, cancellationToken);
        return usuario is null ? null : ParaDto(usuario);
    }

    public async Task<(int? Id, IReadOnlyDictionary<string, string[]>? Erros)> IncluirAsync(UsuarioDto dto, CancellationToken cancellationToken = default)
    {
        var erros = await ValidarAsync(dto, idExcluir: null, exigirSenha: true, cancellationToken);
        if (erros.Count > 0)
        {
            return (null, erros);
        }

        var usuario = new Usuario();
        AplicarDto(usuario, dto);
        usuario.SenhaHash = Hasher.HashPassword(usuario, dto.Senha!);

        await repository.IncluirAsync(usuario, cancellationToken);
        return (usuario.Id, null);
    }

    public async Task<(bool Encontrado, IReadOnlyDictionary<string, string[]>? Erros)> AtualizarAsync(int id, UsuarioDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await repository.ObterPorIdAsync(id, cancellationToken);
        if (usuario is null)
        {
            return (false, null);
        }

        var erros = await ValidarAsync(dto, idExcluir: id, exigirSenha: false, cancellationToken);
        if (erros.Count > 0)
        {
            return (true, erros);
        }

        AplicarDto(usuario, dto);
        if (!string.IsNullOrWhiteSpace(dto.Senha))
        {
            usuario.SenhaHash = Hasher.HashPassword(usuario, dto.Senha);
        }

        await repository.AtualizarAsync(usuario, cancellationToken);
        return (true, null);
    }

    public async Task<bool> ExcluirAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await repository.ObterPorIdAsync(id, cancellationToken);
        if (usuario is null)
        {
            return false;
        }

        await repository.ExcluirAsync(usuario, cancellationToken);
        return true;
    }

    private static void AplicarDto(Usuario usuario, UsuarioDto dto)
    {
        usuario.Login = dto.Login;
        usuario.Nome = dto.Nome;
        usuario.Administrador = dto.Administrador ? "S" : "N";
        usuario.AcessoXmlsBaixados = dto.AcessoXmlsBaixados ? "S" : "N";
        usuario.AcessoRegistros = dto.AcessoRegistros ? "S" : "N";
        usuario.AcessoEmpresas = dto.AcessoEmpresas ? "S" : "N";
        usuario.AcessoConfiguracao = dto.AcessoConfiguracao ? "S" : "N";
        usuario.AcessoImportacao = dto.AcessoImportacao ? "S" : "N";
        usuario.AcessoBaixarXml = dto.AcessoBaixarXml ? "S" : "N";
        usuario.AcessoExportarXmls = dto.AcessoExportarXmls ? "S" : "N";
        usuario.AcessoBaixarPorChave = dto.AcessoBaixarPorChave ? "S" : "N";
        usuario.Inativo = dto.Inativo ? "S" : "N";
    }

    private static UsuarioDto ParaDto(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Login = usuario.Login,
        Nome = usuario.Nome,
        Administrador = usuario.Administrador == "S",
        AcessoXmlsBaixados = usuario.AcessoXmlsBaixados == "S",
        AcessoRegistros = usuario.AcessoRegistros == "S",
        AcessoEmpresas = usuario.AcessoEmpresas == "S",
        AcessoConfiguracao = usuario.AcessoConfiguracao == "S",
        AcessoImportacao = usuario.AcessoImportacao == "S",
        AcessoBaixarXml = usuario.AcessoBaixarXml == "S",
        AcessoExportarXmls = usuario.AcessoExportarXmls == "S",
        AcessoBaixarPorChave = usuario.AcessoBaixarPorChave == "S",
        Inativo = usuario.Inativo == "S"
    };

    private async Task<Dictionary<string, string[]>> ValidarAsync(UsuarioDto dto, int? idExcluir, bool exigirSenha, CancellationToken cancellationToken)
    {
        var erros = new Dictionary<string, List<string>>();

        void AdicionarErro(string campo, string mensagem)
        {
            if (!erros.TryGetValue(campo, out var lista))
            {
                lista = [];
                erros[campo] = lista;
            }

            lista.Add(mensagem);
        }

        if (string.IsNullOrWhiteSpace(dto.Login))
        {
            AdicionarErro(nameof(dto.Login), "Login é obrigatório.");
        }
        else if (dto.Login.Length > 50)
        {
            AdicionarErro(nameof(dto.Login), "Login deve ter no máximo 50 caracteres.");
        }
        else if (await repository.ExisteComLoginAsync(dto.Login, idExcluir, cancellationToken))
        {
            AdicionarErro(nameof(dto.Login), "Já existe um usuário cadastrado com esse login.");
        }

        if (exigirSenha && string.IsNullOrWhiteSpace(dto.Senha))
        {
            AdicionarErro(nameof(dto.Senha), "Senha é obrigatória.");
        }

        return erros.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
    }
}
