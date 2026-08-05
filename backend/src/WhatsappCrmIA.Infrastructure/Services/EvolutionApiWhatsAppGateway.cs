using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Infrastructure.Services;

/// <summary>
/// Implementação do gateway WhatsApp usando a Evolution API (self-hosted, não-oficial).
/// Documentação: https://doc.evolution-api.com
/// Troca futura para WhatsApp Cloud API = criar outra classe implementando IWhatsAppGateway.
/// </summary>
public class EvolutionApiWhatsAppGateway : IWhatsAppGateway
{
    private readonly HttpClient _http;

    public EvolutionApiWhatsAppGateway(HttpClient http, IConfiguration config)
    {
        _http = http;
        _http.BaseAddress = new Uri(config["EvolutionApi:BaseUrl"]!);
        _http.DefaultRequestHeaders.Add("apikey", config["EvolutionApi:ApiKey"]);
    }

    public async Task<string> CreateInstanceAsync(string instanceName, CancellationToken ct = default)
    {
        var payload = new
        {
            instanceName,
            qrcode = true,
            integration = "WHATSAPP-BAILEYS"
        };

        var response = await _http.PostAsJsonAsync("/instance/create", payload, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> GetQrCodeAsync(string instanceName, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/instance/connect/{instanceName}", ct);
        await EnsureSuccessOrThrowAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<QrCodeResponse>(cancellationToken: ct);
        return result?.Base64 ?? string.Empty;
    }

    public async Task<bool> IsConnectedAsync(string instanceName, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/instance/connectionState/{instanceName}", ct);
        if (!response.IsSuccessStatusCode) return false;

        var result = await response.Content.ReadFromJsonAsync<ConnectionStateResponse>(cancellationToken: ct);
        return result?.Instance?.State == "open";
    }

    public async Task SendTextMessageAsync(
        string instanceName, string toPhoneNumber, string message, CancellationToken ct = default)
    {
        var payload = new
        {
            number = toPhoneNumber,
            text = message
        };

        var response = await _http.PostAsJsonAsync($"/message/sendText/{instanceName}", payload, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
    }

    public async Task SetWebhookAsync(string instanceName, string webhookUrl, CancellationToken ct = default)
    {
        // Essa versão/build da Evolution API exige os campos ANINHADOS dentro de
        // "webhook" (formato "v1 style"). Confirmado pelo erro real que ela retorna
        // quando mandamos os campos soltos: 'instance requires property "webhook"'.
        var payload = new
        {
            webhook = new
            {
                enabled = true,
                url = webhookUrl,
                webhookByEvents = false,
                webhookBase64 = false,
                events = new[] { "MESSAGES_UPSERT" }
            }
        };

        var response = await _http.PostAsJsonAsync($"/webhook/set/{instanceName}", payload, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
    }

    public async Task LogoutAsync(string instanceName, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/instance/logout/{instanceName}", ct);
        await EnsureSuccessOrThrowAsync(response, ct);
    }

    public async Task DeleteInstanceAsync(string instanceName, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/instance/delete/{instanceName}", ct);
        // Se a instância já não existir (ex: nunca chegou a conectar), não é um erro real.
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            await EnsureSuccessOrThrowAsync(response, ct);
    }

    /// <summary>
    /// Em vez de EnsureSuccessStatusCode() (que descarta o corpo da resposta),
    /// lê o corpo e lança uma exceção com o motivo real que a Evolution API deu.
    /// </summary>
    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new EvolutionApiException((int)response.StatusCode, body);
    }

    private class QrCodeResponse
    {
        [JsonPropertyName("base64")]
        public string? Base64 { get; set; }
    }

    private class ConnectionStateResponse
    {
        [JsonPropertyName("instance")]
        public InstanceState? Instance { get; set; }
    }

    private class InstanceState
    {
        [JsonPropertyName("state")]
        public string? State { get; set; }
    }
}
