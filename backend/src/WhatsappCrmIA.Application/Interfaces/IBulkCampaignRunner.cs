namespace WhatsappCrmIA.Application.Interfaces;

/// <summary>
/// Dispara o processamento de uma campanha de mensagem em lote em segundo
/// plano (implementado com Hangfire). É "fire and forget" — a campanha roda
/// aos poucos, respeitando o intervalo entre envios configurado.
/// </summary>
public interface IBulkCampaignRunner
{
    void Enqueue(Guid campaignId);
}
