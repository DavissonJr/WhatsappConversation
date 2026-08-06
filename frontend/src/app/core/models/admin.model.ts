export interface AdminTenantSummary {
  id: string;
  name: string;
  segment: string;
  plan: string;
  isActive: boolean;
  createdAtUtc: string;
  ownerName?: string;
  ownerEmail?: string;
  userCount: number;
  whatsAppConnectionCount: number;
  connectedWhatsAppCount: number;
  contactCount: number;
  conversationCount: number;
  messageCount: number;
  appointmentCount: number;
  proposalCount: number;
  totalAiInputTokens: number;
  totalAiOutputTokens: number;
  totalAiEstimatedCostUsd: number;
  lastActivityUtc?: string;
}
