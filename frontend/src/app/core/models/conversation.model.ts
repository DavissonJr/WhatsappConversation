export interface Contact {
  id: string;
  name?: string;
  phoneNumber: string;
  profilePictureUrl?: string;
}

export type ConversationStatus = 'Open' | 'WaitingHuman' | 'Closed';

export interface Message {
  id: string;
  content: string;
  direction: 'Inbound' | 'Outbound';
  sentBy: 'Contact' | 'AiAgent' | 'HumanAgent' | 'System';
  aiGenerated: boolean;
  createdAtUtc: string;
}

export interface ConversationSummary {
  id: string;
  contact: Contact;
  status: ConversationStatus;
  lastMessageAtUtc: string;
  lastMessagePreview?: string;
}

export interface Conversation {
  id: string;
  contact: Contact;
  status: ConversationStatus;
  lastMessageAtUtc: string;
  messages: Message[];
}
