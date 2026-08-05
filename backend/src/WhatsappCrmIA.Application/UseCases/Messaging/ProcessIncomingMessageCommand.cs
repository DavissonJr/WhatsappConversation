using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.Messaging;

/// <summary>
/// Fluxo central do produto: mensagem chega do webhook da Evolution API,
/// é persistida, a IA gera (ou sugere) uma resposta, e opcionalmente
/// já dispara o envio de volta ao contato.
/// </summary>
public record ProcessIncomingMessageCommand(
    Guid TenantId,
    string InstanceName,
    string FromPhoneNumber,
    string ContactName,
    string MessageText,
    string WhatsAppMessageId
) : IRequest<ProcessIncomingMessageResult>;

public record ProcessIncomingMessageResult(bool AutoReplied, string? ReplyText);

public class ProcessIncomingMessageHandler
    : IRequestHandler<ProcessIncomingMessageCommand, ProcessIncomingMessageResult>
{
    // Preço aproximado por milhão de tokens (modelo Sonnet). Ajuste aqui se
    // trocar de modelo ou se os preços da Anthropic mudarem.
    private const decimal InputCostPerMillionTokens = 3.00m;
    private const decimal OutputCostPerMillionTokens = 15.00m;

    private readonly IApplicationDbContext _db;
    private readonly IAiAgentService _aiAgent;
    private readonly IWhatsAppGateway _whatsApp;
    private readonly ILogger<ProcessIncomingMessageHandler> _logger;
    private readonly INotificationService _notifications;
    private readonly ISecretProtector _secretProtector;

    public ProcessIncomingMessageHandler(
        IApplicationDbContext db,
        IAiAgentService aiAgent,
        IWhatsAppGateway whatsApp,
        ILogger<ProcessIncomingMessageHandler> logger,
        INotificationService notifications,
        ISecretProtector secretProtector)
    {
        _db = db;
        _aiAgent = aiAgent;
        _whatsApp = whatsApp;
        _logger = logger;
        _notifications = notifications;
        _secretProtector = secretProtector;
    }

    public async Task<ProcessIncomingMessageResult> Handle(
        ProcessIncomingMessageCommand request, CancellationToken ct)
    {
        // 0. Resolve qual número da empresa recebeu essa mensagem
        // IMPORTANTE: IgnoreQueryFilters() é necessário aqui porque o webhook não tem
        // usuário autenticado (não existe JWT nessa chamada). O filtro automático de
        // tenant (que normalmente isola os dados por usuário logado) ficaria comparando
        // com "null" e zeraria qualquer resultado. O TenantId já vem validado pela
        // própria URL do webhook (que só nós configuramos), então isso é seguro aqui.
        var whatsappConnection = await _db.WhatsAppConnections
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.TenantId == request.TenantId
                                       && w.InstanceName == request.InstanceName, ct);

        if (whatsappConnection is null)
        {
            _logger.LogWarning(
                "Mensagem descartada: nenhuma WhatsAppConnection encontrada para tenant={TenantId} instance={InstanceName}. " +
                "Isso normalmente significa que o número foi deletado/recriado e o webhook antigo ainda está configurado, " +
                "ou que o instanceName no banco não bate com o da URL do webhook.",
                request.TenantId, request.InstanceName);
            return new ProcessIncomingMessageResult(false, null);
        }

        // 1. Garante o contato
        var normalizedPhone = Domain.Common.PhoneNumberNormalizer.Normalize(request.FromPhoneNumber);
        var contact = await _db.Contacts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == request.TenantId
                                       && c.PhoneNumber == normalizedPhone, ct);

        if (contact is null)
        {
            contact = new Contact
            {
                TenantId = request.TenantId,
                PhoneNumber = normalizedPhone,
                Name = request.ContactName
            };
            _db.Contacts.Add(contact);
        }

        // Contatos criados antes dessa funcionalidade existir (ou cuja busca anterior
        // falhou) ainda não têm foto — tenta buscar de novo sempre que estiver faltando.
        if (string.IsNullOrEmpty(contact.ProfilePictureUrl))
        {
            try
            {
                contact.ProfilePictureUrl = await _whatsApp.GetProfilePictureUrlAsync(
                    whatsappConnection.InstanceName, normalizedPhone, ct);

                if (string.IsNullOrEmpty(contact.ProfilePictureUrl))
                {
                    _logger.LogInformation(
                        "Busca de foto de perfil retornou vazia para {Phone} (pode ser que o contato não tenha foto).",
                        normalizedPhone);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao buscar foto de perfil para {Phone}.", normalizedPhone);
            }
        }

        // 2. Garante a conversa aberta (ligada a esse contato E a esse número específico)
        var conversation = await _db.Conversations
            .IgnoreQueryFilters()
            .Include(c => c.Messages)
            .Where(c => c.ContactId == contact.Id
                        && c.WhatsAppConnectionId == whatsappConnection.Id
                        && c.Status != ConversationStatus.Closed)
            .OrderByDescending(c => c.LastMessageAtUtc)
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
        {
            conversation = new Conversation
            {
                TenantId = request.TenantId,
                Contact = contact,
                WhatsAppConnectionId = whatsappConnection.Id,
                Status = ConversationStatus.Open
            };
            _db.Conversations.Add(conversation);
        }

        // 3. Persiste a mensagem recebida
        var inboundMessage = new Message
        {
            TenantId = request.TenantId,
            Conversation = conversation,
            Content = request.MessageText,
            Direction = MessageDirection.Inbound,
            SentBy = MessageSender.Contact,
            WhatsAppMessageId = request.WhatsAppMessageId
        };
        _db.Messages.Add(inboundMessage);
        conversation.LastMessageAtUtc = DateTime.UtcNow;

        // 4. Busca config do agente de IA do tenant
        var agentConfig = await _db.AiAgentConfigs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.TenantId == request.TenantId, ct);

        if (agentConfig is null || !agentConfig.AutoReplyEnabled)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Mensagem recebida salva (sem auto-resposta: {Motivo}). Conversa={ConversationId}",
                agentConfig is null ? "sem AiAgentConfig" : "AutoReplyEnabled=false", conversation.Id);
            await _notifications.NotifyConversationUpdated(request.TenantId, conversation.Id);
            return new ProcessIncomingMessageResult(false, null);
        }

        // 4.5 Cada tenant usa a PRÓPRIA chave da Anthropic (custo sai da conta
        // dele, não da sua). Sem chave configurada, não tem como chamar a IA.
        if (string.IsNullOrEmpty(agentConfig.AnthropicApiKeyEncrypted))
        {
            conversation.Status = ConversationStatus.WaitingHuman;
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning(
                "Mensagem recebida salva, mas o tenant ainda não configurou a chave da Anthropic " +
                "(Configurações > Agente de IA). Conversa={ConversationId}", conversation.Id);
            await _notifications.NotifyConversationUpdated(request.TenantId, conversation.Id);
            return new ProcessIncomingMessageResult(false, null);
        }

        // 4.6 Limite de gasto auto-imposto pelo próprio tenant (opcional, é só
        // um teto de segurança pra ele não ser surpreendido pela fatura da
        // Anthropic — o dinheiro já é da conta dele, isso aqui não te protege,
        // protege ELE).
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == request.TenantId, ct);
        if (tenant is null || tenant.AiCreditsBalanceUsd <= 0m)
        {
            conversation.Status = ConversationStatus.WaitingHuman;
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning(
                "Mensagem recebida salva, mas o limite de gasto de IA do tenant chegou a zero (saldo={Saldo}). Conversa={ConversationId}",
                tenant?.AiCreditsBalanceUsd, conversation.Id);
            await _notifications.NotifyConversationUpdated(request.TenantId, conversation.Id);
            return new ProcessIncomingMessageResult(false, null);
        }

        var anthropicApiKey = _secretProtector.Decrypt(agentConfig.AnthropicApiKeyEncrypted);

        // 5. Monta histórico e chama a IA (Claude)
        var history = conversation.Messages
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => (
                role: m.Direction == MessageDirection.Inbound ? "user" : "assistant",
                content: m.Content))
            .ToList();
        history.Add(("user", request.MessageText));

        AiReplyResult aiResult;
        try
        {
            aiResult = await _aiAgent.GenerateReplyAsync(anthropicApiKey, agentConfig.SystemPrompt, history, ct);
        }
        catch (Exception ex)
        {
            // Se a IA falhar (ex: chave da Anthropic inválida/sem saldo na conta do tenant),
            // a mensagem AINDA PRECISA ser salva — só não vai ter resposta automática dessa vez.
            _logger.LogError(ex,
                "Falha ao chamar a IA para gerar resposta. A mensagem será salva mesmo assim, sem auto-resposta. Conversa={ConversationId}",
                conversation.Id);

            conversation.Status = ConversationStatus.WaitingHuman;
            await _db.SaveChangesAsync(ct);
            await _notifications.NotifyConversationUpdated(request.TenantId, conversation.Id);
            return new ProcessIncomingMessageResult(false, null);
        }

        conversation.LastDetectedIntent = aiResult.DetectedIntent;

        // Desconta o custo real (baseado nos tokens usados) do saldo do tenant,
        // e registra o consumo para o histórico/auditoria.
        var costUsd = (aiResult.InputTokens / 1_000_000m) * InputCostPerMillionTokens
                     + (aiResult.OutputTokens / 1_000_000m) * OutputCostPerMillionTokens;
        tenant.AiCreditsBalanceUsd = Math.Max(0, tenant.AiCreditsBalanceUsd - costUsd);
        _db.AiUsageLogs.Add(new AiUsageLog
        {
            TenantId = request.TenantId,
            ConversationId = conversation.Id,
            InputTokens = aiResult.InputTokens,
            OutputTokens = aiResult.OutputTokens,
            CostUsd = costUsd
        });

        // 6. Se precisa de aprovação humana, apenas registra sugestão e para por aqui.
        //    (No painel, o agente humano aprova e um outro comando dispara o envio.)
        if (agentConfig.RequireHumanApproval || aiResult.ShouldEscalateToHuman)
        {
            conversation.Status = ConversationStatus.WaitingHuman;
            conversation.PendingAiSuggestion = aiResult.ReplyText;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Mensagem recebida salva, aguardando aprovação humana. Conversa={ConversationId}", conversation.Id);
            await _notifications.NotifyConversationUpdated(request.TenantId, conversation.Id);
            return new ProcessIncomingMessageResult(false, aiResult.ReplyText);
        }

        // 7. Envia a resposta automaticamente
        try
        {
            await _whatsApp.SendTextMessageAsync(
                whatsappConnection.InstanceName, request.FromPhoneNumber, aiResult.ReplyText, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao enviar a resposta automática pelo WhatsApp. A mensagem recebida será salva mesmo assim. Conversa={ConversationId}",
                conversation.Id);

            conversation.Status = ConversationStatus.WaitingHuman;
            await _db.SaveChangesAsync(ct);
            await _notifications.NotifyConversationUpdated(request.TenantId, conversation.Id);
            return new ProcessIncomingMessageResult(false, aiResult.ReplyText);
        }

        _db.Messages.Add(new Message
        {
            TenantId = request.TenantId,
            Conversation = conversation,
            Content = aiResult.ReplyText,
            Direction = MessageDirection.Outbound,
            SentBy = MessageSender.AiAgent,
            AiGenerated = true
        });

        await _db.SaveChangesAsync(ct);
        await _notifications.NotifyConversationUpdated(request.TenantId, conversation.Id);
        return new ProcessIncomingMessageResult(true, aiResult.ReplyText);
    }
}
