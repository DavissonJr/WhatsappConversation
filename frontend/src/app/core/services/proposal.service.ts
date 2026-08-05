import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Proposal, ProposalStatus } from '../models/proposal.model';

@Injectable({ providedIn: 'root' })
export class ProposalService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/proposals`;

  getAll(): Observable<Proposal[]> {
    return this.http.get<Proposal[]>(this.baseUrl);
  }

  generate(conversationId: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/generate`, { conversationId });
  }

  update(id: string, payload: { title: string; description: string; value?: number }): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  send(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/send`, {});
  }

  updateStatus(id: string, status: ProposalStatus): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/status`, { status });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
