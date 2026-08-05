import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Conversation, ConversationSummary } from '../models/conversation.model';

@Injectable({ providedIn: 'root' })
export class ConversationService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/conversations`;

  getAll(): Observable<ConversationSummary[]> {
    return this.http.get<ConversationSummary[]>(this.baseUrl);
  }

  getById(id: string): Observable<Conversation> {
    return this.http.get<Conversation>(`${this.baseUrl}/${id}`);
  }

  sendMessage(conversationId: string, content: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${conversationId}/messages`, { content });
  }

  startConversation(payload: {
    whatsAppConnectionId: string;
    phoneNumber: string;
    contactName?: string;
    content: string;
  }): Observable<{ conversationId: string }> {
    return this.http.post<{ conversationId: string }>(`${this.baseUrl}/start`, payload);
  }
}
