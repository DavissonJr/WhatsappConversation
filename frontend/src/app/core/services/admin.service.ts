import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminTenantSummary } from '../models/admin.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/admin`;

  getTenants(): Observable<AdminTenantSummary[]> {
    return this.http.get<AdminTenantSummary[]>(`${this.baseUrl}/tenants`);
  }

  setTenantActive(id: string, isActive: boolean): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/tenants/${id}/active`, { isActive });
  }
}
