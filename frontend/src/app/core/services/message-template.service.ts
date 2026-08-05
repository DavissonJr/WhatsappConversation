import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MessageTemplate, TemplateScope } from '../models/message-template.model';

export interface UpsertTemplatePayload {
  name: string;
  scope: TemplateScope;
  content: string;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class MessageTemplateService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/message-templates`;

  getAll(): Observable<MessageTemplate[]> {
    return this.http.get<MessageTemplate[]>(this.baseUrl);
  }

  create(payload: UpsertTemplatePayload): Observable<MessageTemplate> {
    return this.http.post<MessageTemplate>(this.baseUrl, payload);
  }

  update(id: string, payload: UpsertTemplatePayload): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
