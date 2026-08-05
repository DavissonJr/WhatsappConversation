namespace WhatsappCrmIA.Application.Interfaces;

/// <summary>
/// Lançada quando a Evolution API responde com erro. Carrega o corpo da resposta
/// para dar contexto real do que deu errado, em vez de um HttpRequestException genérico.
/// </summary>
public class EvolutionApiException : Exception
{
    public int StatusCode { get; }

    public EvolutionApiException(int statusCode, string responseBody)
        : base($"Evolution API respondeu {statusCode}: {Truncate(responseBody)}")
    {
        StatusCode = statusCode;
    }

    private static string Truncate(string text) =>
        text.Length > 300 ? text[..300] + "..." : text;
}
