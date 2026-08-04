using WhatsappCrmIA.Domain.Common;

namespace WhatsappCrmIA.Domain.Entities;

/// <summary>
/// Dados da instância WhatsApp conectada via Evolution API para um tenant.
/// </summary>
public class WhatsAppConnection : BaseEntity
{
    public string InstanceName { get; set; } = default!; // nome da instância na Evolution API
    public string? PhoneNumber { get; set; }
    public bool IsConnected { get; set; }
    public string? QrCodeBase64 { get; set; } // exibido no painel enquanto não conecta
    public DateTime? ConnectedAtUtc { get; set; }
}
