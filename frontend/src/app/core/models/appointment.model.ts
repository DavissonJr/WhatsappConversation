import { Contact } from './conversation.model';

export type AppointmentStatus = 'Scheduled' | 'Confirmed' | 'Cancelled' | 'Completed' | 'NoShow';

export interface Reminder {
  id: string;
  sendAtUtc: string;
  status: string;
}

export interface Appointment {
  id: string;
  contact: Contact;
  whatsAppConnectionLabel: string;
  title: string;
  scheduledForUtc: string;
  status: AppointmentStatus;
  notes?: string;
  reminders: Reminder[];
}
