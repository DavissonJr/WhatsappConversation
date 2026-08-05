import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { OnboardingStatus } from '../models/onboarding.model';

@Injectable({ providedIn: 'root' })
export class OnboardingService {
  private http = inject(HttpClient);

  getStatus(): Observable<OnboardingStatus> {
    return this.http.get<OnboardingStatus>(`${environment.apiUrl}/api/onboarding/status`);
  }
}
