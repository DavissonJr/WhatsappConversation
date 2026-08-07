import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BulkAudienceFilters, BulkCampaignSummary } from '../models/bulk-campaign.model';

export interface CreateBulkCampaignPayload {
  title: string;
  messageText: string;
  whatsAppConnectionId: string;
  delaySeconds: number;
  filters: BulkAudienceFilters;
}

@Injectable({ providedIn: 'root' })
export class BulkCampaignService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/bulk-campaigns`;

  getAll(): Observable<BulkCampaignSummary[]> {
    return this.http.get<BulkCampaignSummary[]>(this.baseUrl);
  }

  preview(filters: BulkAudienceFilters): Observable<{ count: number }> {
    return this.http.post<{ count: number }>(`${this.baseUrl}/preview`, { filters });
  }

  create(payload: CreateBulkCampaignPayload): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, payload);
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, {});
  }
}
