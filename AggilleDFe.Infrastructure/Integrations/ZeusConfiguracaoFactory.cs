using System.Security.Cryptography.X509Certificates;
using AggilleDFe.Domain.Entities;
using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Utils;

namespace AggilleDFe.Infrastructure.Integrations;

public static class ZeusConfiguracaoFactory
{
    public static X509Certificate2 CarregarCertificado(Empresa empresa)
    {
        if (string.IsNullOrWhiteSpace(empresa.CertificadoDigital))
        {
            throw new InvalidOperationException("A empresa não possui um certificado digital configurado.");
        }

        if (!File.Exists(empresa.CertificadoDigital))
        {
            throw new InvalidOperationException($"Arquivo de certificado digital não encontrado: {empresa.CertificadoDigital}");
        }

        return CertificadoDigitalUtils.ObterDoCaminho(empresa.CertificadoDigital, empresa.SenhaCertificado ?? string.Empty);
    }

    public static ConfiguracaoServico Criar(Empresa empresa, string? diretorioSchemas)
    {
        if (string.IsNullOrWhiteSpace(empresa.Uf) || !Enum.TryParse<Estado>(empresa.Uf, out var uf))
        {
            throw new InvalidOperationException($"UF da empresa inválida para o Zeus DFe.NET: \"{empresa.Uf}\".");
        }

        var ambiente = empresa.Ambiente switch
        {
            "P" => TipoAmbiente.Producao,
            "H" => TipoAmbiente.Homologacao,
            _ => throw new InvalidOperationException($"Ambiente da NFe inválido para a empresa (esperado \"P\" ou \"H\"): \"{empresa.Ambiente}\".")
        };

        var validarSchemas = !string.IsNullOrWhiteSpace(diretorioSchemas) && Directory.Exists(diretorioSchemas);

        return new ConfiguracaoServico
        {
            Certificado = new ConfiguracaoCertificado
            {
                TipoCertificado = TipoCertificado.A1Arquivo,
                Arquivo = empresa.CertificadoDigital,
                Senha = empresa.SenhaCertificado
            },
            TimeOut = empresa.Timeout ?? 30000,
            cUF = uf,
            tpAmb = ambiente,
            tpEmis = TipoEmissao.teNormal,
            ModeloDocumento = ModeloDocumento.NFe,
            DefineVersaoServicosAutomaticamente = false,
            VersaoNfeStatusServico = VersaoServico.Versao400,
            ValidarSchemas = validarSchemas,
            DiretorioSchemas = validarSchemas ? diretorioSchemas : null
        };
    }
}
