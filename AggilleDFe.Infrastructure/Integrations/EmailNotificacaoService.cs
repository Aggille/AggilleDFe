using System.Net;
using System.Net.Mail;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AggilleDFe.Infrastructure.Integrations;

public class EmailNotificacaoService(ILogger<EmailNotificacaoService> logger) : IEmailNotificacaoService
{
    public async Task EnviarAsync(Empresa empresa, string assunto, string corpo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(empresa.EmailEnvioNotificacoes) || string.IsNullOrWhiteSpace(empresa.ServidorSmtp))
        {
            return;
        }

        try
        {
            using var mensagem = new MailMessage
            {
                From = new MailAddress(string.IsNullOrWhiteSpace(empresa.EmailSmtp) ? empresa.EmailEnvioNotificacoes : empresa.EmailSmtp),
                Subject = assunto,
                Body = corpo
            };
            mensagem.To.Add(empresa.EmailEnvioNotificacoes);

            using var cliente = new SmtpClient(empresa.ServidorSmtp, empresa.PortaSmtp ?? 587)
            {
                EnableSsl = empresa.TipoAutenticacaoSmtp is not null,
                Credentials = string.IsNullOrWhiteSpace(empresa.UsuarioSmtp)
                    ? null
                    : new NetworkCredential(empresa.UsuarioSmtp, empresa.SenhaSmtp)
            };

            await cliente.SendMailAsync(mensagem, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar e-mail de notificação para a empresa {EmpresaId} ({Assunto}).", empresa.Id, assunto);
        }
    }
}
