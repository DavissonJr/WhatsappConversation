import { Contact } from './conversation.model';

export type ProposalStatus = 'Draft' | 'SentToClient' | 'Accepted' | 'Rejected' | 'Expired';

export interface Proposal {
  id: string;
  contact: Contact;
  conversationId?: string;
  title: string;
  description: string;
  value?: number;
  status: ProposalStatus;
  aiGenerated: boolean;
  sentAtUtc?: string;
  createdAtUtc: string;
}
