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

    /// <summary>
    /// Configura a URL de webhook da instância, para que mensagens recebidas
    /// sejam encaminhadas automaticamente para a nossa API.
    /// </summary>
    Task SetWebhookAsync(string instanceName, string webhookUrl, CancellationToken ct = default);

    /// <summary>
    /// Desconecta a sessão do WhatsApp (equivalente a "sair" no aparelho),
    /// mas mantém a instância — pode reconectar escaneando um novo QR code.
    /// </summary>
    Task LogoutAsync(string instanceName, CancellationToken ct = default);
}
