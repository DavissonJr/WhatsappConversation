using WhatsappCrmIA.Domain.Common;

namespace WhatsappCrmIA.Domain.Entities;

/// <summary>
/// Dados de um número de WhatsApp conectado via Evolution API para um tenant.
/// Um tenant pode ter vários números (ex: "Recepção", "Financeiro").
/// </summary>
public class WhatsAppConnection : BaseEntity
{
    public string Label { get; set; } = default!; // nome dado pelo usuário, ex: "Recepção"
    public string InstanceName { get; set; } = default!; // nome único na Evolution API
    public string? PhoneNumber { get; set; }
    public bool IsConnected { get; set; }
    public string? QrCodeBase64 { get; set; }
    public DateTime? ConnectedAtUtc { get; set; }
}
