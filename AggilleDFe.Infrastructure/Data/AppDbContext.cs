using AggilleDFe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AggilleDFe.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Log> Logs => Set<Log>();
    public DbSet<Xml> Xmls => Set<Xml>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Configuracao>(e =>
        {
            e.ToTable("CONFIGURACAO");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("ID");
            e.Property(x => x.NomeEmpresa).HasColumnName("NOME_EMPRESA").HasMaxLength(60);
            e.Property(x => x.CnpjEmpresa).HasColumnName("CNPJ_EMPRESA").HasMaxLength(14);
            e.Property(x => x.VersaoBanco).HasColumnName("VERSAO_BANCO");
            e.Property(x => x.TempoExecucao).HasColumnName("TEMPO_EXECUCAO");
            e.Property(x => x.QuantidadeEmpresasPermitidas).HasColumnName("QUANTIDADE_EMPRESAS_PERMITIDAS");
            e.Property(x => x.ApiAtiva).HasColumnName("API_ATIVA").HasMaxLength(1);
            e.Property(x => x.PortaApi).HasColumnName("PORTA_API");
            e.Property(x => x.UsuarioApi).HasColumnName("USUARIO_API").HasMaxLength(50);
            e.Property(x => x.SenhaApi).HasColumnName("SENHA_API").HasMaxLength(20);
            e.Property(x => x.ProcessarIndividualmente).HasColumnName("PROCESSAR_INDIVIDUALMENTE").HasMaxLength(1);
            e.Property(x => x.UltimaEmpresaProcessadaId).HasColumnName("ULTIMA_EMPRESA_PROCESSADA_ID");
        });

        modelBuilder.Entity<Empresa>(e =>
        {
            e.ToTable("EMPRESAS");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("ID");
            e.Property(x => x.RazaoSocial).HasColumnName("RAZAO_SOCIAL").HasMaxLength(60);
            e.Property(x => x.Cnpj).HasColumnName("CNPJ").HasMaxLength(20);
            e.Property(x => x.Uf).HasColumnName("UF").HasMaxLength(2);
            e.Property(x => x.CertificadoDigital).HasColumnName("CERTIFICADO_DIGITAL").HasMaxLength(1024);
            e.Property(x => x.SenhaCertificado).HasColumnName("SENHA_CERTIFICADO").HasMaxLength(50);
            e.Property(x => x.PastaXml).HasColumnName("PASTA_XML").HasMaxLength(1024);
            e.Property(x => x.UltimoNsu).HasColumnName("ULTIMO_NSU");
            e.Property(x => x.Ambiente).HasColumnName("AMBIENTE").HasMaxLength(1);
            e.Property(x => x.Timeout).HasColumnName("TIMEOUT");
            e.Property(x => x.TempoRetorno).HasColumnName("TEMPO_RETORNO");
            e.Property(x => x.IntervaloTentativas).HasColumnName("INTERVALO_TENTATIVAS");
            e.Property(x => x.QuantidadeTentativas).HasColumnName("QUANTIDADE_TENTATIVAS");
            e.Property(x => x.EmailEnvioNotificacoes).HasColumnName("EMAIL_ENVIO_NOTIFICACOES").HasMaxLength(1024);
            e.Property(x => x.ServidorSmtp).HasColumnName("SERVIDOR_SMTP").HasMaxLength(200);
            e.Property(x => x.UsuarioSmtp).HasColumnName("USUARIO_SMTP").HasMaxLength(50);
            e.Property(x => x.SenhaSmtp).HasColumnName("SENHA_SMTP").HasMaxLength(20);
            e.Property(x => x.EmailSmtp).HasColumnName("EMAIL_SMTP").HasMaxLength(200);
            e.Property(x => x.TipoAutenticacaoSmtp).HasColumnName("TIPO_AUTENTICACAO_SMTP");
            e.Property(x => x.ServidorPop).HasColumnName("SERVIDOR_POP").HasMaxLength(200);
            e.Property(x => x.UsuarioPop).HasColumnName("USUARIO_POP").HasMaxLength(50);
            e.Property(x => x.EmailPop).HasColumnName("EMAIL_POP").HasMaxLength(200);
            e.Property(x => x.SenhaPop).HasColumnName("SENHA_POP").HasMaxLength(20);
            e.Property(x => x.TipoAutenticacaoPop).HasColumnName("TIPO_AUTENTICACAO_POP");
            e.Property(x => x.PortaPop).HasColumnName("PORTA_POP");
            e.Property(x => x.PortaSmtp).HasColumnName("PORTA_SMTP");
            e.Property(x => x.Ie).HasColumnName("IE").HasMaxLength(20);
            e.Property(x => x.Manifesta).HasColumnName("MANIFESTA").HasMaxLength(1);
            e.Property(x => x.Posicao).HasColumnName("POSICAO");
            e.Property(x => x.Inativo).HasColumnName("INATIVO").HasMaxLength(1);
            e.Property(x => x.UltimoNsuCte).HasColumnName("ULTIMO_NSU_CTE");
            e.Property(x => x.HoraInicial).HasColumnName("HORA_INICIAL");
            e.Property(x => x.HoraFinal).HasColumnName("HORA_FINAL");
            e.Property(x => x.BloqueadaAte).HasColumnName("BLOQUEADA_ATE").HasColumnType("timestamp without time zone");
            e.Property(x => x.CertificadoNotificadoEm).HasColumnName("CERTIFICADO_NOTIFICADO_EM");
        });

        modelBuilder.Entity<Log>(e =>
        {
            e.ToTable("LOGS");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("ID");
            e.Property(x => x.Data).HasColumnName("DATA");
            e.Property(x => x.HoraInicio).HasColumnName("HORA_INICIO");
            e.Property(x => x.HoraFinal).HasColumnName("HORA_FINAL");
            e.Property(x => x.EmpresaId).HasColumnName("EMPRESA_ID");
            e.Property(x => x.QuantidadeXmls).HasColumnName("QUANTIDADE_XMLS");
            e.Property(x => x.Mensagem).HasColumnName("MENSAGEM").HasColumnType("text");
            e.Property(x => x.XmlId).HasColumnName("XML_ID");
            e.Property(x => x.Chave).HasColumnName("CHAVE").HasMaxLength(44);
            e.Property(x => x.Nsu).HasColumnName("NSU");
        });

        modelBuilder.Entity<Xml>(e =>
        {
            e.ToTable("XMLS");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("ID");
            e.Property(x => x.Chave).HasColumnName("CHAVE").HasMaxLength(44).IsRequired();
            e.Property(x => x.Protocolo).HasColumnName("PROTOCOLO").HasMaxLength(30);
            e.Property(x => x.Emissao).HasColumnName("EMISSAO");
            e.Property(x => x.DataDownload).HasColumnName("DATA_DOWNLOAD");
            e.Property(x => x.FornecedorNome).HasColumnName("FORNECEDOR_NOME").HasMaxLength(100);
            e.Property(x => x.FornecedorCnpj).HasColumnName("FORNECEDOR_CNPJ").HasMaxLength(20);
            e.Property(x => x.FornecedorCidade).HasColumnName("FORNECEDOR_CIDADE").HasMaxLength(100);
            e.Property(x => x.FornecedorUf).HasColumnName("FORNECEDOR_UF").HasMaxLength(2);
            e.Property(x => x.ValorTotal).HasColumnName("VALOR_TOTAL").HasPrecision(15, 2);
            e.Property(x => x.ValorIcms).HasColumnName("VALOR_ICMS").HasPrecision(15, 2);
            e.Property(x => x.StatusNfe).HasColumnName("STATUS_NFE");
            e.Property(x => x.MensagemNfe).HasColumnName("MENSAGEM_NFE").HasMaxLength(254);
            e.Property(x => x.NomeXml).HasColumnName("NOME_XML").HasMaxLength(1024);
            e.Property(x => x.Numero).HasColumnName("NUMERO");
            e.Property(x => x.Serie).HasColumnName("SERIE").HasMaxLength(3);
            e.Property(x => x.Modelo).HasColumnName("MODELO").HasMaxLength(3);
            e.Property(x => x.EmpresaId).HasColumnName("EMPRESA_ID");
            e.Property(x => x.Cancelada).HasColumnName("CANCELADA").HasMaxLength(1);
            e.Property(x => x.Schema).HasColumnName("SCHEMA").HasMaxLength(20);
            e.Property(x => x.Descricao).HasColumnName("DESCRICAO").HasMaxLength(100);
            e.Property(x => x.Mensagem).HasColumnName("MENSAGEM").HasMaxLength(100);
            e.Property(x => x.Situacao).HasColumnName("SITUACAO").HasMaxLength(100);
            e.Property(x => x.DataCiencia).HasColumnName("DATA_CIENCIA");
            e.Property(x => x.DataRealizacao).HasColumnName("DATA_REALIZACAO");
            e.Property(x => x.DataNaoRealizacao).HasColumnName("DATA_NAO_REALIZACAO");
            e.Property(x => x.DataDesconhecimento).HasColumnName("DATA_DESCONHECIMENTO");
            e.Property(x => x.MotivoNaoRealizacao).HasColumnName("MOTIVO_NAO_REALIZACAO").HasMaxLength(1024);
            e.Property(x => x.DataCancelamento).HasColumnName("DATA_CANCELAMENTO");
            e.Property(x => x.MotivoCancelamento).HasColumnName("MOTIVO_CANCELAMENTO").HasMaxLength(500);
            e.Property(x => x.ConteudoXml).HasColumnName("CONTEUDO_XML").HasColumnType("text");
        });
    }
}
