using System.Text.RegularExpressions;

namespace WhatsappCrmIA.Domain.Common;

/// <summary>
/// Números de celular brasileiros têm 13 dígitos (55 + DDD + 9 + 8 dígitos),
/// mas o WhatsApp às vezes manda no formato antigo, sem o "9" (12 dígitos).
/// Sem normalizar, a mesma pessoa vira dois contatos diferentes dependendo de
/// quem iniciou a conversa (o painel, que usa o formato completo, ou o
/// WhatsApp, que às vezes manda o formato antigo no webhook).
/// </summary>
public static class PhoneNumberNormalizer
{
    public static string Normalize(string rawNumber)
    {
        var digits = Regex.Replace(rawNumber ?? string.Empty, "[^0-9]", "");

        // 12 dígitos + começa com 55 (DDI Brasil) + os 8 dígitos finais formam
        // um número de celular válido → provavelmente está faltando o "9".
        if (digits.Length == 12 && digits.StartsWith("55"))
        {
            var ddd = digits.Substring(2, 2);
            var rest = digits.Substring(4);
            if (rest.Length == 8)
            {
                digits = $"55{ddd}9{rest}";
            }
        }

        return digits;
    }
}
