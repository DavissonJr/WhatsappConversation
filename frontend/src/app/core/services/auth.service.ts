import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface RegisterPayload {
  companyName: string;
  segment: string;
  fullName: string;
  email: string;
  password: string;
}

export interface LoginPayload {
  email: string;
  password: string;
}

interface AuthResponse {
  token: string;
}

interface RegisterResponse {
  requiresVerification: boolean;
}

interface DecodedToken {
  name?: string;
  email?: string;
  tenant_id?: string;
  role?: string;
  platform_admin?: string;
  exp?: number;
}

const TOKEN_KEY = 'wcia_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private baseUrl = `${environment.apiUrl}/api/auth`;

  currentUserName = signal<string | null>(null);
  isAuthenticated = signal<boolean>(false);
  isPlatformAdmin = signal<boolean>(false);

  constructor() {
    this.hydrateFromStorage();
  }

  /** Passo 1: manda os dados, um código de 6 dígitos chega no e-mail. */
  register(payload: RegisterPayload): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.baseUrl}/register`, payload);
  }

  /** Passo 2: confirma o código — só aqui a conta é criada de verdade. */
  verifyRegistration(email: string, code: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/verify-registration`, { email, code }).pipe(
      tap((res) => this.setSession(res.token)),
    );
  }

  resendCode(email: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/resend-code`, { email });
  }

  login(payload: LoginPayload): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, payload).pipe(
      tap((res) => this.setSession(res.token)),
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.isAuthenticated.set(false);
    this.currentUserName.set(null);
    this.isPlatformAdmin.set(false);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private setSession(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
    this.applyToken(token);
  }

  private hydrateFromStorage(): void {
    const token = this.getToken();
    if (token) this.applyToken(token);
  }

  private applyToken(token: string): void {
    const decoded = this.decode(token);
    if (!decoded) return;

    if (decoded.exp && decoded.exp * 1000 < Date.now()) {
      this.logout();
      return;
    }

    this.currentUserName.set(decoded.name ?? decoded.email ?? 'Usuário');
    this.isAuthenticated.set(true);
    this.isPlatformAdmin.set(decoded.platform_admin === 'true');
  }

  private decode(token: string): DecodedToken | null {
    try {
      const payload = token.split('.')[1];
      const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(json);
    } catch {
      return null;
    }
  }
}
