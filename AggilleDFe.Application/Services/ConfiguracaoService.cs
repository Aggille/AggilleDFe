using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Application.Validation;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;

namespace AggilleDFe.Application.Services;

public class ConfiguracaoService(IConfiguracaoRepository repository) : IConfiguracaoService
{
    public async Task<ConfiguracaoDto?> ObterAsync(CancellationToken cancellationToken = default)
    {
        var configuracao = await repository.ObterAsync(cancellationToken);
        return configuracao is null ? null : ParaDto(configuracao);
    }

    public async Task<IReadOnlyDictionary<string, string[]>?> SalvarAsync(ConfiguracaoDto dto, CancellationToken cancellationToken = default)
    {
        var erros = Validar(dto);
        if (erros.Count > 0)
        {
            return erros;
        }

        var configuracao = await repository.ObterAsync(cancellationToken) ?? new Configuracao();

        configuracao.CnpjEmpresa = dto.CnpjEmpresa?.ToUpperInvariant();
        configuracao.NomeEmpresa = dto.NomeEmpresa;
        configuracao.TempoExecucao = dto.TempoExecucao;
        configuracao.PortaApi = dto.PortaApi;
        configuracao.UsuarioApi = dto.UsuarioApi;
        configuracao.SenhaApi = dto.SenhaApi;
        configuracao.ApiAtiva = dto.ApiAtiva ? "S" : "N";
        configuracao.ProcessarIndividualmente = dto.ProcessarIndividualmente ? "S" : "N";

        await repository.SalvarAsync(configuracao, cancellationToken);
        return null;
    }

    private static ConfiguracaoDto ParaDto(Configuracao configuracao) => new()
    {
        Id = configuracao.Id,
        CnpjEmpresa = configuracao.CnpjEmpresa,
        NomeEmpresa = configuracao.NomeEmpresa,
        TempoExecucao = configuracao.TempoExecucao,
        PortaApi = configuracao.PortaApi,
        UsuarioApi = configuracao.UsuarioApi,
        SenhaApi = configuracao.SenhaApi,
        ApiAtiva = configuracao.ApiAtiva == "S",
        ProcessarIndividualmente = configuracao.ProcessarIndividualmente == "S"
    };

    private static Dictionary<string, string[]> Validar(ConfiguracaoDto dto)
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

        if (string.IsNullOrWhiteSpace(dto.NomeEmpresa))
        {
            AdicionarErro(nameof(dto.NomeEmpresa), "Razão Social é obrigatória.");
        }
        else if (dto.NomeEmpresa.Length > 60)
        {
            AdicionarErro(nameof(dto.NomeEmpresa), "Razão Social deve ter no máximo 60 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(dto.CnpjEmpresa))
        {
            AdicionarErro(nameof(dto.CnpjEmpresa), "C.N.P.J. é obrigatório.");
        }
        else if (!CnpjValidator.FormatoValido(dto.CnpjEmpresa))
        {
            AdicionarErro(nameof(dto.CnpjEmpresa), "C.N.P.J. deve conter 14 caracteres alfanuméricos, sem máscara.");
        }
        else if (!CnpjValidator.DigitosVerificadoresValidos(dto.CnpjEmpresa.ToUpperInvariant()))
        {
            AdicionarErro(nameof(dto.CnpjEmpresa), "C.N.P.J. inválido (dígitos verificadores não conferem).");
        }

        if (dto.TempoExecucao is null or <= 0)
        {
            AdicionarErro(nameof(dto.TempoExecucao), "Tempo de Execução deve ser maior que zero.");
        }

        if (dto.PortaApi is null or < 1 or > 65535)
        {
            AdicionarErro(nameof(dto.PortaApi), "Porta da API deve estar entre 1 e 65535.");
        }

        if (dto.ApiAtiva)
        {
            if (string.IsNullOrWhiteSpace(dto.UsuarioApi))
            {
                AdicionarErro(nameof(dto.UsuarioApi), "Usuário da API é obrigatório quando a API está ativa.");
            }
            else if (dto.UsuarioApi.Length > 50)
            {
                AdicionarErro(nameof(dto.UsuarioApi), "Usuário da API deve ter no máximo 50 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(dto.SenhaApi))
            {
                AdicionarErro(nameof(dto.SenhaApi), "Senha da API é obrigatória quando a API está ativa.");
            }
            else if (dto.SenhaApi.Length > 20)
            {
                AdicionarErro(nameof(dto.SenhaApi), "Senha da API deve ter no máximo 20 caracteres.");
            }
        }

        return erros.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
    }
}
