using WhatsappCrmIA.Domain.Common;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Domain.Entities;

/// <summary>
/// Modelo de mensagem reutilizável, categorizado por escopo (cobrança, lembrete,
/// boas-vindas...). Pode ser usado manualmente pelo atendente ou referenciado
/// pela IA/pelos jobs de lembrete para montar a mensagem final.
/// </summary>
public class MessageTemplate : BaseEntity
{
    public string Name { get; set; } = default!;
    public TemplateScope Scope { get; set; }
    public string Content { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
