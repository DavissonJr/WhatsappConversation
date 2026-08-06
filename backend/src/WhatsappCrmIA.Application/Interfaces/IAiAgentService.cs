using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.Interfaces;

public record AiReplyResult(
    string ReplyText, ConversationIntent DetectedIntent, bool ShouldEscalateToHuman,
    int InputTokens, int OutputTokens, bool CreatedAppointment);

/// <summary>
/// Dados extraídos pela IA quando ela decide criar um agendamento de verdade
/// (via "tool use" da Anthropic — a IA não inventa isso, só chama a
/// ferramenta quando o cliente confirmou data e horário claramente).
/// </summary>
public record AppointmentToolRequest(string Title, DateTime ScheduledForUtc, string? Notes);

/// <summary>
/// Abstração sobre o provedor de LLM (Claude/Anthropic no MVP).
/// </summary>
public interface IAiAgentService
{
    /// <summary>
    /// Gera uma resposta para a conversa considerando o histórico recente e o
    /// system prompt configurado no AiAgentConfig do tenant. Se o cliente
    /// confirmar um agendamento durante a conversa, a IA pode chamar
    /// <paramref name="onCreateAppointment"/> pra criar de verdade — o
    /// callback deve devolver uma frase de confirmação (ex: "Agendamento
    /// criado para 10/08 às 15h"), que a IA usa pra formular a resposta final
    /// ao cliente. Passe null se essa conversa não pode criar agendamentos
    /// (ex: nenhum número conectado disponível).
    /// </summary>
    Task<AiReplyResult> GenerateReplyAsync(
        string apiKey,
        string systemPrompt,
        IReadOnlyList<(string role, string content)> conversationHistory,
        DateTime currentLocalTime,
        Func<AppointmentToolRequest, Task<(bool Success, string Message)>>? onCreateAppointment,
        CancellationToken ct = default);

    /// <summary>
    /// Gera o texto de uma proposta comercial a partir do contexto da conversa.
    /// </summary>
    Task<string> GenerateProposalDraftAsync(
        string apiKey,
        string businessContext,
        IReadOnlyList<(string role, string content)> conversationHistory,
        CancellationToken ct = default);
}
