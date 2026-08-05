import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { OnboardingService } from '../../core/services/onboarding.service';
import { OnboardingStatus } from '../../core/models/onboarding.model';

@Component({
  selector: 'app-onboarding-checklist',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    @if (status(); as s) {
      @if (!allDone(s) && !dismissed()) {
        <div class="onboarding-card">
          <div class="onboarding-header">
            <div>
              <h3>Primeiros passos</h3>
              <p>Faltam {{ remainingCount(s) }} passo(s) pra sua IA começar a atender sozinha.</p>
            </div>
            <button class="dismiss-btn" (click)="dismiss()" title="Esconder por agora">✕</button>
          </div>

          <div class="steps">
            <a class="step" [class.done]="s.hasConnectedWhatsApp" routerLink="/numeros">
              <span class="step-check">{{ s.hasConnectedWhatsApp ? '✓' : '1' }}</span>
              <div class="step-text">
                <strong>Conectar um número de WhatsApp</strong>
                <span>Escaneie o QR code pra sua empresa começar a receber mensagens.</span>
              </div>
            </a>

            <a class="step" [class.done]="s.hasAnthropicApiKey" routerLink="/configuracoes">
              <span class="step-check">{{ s.hasAnthropicApiKey ? '✓' : '2' }}</span>
              <div class="step-text">
                <strong>Configurar a chave da Anthropic</strong>
                <span>Em Configurações → Agente de IA. Sem isso, a IA não gera respostas.</span>
              </div>
            </a>

            <a class="step" [class.done]="s.hasSentOrReceivedMessage" routerLink="/inbox">
              <span class="step-check">{{ s.hasSentOrReceivedMessage ? '✓' : '3' }}</span>
              <div class="step-text">
                <strong>Mandar sua primeira mensagem</strong>
                <span>Teste enviando ou recebendo uma mensagem de verdade.</span>
              </div>
            </a>
          </div>
        </div>
      }
    }
  `,
  styleUrl: './onboarding-checklist.component.scss',
})
export class OnboardingChecklistComponent implements OnInit {
  private service = inject(OnboardingService);
  private router = inject(Router);

  status = signal<OnboardingStatus | null>(null);
  dismissed = signal(false);

  ngOnInit(): void {
    this.dismissed.set(sessionStorage.getItem('onboarding_dismissed') === 'true');
    this.load();

    // Recarrega o status toda vez que o usuário navega, pra checklist
    // atualizar sozinha assim que o passo for concluído.
    this.router.events.subscribe(() => this.load());
  }

  load(): void {
    this.service.getStatus().subscribe((data) => this.status.set(data));
  }

  allDone(s: OnboardingStatus): boolean {
    return s.hasConnectedWhatsApp && s.hasAnthropicApiKey && s.hasSentOrReceivedMessage;
  }

  remainingCount(s: OnboardingStatus): number {
    return [s.hasConnectedWhatsApp, s.hasAnthropicApiKey, s.hasSentOrReceivedMessage]
      .filter((done) => !done).length;
  }

  dismiss(): void {
    sessionStorage.setItem('onboarding_dismissed', 'true');
    this.dismissed.set(true);
  }
}
