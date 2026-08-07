using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Domain.Entities;

namespace WhatsappCrmIA.Application.Interfaces;

/// <summary>
/// Abstração do DbContext exposta para a camada Application (Infrastructure implementa).
/// Mantém a Application livre de dependência direta do EF Core provider.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<AiAgentConfig> AiAgentConfigs { get; }
    DbSet<WhatsAppConnection> WhatsAppConnections { get; }
    DbSet<Contact> Contacts { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }
    DbSet<Proposal> Proposals { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<Reminder> Reminders { get; }
    DbSet<MessageTemplate> MessageTemplates { get; }
    DbSet<AiUsageLog> AiUsageLogs { get; }
    DbSet<PendingRegistration> PendingRegistrations { get; }
    DbSet<BulkMessageCampaign> BulkMessageCampaigns { get; }
    DbSet<BulkMessageRecipient> BulkMessageRecipients { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
