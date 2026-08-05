import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Appointment, AppointmentStatus } from '../models/appointment.model';

export interface CreateAppointmentPayload {
  whatsAppConnectionId: string;
  phoneNumber: string;
  contactName?: string;
  title: string;
  scheduledForUtc: string;
  notes?: string;
  reminderOffsetMinutes: number[];
  reminderMessageTemplate?: string;
}

export interface CreateAppointmentResponse {
  id: string;
  remindersScheduled: number;
  remindersSkippedPast: number;
}

@Injectable({ providedIn: 'root' })
export class AppointmentService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/appointments`;

  getAll(): Observable<Appointment[]> {
    return this.http.get<Appointment[]>(this.baseUrl);
  }

  create(payload: CreateAppointmentPayload): Observable<CreateAppointmentResponse> {
    return this.http.post<CreateAppointmentResponse>(this.baseUrl, payload);
  }

  updateStatus(id: string, status: AppointmentStatus): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/status`, { status });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
