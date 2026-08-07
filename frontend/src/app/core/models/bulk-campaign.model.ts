export interface BulkAudienceFilters {
  excludeBlocked: boolean;
  noAppointmentInLastDays?: number;
  noConversationInLastDays?: number;
  searchTerm?: string;
}

export type BulkCampaignStatus = 'Pending' | 'Running' | 'Completed' | 'Cancelled';

export interface BulkCampaignSummary {
  id: string;
  title: string;
  messageText: string;
  whatsAppConnectionLabel: string;
  status: BulkCampaignStatus;
  delaySeconds: number;
  totalRecipients: number;
  sentCount: number;
  failedCount: number;
  createdAtUtc: string;
  startedAtUtc?: string;
  completedAtUtc?: string;
}
