using AggilleDFe.Domain.Entities;

namespace AggilleDFe.Application.Services;

public static class JanelaExecucaoService
{
    public static bool PodeExecutar(Empresa empresa, TimeOnly horaAtual, bool execucaoManual)
    {
        if (execucaoManual)
        {
            return true;
        }

        if (empresa.HoraInicial is null || empresa.HoraFinal is null)
        {
            return true;
        }

        var inicio = empresa.HoraInicial.Value;
        var fim = empresa.HoraFinal.Value;

        return inicio <= fim
            ? horaAtual >= inicio && horaAtual <= fim
            : horaAtual >= inicio || horaAtual <= fim;
    }

    public static bool PodeExecutar(Empresa empresa, DateTime agora, bool execucaoManual) =>
        PodeExecutar(empresa, TimeOnly.FromDateTime(agora), execucaoManual);
}
