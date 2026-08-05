using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.Proposals;

public record UpdateProposalCommand(
    Guid ProposalId, string Title, string Description, decimal? Value) : IRequest<bool>;

public class UpdateProposalHandler : IRequestHandler<UpdateProposalCommand, bool>
{
    private readonly IApplicationDbContext _db;
    public UpdateProposalHandler(IApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateProposalCommand request, CancellationToken ct)
    {
        var proposal = await _db.Proposals.FirstOrDefaultAsync(p => p.Id == request.ProposalId, ct);
        if (proposal is null) return false;

        proposal.Title = request.Title;
        proposal.Description = request.Description;
        proposal.Value = request.Value;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}

/// <summary>
/// Manda a proposta pro cliente pelo WhatsApp (usa o número da conversa
/// original, se existir) e marca como enviada.
/// </summary>
public record SendProposalCommand(Guid ProposalId) : IRequest<(bool Success, string? Error)>;

public class SendProposalHandler : IRequestHandler<SendProposalCommand, (bool Success, string? Error)>
{
    private readonly IApplicationDbContext _db;
    private readonly IWhatsAppGateway _whatsApp;
    private readonly INotificationService _notifications;
    private readonly ICurrentTenantService _currentTenant;

    public SendProposalHandler(
        IApplicationDbContext db, IWhatsAppGateway whatsApp, INotificationService notifications,
        ICurrentTenantService currentTenant)
    {
        _db = db;
        _whatsApp = whatsApp;
        _notifications = notifications;
        _currentTenant = currentTenant;
    }

    public async Task<(bool Success, string? Error)> Handle(SendProposalCommand request, CancellationToken ct)
    {
        var proposal = await _db.Proposals
            .Include(p => p.Contact)
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId, ct);
        if (proposal is null) return (false, "Proposta não encontrada.");

        Domain.Entities.Conversation? conversation = null;
        if (proposal.ConversationId is { } convId)
        {
            conversation = await _db.Conversations
                .Include(c => c.WhatsAppConnection)
                .FirstOrDefaultAsync(c => c.Id == convId, ct);
        }

        // Se a proposta não veio de uma conversa (ou ela sumiu), usa o primeiro
        // número conectado do tenant como alternativa.
        var connection = conversation?.WhatsAppConnection
            ?? await _db.WhatsAppConnections.FirstOrDefaultAsync(w => w.IsConnected, ct);

        if (connection is null) return (false, "Nenhum número de WhatsApp conectado pra enviar.");

        var messageText = BuildMessageText(proposal);

        try
        {
            await _whatsApp.SendTextMessageAsync(connection.InstanceName, proposal.Contact.PhoneNumber, messageText, ct);
        }
        catch (Exception ex)
        {
            return (false, $"Falha ao enviar pelo WhatsApp: {ex.Message}");
        }

        proposal.Status = ProposalStatus.SentToClient;
        proposal.SentAtUtc = DateTime.UtcNow;

        // Se tiver uma conversa associada, registra a proposta como mensagem também.
        if (conversation is not null)
        {
            conversation.LastMessageAtUtc = DateTime.UtcNow;
            _db.Messages.Add(new Message
            {
                TenantId = proposal.TenantId,
                ConversationId = conversation.Id,
                Content = messageText,
                Direction = MessageDirection.Outbound,
                SentBy = MessageSender.HumanAgent,
                AiGenerated = false
            });
        }

        await _db.SaveChangesAsync(ct);

        if (conversation is not null && _currentTenant.TenantId is { } tenantId)
            await _notifications.NotifyConversationUpdated(tenantId, conversation.Id);

        return (true, null);
    }

    private static string BuildMessageText(Proposal proposal)
    {
        var text = $"*{proposal.Title}*\n\n{proposal.Description}";
        if (proposal.Value is { } value)
            text += $"\n\n💰 Valor: R$ {value:N2}";
        return text;
    }
}

public record UpdateProposalStatusCommand(Guid ProposalId, ProposalStatus Status) : IRequest<bool>;

public class UpdateProposalStatusHandler : IRequestHandler<UpdateProposalStatusCommand, bool>
{
    private readonly IApplicationDbContext _db;
    public UpdateProposalStatusHandler(IApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateProposalStatusCommand request, CancellationToken ct)
    {
        var proposal = await _db.Proposals.FirstOrDefaultAsync(p => p.Id == request.ProposalId, ct);
        if (proposal is null) return false;

        proposal.Status = request.Status;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public record DeleteProposalCommand(Guid ProposalId) : IRequest<bool>;

public class DeleteProposalHandler : IRequestHandler<DeleteProposalCommand, bool>
{
    private readonly IApplicationDbContext _db;
    public DeleteProposalHandler(IApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteProposalCommand request, CancellationToken ct)
    {
        var proposal = await _db.Proposals.FirstOrDefaultAsync(p => p.Id == request.ProposalId, ct);
        if (proposal is null) return false;

        _db.Proposals.Remove(proposal);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
