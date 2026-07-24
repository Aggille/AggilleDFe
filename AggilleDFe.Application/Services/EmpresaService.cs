using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Application.Validation;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;

namespace AggilleDFe.Application.Services;

public class EmpresaService(IEmpresaRepository repository) : IEmpresaService
{
    private static readonly HashSet<string> UfsValidas =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG",
        "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    ];

    public async Task<IReadOnlyList<EmpresaDto>> PesquisarAsync(string? busca, CancellationToken cancellationToken = default)
    {
        var empresas = await repository.PesquisarAsync(busca, cancellationToken);
        return empresas.Select(ParaDto).ToList();
    }

    public async Task<EmpresaDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var empresa = await repository.ObterPorIdAsync(id, cancellationToken);
        return empresa is null ? null : ParaDto(empresa);
    }

    public async Task<(int? Id, IReadOnlyDictionary<string, string[]>? Erros)> IncluirAsync(EmpresaDto dto, CancellationToken cancellationToken = default)
    {
        var erros = await ValidarAsync(dto, idExcluir: null, cancellationToken);
        if (erros.Count > 0)
        {
            return (null, erros);
        }

        var empresa = new Empresa();
        AplicarDto(empresa, dto);

        await repository.IncluirAsync(empresa, cancellationToken);
        return (empresa.Id, null);
    }

    public async Task<(bool Encontrado, IReadOnlyDictionary<string, string[]>? Erros)> AtualizarAsync(int id, EmpresaDto dto, CancellationToken cancellationToken = default)
    {
        var empresa = await repository.ObterPorIdAsync(id, cancellationToken);
        if (empresa is null)
        {
            return (false, null);
        }

        var erros = await ValidarAsync(dto, idExcluir: id, cancellationToken);
        if (erros.Count > 0)
        {
            return (true, erros);
        }

        AplicarDto(empresa, dto);

        await repository.AtualizarAsync(empresa, cancellationToken);
        return (true, null);
    }

    public async Task<bool> ExcluirAsync(int id, CancellationToken cancellationToken = default)
    {
        var empresa = await repository.ObterPorIdAsync(id, cancellationToken);
        if (empresa is null)
        {
            return false;
        }

        await repository.ExcluirAsync(empresa, cancellationToken);
        return true;
    }

    private static void AplicarDto(Empresa empresa, EmpresaDto dto)
    {
        empresa.RazaoSocial = dto.RazaoSocial;
        empresa.Cnpj = dto.Cnpj.ToUpperInvariant();
        empresa.Uf = dto.Uf.ToUpperInvariant();
        empresa.CertificadoDigital = dto.CertificadoDigital;
        empresa.SenhaCertificado = dto.SenhaCertificado;
        empresa.PastaXml = dto.PastaXml;
        empresa.UltimoNsu = dto.UltimoNsu;
        empresa.Ambiente = dto.Ambiente;
        empresa.Timeout = dto.Timeout;
        empresa.TempoRetorno = dto.TempoRetorno;
        empresa.IntervaloTentativas = dto.IntervaloTentativas;
        empresa.QuantidadeTentativas = dto.QuantidadeTentativas;
        empresa.EmailEnvioNotificacoes = dto.EmailEnvioNotificacoes;
        empresa.ServidorSmtp = dto.ServidorSmtp;
        empresa.UsuarioSmtp = dto.UsuarioSmtp;
        empresa.SenhaSmtp = dto.SenhaSmtp;
        empresa.EmailSmtp = dto.EmailSmtp;
        empresa.TipoAutenticacaoSmtp = dto.TipoAutenticacaoSmtp;
        empresa.ServidorPop = dto.ServidorPop;
        empresa.UsuarioPop = dto.UsuarioPop;
        empresa.EmailPop = dto.EmailPop;
        empresa.SenhaPop = dto.SenhaPop;
        empresa.TipoAutenticacaoPop = dto.TipoAutenticacaoPop;
        empresa.PortaPop = dto.PortaPop;
        empresa.PortaSmtp = dto.PortaSmtp;
        empresa.Ie = dto.Ie;
        empresa.Manifesta = dto.Manifesta ? "S" : "N";
        empresa.Posicao = dto.Posicao;
        empresa.Inativo = dto.Inativo ? "S" : "N";
        empresa.UltimoNsuCte = dto.UltimoNsuCte;
        empresa.HoraInicial = dto.HoraInicial;
        empresa.HoraFinal = dto.HoraFinal;
    }

    private static EmpresaDto ParaDto(Empresa empresa) => new()
    {
        Id = empresa.Id,
        RazaoSocial = empresa.RazaoSocial ?? string.Empty,
        Cnpj = empresa.Cnpj ?? string.Empty,
        Uf = empresa.Uf ?? string.Empty,
        CertificadoDigital = empresa.CertificadoDigital,
        SenhaCertificado = empresa.SenhaCertificado,
        PastaXml = empresa.PastaXml,
        UltimoNsu = empresa.UltimoNsu,
        Ambiente = empresa.Ambiente,
        Timeout = empresa.Timeout,
        TempoRetorno = empresa.TempoRetorno,
        IntervaloTentativas = empresa.IntervaloTentativas,
        QuantidadeTentativas = empresa.QuantidadeTentativas,
        EmailEnvioNotificacoes = empresa.EmailEnvioNotificacoes,
        ServidorSmtp = empresa.ServidorSmtp,
        UsuarioSmtp = empresa.UsuarioSmtp,
        SenhaSmtp = empresa.SenhaSmtp,
        EmailSmtp = empresa.EmailSmtp,
        TipoAutenticacaoSmtp = empresa.TipoAutenticacaoSmtp,
        ServidorPop = empresa.ServidorPop,
        UsuarioPop = empresa.UsuarioPop,
        EmailPop = empresa.EmailPop,
        SenhaPop = empresa.SenhaPop,
        TipoAutenticacaoPop = empresa.TipoAutenticacaoPop,
        PortaPop = empresa.PortaPop,
        PortaSmtp = empresa.PortaSmtp,
        Ie = empresa.Ie,
        Manifesta = empresa.Manifesta == "S",
        Posicao = empresa.Posicao,
        Inativo = empresa.Inativo == "S",
        UltimoNsuCte = empresa.UltimoNsuCte,
        HoraInicial = empresa.HoraInicial,
        HoraFinal = empresa.HoraFinal
    };

    private async Task<Dictionary<string, string[]>> ValidarAsync(EmpresaDto dto, int? idExcluir, CancellationToken cancellationToken)
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

        if (string.IsNullOrWhiteSpace(dto.RazaoSocial))
        {
            AdicionarErro(nameof(dto.RazaoSocial), "Razão Social é obrigatória.");
        }
        else if (dto.RazaoSocial.Length > 60)
        {
            AdicionarErro(nameof(dto.RazaoSocial), "Razão Social deve ter no máximo 60 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(dto.Cnpj))
        {
            AdicionarErro(nameof(dto.Cnpj), "C.N.P.J. é obrigatório.");
        }
        else if (!CnpjValidator.FormatoValido(dto.Cnpj))
        {
            AdicionarErro(nameof(dto.Cnpj), "C.N.P.J. deve conter 14 caracteres alfanuméricos, sem máscara.");
        }
        else if (!CnpjValidator.DigitosVerificadoresValidos(dto.Cnpj.ToUpperInvariant()))
        {
            AdicionarErro(nameof(dto.Cnpj), "C.N.P.J. inválido (dígitos verificadores não conferem).");
        }
        else if (await repository.ExisteComCnpjAsync(dto.Cnpj.ToUpperInvariant(), idExcluir, cancellationToken))
        {
            AdicionarErro(nameof(dto.Cnpj), "Já existe uma empresa cadastrada com esse C.N.P.J.");
        }

        if (string.IsNullOrWhiteSpace(dto.Uf))
        {
            AdicionarErro(nameof(dto.Uf), "UF é obrigatória.");
        }
        else if (!UfsValidas.Contains(dto.Uf.ToUpperInvariant()))
        {
            AdicionarErro(nameof(dto.Uf), "UF inválida.");
        }

        return erros.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
    }
}
