namespace WhatsappCrmIA.Application.Interfaces;

/// <summary>
/// Abstração sobre o provedor de WhatsApp (Evolution API no MVP;
/// pode ser trocado por WhatsApp Cloud API no futuro sem tocar no domínio).
/// </summary>
public interface IWhatsAppGateway
{
    Task<string> CreateInstanceAsync(string instanceName, CancellationToken ct = default);
    Task<string> GetQrCodeAsync(string instanceName, CancellationToken ct = default);
    Task<bool> IsConnectedAsync(string instanceName, CancellationToken ct = default);
    Task SendTextMessageAsync(string instanceName, string toPhoneNumber, string message, CancellationToken ct = default);
}
