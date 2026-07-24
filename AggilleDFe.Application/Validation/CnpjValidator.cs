using System.Text.RegularExpressions;

namespace AggilleDFe.Application.Validation;

public static partial class CnpjValidator
{
    public static bool FormatoValido(string cnpj) => CnpjRegex().IsMatch(cnpj);

    // Algoritmo oficial da Receita Federal para o CNPJ alfanumérico:
    // cada caractere (dígito ou letra maiúscula) vale seu código ASCII menos 48;
    // os 2 dígitos verificadores continuam sempre numéricos.
    public static bool DigitosVerificadoresValidos(string cnpj)
    {
        if (cnpj.Length != 14 || !char.IsDigit(cnpj[12]) || !char.IsDigit(cnpj[13]))
        {
            return false;
        }

        int[] pesosDigito1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] pesosDigito2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var valoresBase = cnpj.Take(12).Select(c => c - '0').ToArray();

        var digito1 = CalcularDigito(valoresBase, pesosDigito1);
        var valoresComDigito1 = valoresBase.Append(digito1).ToArray();
        var digito2 = CalcularDigito(valoresComDigito1, pesosDigito2);

        return digito1 == cnpj[12] - '0' && digito2 == cnpj[13] - '0';

        static int CalcularDigito(int[] valores, int[] pesos)
        {
            var soma = valores.Zip(pesos, (valor, peso) => valor * peso).Sum();
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }

    [GeneratedRegex("^[A-Za-z0-9]{14}$")]
    private static partial Regex CnpjRegex();
}
