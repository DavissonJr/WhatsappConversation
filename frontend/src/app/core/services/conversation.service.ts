import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Conversation } from '../models/conversation.model';

@Injectable({ providedIn: 'root' })
export class ConversationService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/conversations`;

  getAll(): Observable<Conversation[]> {
    return this.http.get<Conversation[]>(this.baseUrl);
  }

  getById(id: string): Observable<Conversation> {
    return this.http.get<Conversation>(`${this.baseUrl}/${id}`);
  }

  sendMessage(conversationId: string, content: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${conversationId}/messages`, { content });
  }
}
