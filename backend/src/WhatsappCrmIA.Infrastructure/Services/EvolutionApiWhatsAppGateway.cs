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
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> GetQrCodeAsync(string instanceName, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/instance/connect/{instanceName}", ct);
        response.EnsureSuccessStatusCode();

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
        response.EnsureSuccessStatusCode();
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
