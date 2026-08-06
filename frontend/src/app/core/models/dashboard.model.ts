export interface DailyCount {
  date: string;
  count: number;
}

export interface DailyMessageCount {
  date: string;
  inbound: number;
  outbound: number;
}

export interface ConversationStatusCounts {
  open: number;
  waitingHuman: number;
  closed: number;
}

export interface AppointmentStatusCounts {
  scheduled: number;
  confirmed: number;
  completed: number;
  cancelled: number;
  noShow: number;
}

export interface ProposalStatusCounts {
  draft: number;
  sentToClient: number;
  accepted: number;
  rejected: number;
  expired: number;
}

export interface DashboardSummary {
  totalContacts: number;
  newContactsLast7Days: number;
  newContactsByDay: DailyCount[];
  messagesByDay: DailyMessageCount[];
  conversationsByStatus: ConversationStatusCounts;
  appointmentsByStatus: AppointmentStatusCounts;
  upcomingAppointmentsCount: number;
  proposalsByStatus: ProposalStatusCounts;
  proposalConversionRatePercent: number;
  aiAutoReplyRatePercent: number;
}
