using AggilleDFe.Domain.Entities;

namespace AggilleDFe.Application.Interfaces;

public interface IEmailNotificacaoService
{
    /// <summary>
    /// Envia um e-mail de notificação usando o SMTP cadastrado na própria empresa
    /// (Empresa.ServidorSmtp/PortaSmtp/UsuarioSmtp/SenhaSmtp) para
    /// Empresa.EmailEnvioNotificacoes. Se a empresa não tiver e-mail de notificação
    /// ou servidor SMTP configurado, não faz nada (silencioso — notificação é
    /// opcional). Falhas de envio são logadas, mas nunca lançam exceção — nunca deve
    /// interromper o fluxo principal (distribuição de DFe).
    /// </summary>
    Task EnviarAsync(Empresa empresa, string assunto, string corpo, CancellationToken cancellationToken = default);
}
