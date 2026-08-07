import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ContactListItem } from '../models/contact.model';

@Injectable({ providedIn: 'root' })
export class ContactService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/contacts`;

  getAll(search?: string): Observable<ContactListItem[]> {
    const params = search ? { search } : undefined;
    return this.http.get<ContactListItem[]>(this.baseUrl, { params });
  }

  update(id: string, payload: { name?: string; notes?: string; isBlocked: boolean }): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }
}
