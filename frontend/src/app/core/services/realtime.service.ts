import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private auth = inject(AuthService);
  private connection?: signalR.HubConnection;

  /** Emite o id da conversa que mudou (nova mensagem, status, etc). */
  conversationUpdated$ = new Subject<string>();

  connect(): void {
    if (this.connection) return; // já conectado

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/conversations`, {
        accessTokenFactory: () => this.auth.getToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('conversationUpdated', (conversationId: string) => {
      this.conversationUpdated$.next(conversationId);
    });

    this.connection.start().catch((err) => {
      // Não é fatal: o polling de fallback no Inbox continua funcionando mesmo sem SignalR.
      console.warn('Não foi possível conectar ao SignalR, seguindo só com polling.', err);
    });
  }

  disconnect(): void {
    this.connection?.stop();
    this.connection = undefined;
  }
}
