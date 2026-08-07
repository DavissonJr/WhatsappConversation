using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;

namespace WhatsappCrmIA.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentTenantService _currentTenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantService currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AiAgentConfig> AiAgentConfigs => Set<AiAgentConfig>();
    public DbSet<WhatsAppConnection> WhatsAppConnections => Set<WhatsAppConnection>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<AiUsageLog> AiUsageLogs => Set<AiUsageLog>();
    public DbSet<PendingRegistration> PendingRegistrations => Set<PendingRegistration>();
    public DbSet<BulkMessageCampaign> BulkMessageCampaigns => Set<BulkMessageCampaign>();
    public DbSet<BulkMessageRecipient> BulkMessageRecipients => Set<BulkMessageRecipient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Isolamento multi-tenant: todo select automaticamente filtra pelo tenant atual.
        // IMPORTANTE: comandos administrativos/cross-tenant precisam usar IgnoreQueryFilters().
        // Users NÃO tem filtro: o login precisa localizar o usuário pelo e-mail
        // antes mesmo de sabermos qual é o tenant atual.
        modelBuilder.Entity<AiAgentConfig>().HasQueryFilter(e => e.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<WhatsAppConnection>().HasQueryFilter(e => e.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<Contact>().HasQueryFilter(e => e.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<Conversation>().HasQueryFilter(e => e.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<Message>().HasQueryFilter(e => e.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<Proposal>().HasQueryFilter(e => e.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => e.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<Reminder>().HasQueryFilter(e => e.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<MessageTemplate>().HasQueryFilter(e => e.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<AiUsageLog>().HasQueryFilter(e => e.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<BulkMessageCampaign>().HasQueryFilter(e => e.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<BulkMessageRecipient>().HasQueryFilter(e => e.TenantId == _currentTenant.TenantId);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty
                && _currentTenant.TenantId.HasValue)
            {
                entry.Entity.TenantId = _currentTenant.TenantId.Value;
            }
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(ct);
    }
}
