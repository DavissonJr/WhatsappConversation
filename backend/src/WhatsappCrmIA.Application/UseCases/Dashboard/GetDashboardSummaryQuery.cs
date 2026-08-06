using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.Dashboard;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public class GetDashboardSummaryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private const int TrendDays = 14;

    private readonly IApplicationDbContext _db;
    public GetDashboardSummaryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var trendStart = now.Date.AddDays(-(TrendDays - 1));
        var last30Start = now.Date.AddDays(-29);

        // ---- Contatos ----
        var totalContacts = await _db.Contacts.CountAsync(ct);
        var newContactsLast7Days = await _db.Contacts.CountAsync(c => c.CreatedAtUtc >= now.AddDays(-7), ct);

        var contactDates = await _db.Contacts
            .Where(c => c.CreatedAtUtc >= trendStart)
            .Select(c => c.CreatedAtUtc.Date)
            .ToListAsync(ct);
        var newContactsByDay = BuildDailyCountSeries(contactDates, trendStart, now.Date);

        // ---- Mensagens (últimos 14 dias, entrada vs saída) ----
        var messageRows = await _db.Messages
            .Where(m => m.CreatedAtUtc >= trendStart)
            .Select(m => new { Date = m.CreatedAtUtc.Date, m.Direction })
            .ToListAsync(ct);
        var messagesByDay = BuildDailyMessageSeries(
            messageRows.Select(m => (m.Date, m.Direction)).ToList(), trendStart, now.Date);

        // ---- Conversas por status ----
        var conversationCounts = await _db.Conversations
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var conversationsByStatus = new ConversationStatusCountsDto(
            conversationCounts.FirstOrDefault(x => x.Status == ConversationStatus.Open)?.Count ?? 0,
            conversationCounts.FirstOrDefault(x => x.Status == ConversationStatus.WaitingHuman)?.Count ?? 0,
            conversationCounts.FirstOrDefault(x => x.Status == ConversationStatus.Closed)?.Count ?? 0);

        // ---- Agendamentos por status + próximos 7 dias ----
        var appointmentCounts = await _db.Appointments
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var appointmentsByStatus = new AppointmentStatusCountsDto(
            appointmentCounts.FirstOrDefault(x => x.Status == AppointmentStatus.Scheduled)?.Count ?? 0,
            appointmentCounts.FirstOrDefault(x => x.Status == AppointmentStatus.Confirmed)?.Count ?? 0,
            appointmentCounts.FirstOrDefault(x => x.Status == AppointmentStatus.Completed)?.Count ?? 0,
            appointmentCounts.FirstOrDefault(x => x.Status == AppointmentStatus.Cancelled)?.Count ?? 0,
            appointmentCounts.FirstOrDefault(x => x.Status == AppointmentStatus.NoShow)?.Count ?? 0);

        var upcomingAppointmentsCount = await _db.Appointments.CountAsync(a =>
            a.ScheduledForUtc >= now && a.ScheduledForUtc <= now.AddDays(7) &&
            (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed), ct);

        // ---- Propostas por status + taxa de conversão ----
        var proposalCounts = await _db.Proposals
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var proposalsByStatus = new ProposalStatusCountsDto(
            proposalCounts.FirstOrDefault(x => x.Status == ProposalStatus.Draft)?.Count ?? 0,
            proposalCounts.FirstOrDefault(x => x.Status == ProposalStatus.SentToClient)?.Count ?? 0,
            proposalCounts.FirstOrDefault(x => x.Status == ProposalStatus.Accepted)?.Count ?? 0,
            proposalCounts.FirstOrDefault(x => x.Status == ProposalStatus.Rejected)?.Count ?? 0,
            proposalCounts.FirstOrDefault(x => x.Status == ProposalStatus.Expired)?.Count ?? 0);

        var decidedProposals = proposalsByStatus.SentToClient + proposalsByStatus.Accepted + proposalsByStatus.Rejected;
        var conversionRate = decidedProposals > 0
            ? Math.Round(proposalsByStatus.Accepted * 100m / decidedProposals, 1)
            : 0m;

        // ---- Taxa de resposta automática da IA (últimos 30 dias) ----
        var outboundLast30Days = await _db.Messages
            .Where(m => m.CreatedAtUtc >= last30Start && m.Direction == MessageDirection.Outbound)
            .Select(m => m.AiGenerated)
            .ToListAsync(ct);
        var aiAutoReplyRate = outboundLast30Days.Count > 0
            ? Math.Round(outboundLast30Days.Count(x => x) * 100m / outboundLast30Days.Count, 1)
            : 0m;

        return new DashboardSummaryDto(
            totalContacts, newContactsLast7Days, newContactsByDay, messagesByDay,
            conversationsByStatus, appointmentsByStatus, upcomingAppointmentsCount,
            proposalsByStatus, conversionRate, aiAutoReplyRate);
    }

    private static List<DailyCountDto> BuildDailyCountSeries(List<DateTime> dates, DateTime start, DateTime end)
    {
        var counts = dates.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
        var result = new List<DailyCountDto>();
        for (var day = start; day <= end; day = day.AddDays(1))
            result.Add(new DailyCountDto(day, counts.GetValueOrDefault(day, 0)));
        return result;
    }

    private static List<DailyMessageCountDto> BuildDailyMessageSeries(
        List<(DateTime Date, MessageDirection Direction)> rows, DateTime start, DateTime end)
    {
        var byDay = new Dictionary<DateTime, (int In, int Out)>();
        foreach (var row in rows)
        {
            var current = byDay.GetValueOrDefault(row.Date, (0, 0));
            byDay[row.Date] = row.Direction == MessageDirection.Inbound
                ? (current.Item1 + 1, current.Item2)
                : (current.Item1, current.Item2 + 1);
        }

        var result = new List<DailyMessageCountDto>();
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            var counts = byDay.GetValueOrDefault(day, (0, 0));
            result.Add(new DailyMessageCountDto(day, counts.Item1, counts.Item2));
        }
        return result;
    }
}
