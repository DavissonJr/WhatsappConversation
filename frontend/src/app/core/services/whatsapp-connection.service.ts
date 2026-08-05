import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WhatsAppConnection } from '../models/whatsapp-connection.model';

@Injectable({ providedIn: 'root' })
export class WhatsAppConnectionService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/whatsapp-connections`;

  getAll(): Observable<WhatsAppConnection[]> {
    return this.http.get<WhatsAppConnection[]>(this.baseUrl);
  }

  create(label: string): Observable<WhatsAppConnection> {
    return this.http.post<WhatsAppConnection>(this.baseUrl, { label });
  }

  getQrCode(id: string): Observable<{ qrCodeBase64: string }> {
    return this.http.get<{ qrCodeBase64: string }>(`${this.baseUrl}/${id}/qrcode`);
  }

  refreshStatus(id: string): Observable<{ isConnected: boolean }> {
    return this.http.post<{ isConnected: boolean }>(`${this.baseUrl}/${id}/refresh-status`, {});
  }
}
