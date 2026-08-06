namespace WhatsappCrmIA.Application.DTOs;

public record DailyCountDto(DateTime Date, int Count);
public record DailyMessageCountDto(DateTime Date, int Inbound, int Outbound);

public record ConversationStatusCountsDto(int Open, int WaitingHuman, int Closed);
public record AppointmentStatusCountsDto(int Scheduled, int Confirmed, int Completed, int Cancelled, int NoShow);
public record ProposalStatusCountsDto(int Draft, int SentToClient, int Accepted, int Rejected, int Expired);

public record DashboardSummaryDto(
    int TotalContacts,
    int NewContactsLast7Days,
    IReadOnlyList<DailyCountDto> NewContactsByDay,
    IReadOnlyList<DailyMessageCountDto> MessagesByDay,
    ConversationStatusCountsDto ConversationsByStatus,
    AppointmentStatusCountsDto AppointmentsByStatus,
    int UpcomingAppointmentsCount,
    ProposalStatusCountsDto ProposalsByStatus,
    decimal ProposalConversionRatePercent,
    decimal AiAutoReplyRatePercent);
