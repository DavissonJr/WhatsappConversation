using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Infrastructure.Services;

/// <summary>
/// Implementação de IAiAgentService usando a API da Anthropic (Claude).
/// A chave de API vem por chamada (cada tenant usa a própria conta).
/// Suporta "tool use": a IA pode chamar a ferramenta "create_appointment"
/// quando o cliente confirma um agendamento durante a conversa.
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

    private static readonly object[] Tools =
    [
        new
        {
            name = "create_appointment",
            description =
                "Cria um agendamento de verdade no sistema quando o cliente CONFIRMAR claramente " +
                "uma data e horário específicos para um compromisso (consulta, serviço, reunião etc). " +
                "Só chame esta ferramenta quando tiver certeza da data e do horário exatos — se " +
                "estiver ambíguo (ex: só 'sexta-feira' sem hora, ou 'de manhã' sem data), NÃO chame " +
                "a ferramenta ainda: responda normalmente pedindo a informação que falta.",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    title = new { type = "string", description = "Título curto do agendamento, ex: 'Consulta odontológica'" },
                    scheduled_for = new { type = "string", description = "Data e hora exatas no formato ISO 8601 sem fuso, ex: 2026-08-10T15:00:00" },
                    notes = new { type = "string", description = "Observações relevantes que o cliente mencionou (opcional)" }
                },
                required = new[] { "title", "scheduled_for" }
            }
        }
    ];

    public async Task<AiReplyResult> GenerateReplyAsync(
        string apiKey,
        string systemPrompt,
        IReadOnlyList<(string role, string content)> conversationHistory,
        DateTime currentLocalTime,
        Func<AppointmentToolRequest, Task<string>>? onCreateAppointment,
        CancellationToken ct = default)
    {
        var canCreateAppointment = onCreateAppointment is not null;

        var fullSystemPrompt = $$"""
            {{systemPrompt}}

            Data e hora atuais (horário local do estabelecimento): {{currentLocalTime:dddd, dd/MM/yyyy HH:mm}}.
            Use isso para calcular datas relativas que o cliente mencionar (ex: "amanhã", "sexta que vem").

            {{(canCreateAppointment
                ? "Você TEM a ferramenta create_appointment disponível — use-a de verdade quando o cliente confirmar um agendamento."
                : "Você NÃO tem uma ferramenta de agendamento disponível agora. Se o cliente quiser agendar, avise que um atendente vai confirmar o horário.")}}

            Depois de qualquer ação (ou se não usar nenhuma ferramenta), responda SEMPRE em JSON puro,
            sem markdown, no formato:
            {"reply": "texto da resposta ao cliente em pt-BR",
             "intent": "GeneralQuestion|PriceRequest|Scheduling|Complaint|Other",
             "escalate_to_human": true|false}

            Marque escalate_to_human=true se o cliente pedir para falar com humano,
            reclamar de algo sério, ou se a pergunta fugir do escopo do negócio.
            """;

        var messages = conversationHistory
            .Select(m => new Dictionary<string, object> { ["role"] = m.role, ["content"] = m.content })
            .ToList();

        var payload = new Dictionary<string, object>
        {
            ["model"] = Model,
            ["max_tokens"] = 1000,
            ["system"] = fullSystemPrompt,
            ["messages"] = messages
        };
        if (canCreateAppointment) payload["tools"] = Tools;

        var response = await _http.SendAsync(BuildRequest(apiKey, payload), ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        var raw = await response.Content.ReadFromJsonAsync<ClaudeResponse>(cancellationToken: ct);

        var totalInputTokens = raw?.Usage?.InputTokens ?? 0;
        var totalOutputTokens = raw?.Usage?.OutputTokens ?? 0;
        var createdAppointment = false;

        var toolUseBlock = raw?.Content?.FirstOrDefault(c => c.Type == "tool_use" && c.Name == "create_appointment");

        if (toolUseBlock is not null && onCreateAppointment is not null)
        {
            createdAppointment = true;

            string toolResultText;
            try
            {
                var input = toolUseBlock.Input.GetValueOrDefault();
                var title = input.TryGetProperty("title", out var t) ? t.GetString() ?? "Agendamento" : "Agendamento";
                var scheduledForRaw = input.TryGetProperty("scheduled_for", out var s) ? s.GetString() : null;
                var notes = input.TryGetProperty("notes", out var n) ? n.GetString() : null;

                if (scheduledForRaw is null || !DateTime.TryParse(scheduledForRaw, out var scheduledForLocal))
                {
                    toolResultText = "Erro: não foi possível entender a data/hora informada. Peça pro cliente confirmar de novo, com data e hora bem claras.";
                    createdAppointment = false;
                }
                else
                {
                    var scheduledForUtc = DateTime.SpecifyKind(scheduledForLocal, DateTimeKind.Unspecified) - (currentLocalTime - DateTime.UtcNow);
                    toolResultText = await onCreateAppointment(new AppointmentToolRequest(title, scheduledForUtc, notes));
                }
            }
            catch (Exception ex)
            {
                toolResultText = $"Erro ao criar o agendamento: {ex.Message}. Avise o cliente que um atendente vai confirmar manualmente.";
                createdAppointment = false;
            }

            // Manda de volta pro Claude o resultado da ferramenta, pra ele formular a resposta final.
            var assistantContent = raw!.Content!.Select(c => c.Type == "tool_use"
                ? (object)new { type = "tool_use", id = c.Id, name = c.Name, input = c.Input }
                : new { type = "text", text = c.Text }).ToList();

            messages.Add(new Dictionary<string, object> { ["role"] = "assistant", ["content"] = assistantContent });
            messages.Add(new Dictionary<string, object>
            {
                ["role"] = "user",
                ["content"] = new object[]
                {
                    new { type = "tool_result", tool_use_id = toolUseBlock.Id, content = toolResultText }
                }
            });

            var followUpPayload = new Dictionary<string, object>
            {
                ["model"] = Model,
                ["max_tokens"] = 1000,
                ["system"] = fullSystemPrompt,
                ["messages"] = messages,
                ["tools"] = Tools
            };

            var followUpResponse = await _http.SendAsync(BuildRequest(apiKey, followUpPayload), ct);
            await EnsureSuccessOrThrowAsync(followUpResponse, ct);
            raw = await followUpResponse.Content.ReadFromJsonAsync<ClaudeResponse>(cancellationToken: ct);

            totalInputTokens += raw?.Usage?.InputTokens ?? 0;
            totalOutputTokens += raw?.Usage?.OutputTokens ?? 0;
        }

        var text = raw?.Content?.FirstOrDefault(c => c.Type == "text")?.Text ?? "{}";
        var clean = text.Replace("```json", "").Replace("```", "").Trim();

        try
        {
            var parsed = JsonSerializer.Deserialize<AiReplyJson>(clean,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var intent = Enum.TryParse<ConversationIntent>(parsed?.Intent, true, out var i)
                ? i : ConversationIntent.Unknown;

            return new AiReplyResult(
                parsed?.Reply ?? "Desculpe, não consegui processar sua mensagem agora.",
                intent, parsed?.EscalateToHuman ?? false,
                totalInputTokens, totalOutputTokens, createdAppointment);
        }
        catch (JsonException)
        {
            return new AiReplyResult(clean, ConversationIntent.Unknown, true, totalInputTokens, totalOutputTokens, createdAppointment);
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

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("input")]
        public JsonElement? Input { get; set; }
    }
}
