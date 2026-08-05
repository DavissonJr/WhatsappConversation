using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Infrastructure.Services;

/// <summary>
/// Implementação de IAiAgentService usando a API da Anthropic (Claude).
/// A chave de API vem por chamada (não é mais fixa no construtor), porque
/// cada tenant usa a própria conta/chave da Anthropic — o custo da IA sai
/// da conta de cada empresa cliente, não da conta de quem administra o SaaS.
/// </summary>
public class ClaudeAiAgentService : IAiAgentService
{
    private readonly HttpClient _http;
    private const string Model = "claude-sonnet-5";

    public ClaudeAiAgentService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.anthropic.com/");
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException($"Anthropic API respondeu {(int)response.StatusCode}: {body}");
    }

    private HttpRequestMessage BuildRequest(string apiKey, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        return request;
    }

    public async Task<AiReplyResult> GenerateReplyAsync(
        string apiKey,
        string systemPrompt,
        IReadOnlyList<(string role, string content)> conversationHistory,
        CancellationToken ct = default)
    {
        var fullSystemPrompt = $$"""
            {{systemPrompt}}

            Responda SEMPRE em JSON puro, sem markdown, no formato:
            {"reply": "texto da resposta ao cliente em pt-BR",
             "intent": "GeneralQuestion|PriceRequest|Scheduling|Complaint|Other",
             "escalate_to_human": true|false}

            Marque escalate_to_human=true se o cliente pedir para falar com humano,
            reclamar de algo sério, ou se a pergunta fugir do escopo do negócio.
            """;

        var payload = new
        {
            model = Model,
            max_tokens = 1000,
            system = fullSystemPrompt,
            messages = conversationHistory.Select(m => new { role = m.role, content = m.content })
        };

        var response = await _http.SendAsync(BuildRequest(apiKey, payload), ct);
        await EnsureSuccessOrThrowAsync(response, ct);

        var raw = await response.Content.ReadFromJsonAsync<ClaudeResponse>(cancellationToken: ct);
        var text = raw?.Content?.FirstOrDefault(c => c.Type == "text")?.Text ?? "{}";
        var clean = text.Replace("```json", "").Replace("```", "").Trim();
        var inputTokens = raw?.Usage?.InputTokens ?? 0;
        var outputTokens = raw?.Usage?.OutputTokens ?? 0;

        try
        {
            var parsed = JsonSerializer.Deserialize<AiReplyJson>(clean,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var intent = Enum.TryParse<ConversationIntent>(parsed?.Intent, true, out var i)
                ? i : ConversationIntent.Unknown;

            return new AiReplyResult(
                parsed?.Reply ?? "Desculpe, não consegui processar sua mensagem agora.",
                intent,
                parsed?.EscalateToHuman ?? false,
                inputTokens,
                outputTokens);
        }
        catch (JsonException)
        {
            // fallback defensivo: se a IA não devolver JSON válido, ainda respondemos algo
            return new AiReplyResult(clean, ConversationIntent.Unknown, true, inputTokens, outputTokens);
        }
    }

    public async Task<string> GenerateProposalDraftAsync(
        string apiKey,
        string businessContext,
        IReadOnlyList<(string role, string content)> conversationHistory,
        CancellationToken ct = default)
    {
        var systemPrompt = $"""
            Você ajuda a redigir propostas comerciais claras e objetivas em pt-BR
            para o seguinte negócio: {businessContext}
            Baseie-se no histórico da conversa para entender a necessidade do cliente.
            Responda apenas com o texto da proposta, sem comentários adicionais.
            """;

        var payload = new
        {
            model = Model,
            max_tokens = 800,
            system = systemPrompt,
            messages = conversationHistory.Select(m => new { role = m.role, content = m.content })
        };

        var response = await _http.SendAsync(BuildRequest(apiKey, payload), ct);
        await EnsureSuccessOrThrowAsync(response, ct);

        var raw = await response.Content.ReadFromJsonAsync<ClaudeResponse>(cancellationToken: ct);
        return raw?.Content?.FirstOrDefault(c => c.Type == "text")?.Text ?? string.Empty;
    }

    private class AiReplyJson
    {
        public string? Reply { get; set; }
        public string? Intent { get; set; }

        [JsonPropertyName("escalate_to_human")]
        public bool EscalateToHuman { get; set; }
    }

    private class ClaudeResponse
    {
        [JsonPropertyName("content")]
        public List<ClaudeContentBlock>? Content { get; set; }

        [JsonPropertyName("usage")]
        public ClaudeUsage? Usage { get; set; }
    }

    private class ClaudeUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }

    private class ClaudeContentBlock
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
