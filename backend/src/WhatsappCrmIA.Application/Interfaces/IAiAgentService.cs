using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.Interfaces;

public record AiReplyResult(
    string ReplyText, ConversationIntent DetectedIntent, bool ShouldEscalateToHuman,
    int InputTokens, int OutputTokens);

/// <summary>
/// Abstração sobre o provedor de LLM (Claude/Anthropic no MVP).
/// </summary>
public interface IAiAgentService
{
    /// <summary>
    /// Gera uma resposta para a conversa considerando o histórico recente
    /// e o system prompt configurado no AiAgentConfig do tenant.
    /// </summary>
    Task<AiReplyResult> GenerateReplyAsync(
        string apiKey,
        string systemPrompt,
        IReadOnlyList<(string role, string content)> conversationHistory,
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
