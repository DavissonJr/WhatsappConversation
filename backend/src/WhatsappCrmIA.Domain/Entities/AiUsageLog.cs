using WhatsappCrmIA.Domain.Common;

namespace WhatsappCrmIA.Domain.Entities;

/// <summary>
/// Um registro de cada chamada à IA que consumiu créditos. Serve tanto para
/// auditoria (o tenant consegue ver pra onde o dinheiro foi) quanto para
/// calcular estatísticas de uso.
/// </summary>
public class AiUsageLog : BaseEntity
{
    public Guid? ConversationId { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal CostUsd { get; set; }
}
