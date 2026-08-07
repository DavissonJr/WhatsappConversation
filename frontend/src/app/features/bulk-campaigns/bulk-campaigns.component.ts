import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription, interval } from 'rxjs';
import { BulkCampaignService } from '../../core/services/bulk-campaign.service';
import { WhatsAppConnectionService } from '../../core/services/whatsapp-connection.service';
import { MessageTemplateService } from '../../core/services/message-template.service';
import { ToastService } from '../../core/services/toast.service';
import { BulkCampaignSummary } from '../../core/models/bulk-campaign.model';
import { WhatsAppConnection } from '../../core/models/whatsapp-connection.model';
import { MessageTemplate } from '../../core/models/message-template.model';

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Na fila',
  Running: 'Enviando...',
  Completed: 'Concluída',
  Cancelled: 'Cancelada',
};

const POLL_INTERVAL_MS = 5000;

@Component({
  selector: 'app-bulk-campaigns',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bulk-campaigns.component.html',
  styleUrl: './bulk-campaigns.component.scss',
})
export class BulkCampaignsComponent implements OnInit, OnDestroy {
  private service = inject(BulkCampaignService);
  private connectionService = inject(WhatsAppConnectionService);
  private templateService = inject(MessageTemplateService);
  private toast = inject(ToastService);
  private pollSub?: Subscription;

  statusLabels = STATUS_LABELS;

  campaigns = signal<BulkCampaignSummary[]>([]);
  connections = signal<WhatsAppConnection[]>([]);
  templates = signal<MessageTemplate[]>([]);

  // Formulário
  title = signal('');
  messageText = signal('');
  connectionId = signal('');
  delaySeconds = signal(8);

  excludeBlocked = signal(true);
  useNoAppointmentFilter = signal(false);
  noAppointmentDays = signal(30);
  useNoConversationFilter = signal(false);
  noConversationDays = signal(30);
  searchTerm = signal('');

  previewCount = signal<number | null>(null);
  previewing = signal(false);
  creating = signal(false);
  showTemplatePicker = signal(false);

  ngOnInit(): void {
    this.load();
    this.connectionService.getAll().subscribe((data) => {
      this.connections.set(data);
      if (data.length) this.connectionId.set(data[0].id);
    });
    this.templateService.getAll().subscribe((data) => this.templates.set(data.filter((t) => t.isActive)));

    // Enquanto tiver campanha rodando, atualiza sozinho pra mostrar o progresso.
    this.pollSub = interval(POLL_INTERVAL_MS).subscribe(() => {
      if (this.campaigns().some((c) => c.status === 'Running' || c.status === 'Pending')) {
        this.load();
      }
    });
  }

  ngOnDestroy(): void {
    this.pollSub?.unsubscribe();
  }

  load(): void {
    this.service.getAll().subscribe({
      next: (data) => this.campaigns.set(data),
      error: () => this.toast.error('Não foi possível carregar as campanhas.'),
    });
  }

  private buildFilters() {
    return {
      excludeBlocked: this.excludeBlocked(),
      noAppointmentInLastDays: this.useNoAppointmentFilter() ? this.noAppointmentDays() : undefined,
      noConversationInLastDays: this.useNoConversationFilter() ? this.noConversationDays() : undefined,
      searchTerm: this.searchTerm() || undefined,
    };
  }

  runPreview(): void {
    this.previewing.set(true);
    this.previewCount.set(null);
    this.service.preview(this.buildFilters()).subscribe({
      next: (res) => {
        this.previewing.set(false);
        this.previewCount.set(res.count);
      },
      error: () => {
        this.previewing.set(false);
        this.toast.error('Não foi possível calcular o público.');
      },
    });
  }

  useTemplate(template: MessageTemplate): void {
    this.messageText.set(template.content);
    this.showTemplatePicker.set(false);
  }

  startCampaign(): void {
    if (!this.title().trim()) {
      this.toast.error('Dê um nome pra campanha (é só pra você identificar depois).');
      return;
    }
    if (!this.messageText().trim()) {
      this.toast.error('Escreva a mensagem que vai ser enviada.');
      return;
    }
    if (!this.connectionId()) {
      this.toast.error('Conecte um número de WhatsApp antes.');
      return;
    }

    const count = this.previewCount();
    if (count === null) {
      this.toast.error('Clique em "Ver quantos contatos" antes de iniciar.');
      return;
    }
    if (count === 0) {
      this.toast.error('Nenhum contato bate com esses filtros.');
      return;
    }

    if (!confirm(
      `Confirma o envio de "${this.title()}" para ${count} contato(s)? ` +
      `Com ${this.delaySeconds()}s de intervalo, isso leva aproximadamente ${this.estimateMinutes(count)} minutos.`,
    )) return;

    this.creating.set(true);
    this.service
      .create({
        title: this.title(),
        messageText: this.messageText(),
        whatsAppConnectionId: this.connectionId(),
        delaySeconds: this.delaySeconds(),
        filters: this.buildFilters(),
      })
      .subscribe({
        next: () => {
          this.creating.set(false);
          this.toast.success('Campanha iniciada! Acompanhe o progresso na lista abaixo.');
          this.resetForm();
          this.load();
        },
        error: (err) => {
          this.creating.set(false);
          this.toast.error(err?.error?.message ?? 'Não foi possível iniciar a campanha.');
        },
      });
  }

  estimateMinutes(count: number): number {
    return Math.ceil((count * this.delaySeconds()) / 60);
  }

  resetForm(): void {
    this.title.set('');
    this.messageText.set('');
    this.previewCount.set(null);
    this.useNoAppointmentFilter.set(false);
    this.useNoConversationFilter.set(false);
    this.searchTerm.set('');
  }

  cancelCampaign(campaign: BulkCampaignSummary): void {
    if (!confirm(`Cancelar a campanha "${campaign.title}"? Quem ainda não recebeu, não vai receber.`)) return;

    this.service.cancel(campaign.id).subscribe({
      next: () => {
        this.toast.success('Campanha cancelada.');
        this.load();
      },
      error: () => this.toast.error('Não foi possível cancelar.'),
    });
  }

  progressPercent(campaign: BulkCampaignSummary): number {
    if (campaign.totalRecipients === 0) return 0;
    return Math.round(((campaign.sentCount + campaign.failedCount) / campaign.totalRecipients) * 100);
  }
}
