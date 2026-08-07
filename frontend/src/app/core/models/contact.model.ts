export interface ContactListItem {
  id: string;
  name?: string;
  phoneNumber: string;
  profilePictureUrl?: string;
  notes?: string;
  isBlocked: boolean;
  createdAtUtc: string;
  lastActivityUtc?: string;
  conversationCount: number;
  appointmentCount: number;
  proposalCount: number;
}
