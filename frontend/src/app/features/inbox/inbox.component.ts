import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConversationService } from '../../core/services/conversation.service';
import { WhatsAppConnectionService } from '../../core/services/whatsapp-connection.service';
import { MessageTemplateService } from '../../core/services/message-template.service';
import { Conversation, ConversationSummary } from '../../core/models/conversation.model';
import { WhatsAppConnection } from '../../core/models/whatsapp-connection.model';
import { MessageTemplate, SCOPE_LABELS } from '../../core/models/message-template.model';
import { PhoneMaskDirective } from '../../shared/phone-mask.directive';

@Component({
  selector: 'app-inbox',
  standalone: true,
  imports: [CommonModule, FormsModule, PhoneMaskDirective],
  templateUrl: './inbox.component.html',
  styleUrl: './inbox.component.scss',
})
export class InboxComponent implements OnInit {
  private conversationService = inject(ConversationService);
  private connectionService = inject(WhatsAppConnectionService);
  private templateService = inject(MessageTemplateService);

  conversations = signal<ConversationSummary[]>([]);
  selectedConversation = signal<Conversation | null>(null);
  draftMessage = signal('');
  sendingMessage = signal(false);
  sendError = signal<string | null>(null);

  connections = signal<WhatsAppConnection[]>([]);
  showNewConversation = signal(false);
  newPhoneNumber = signal('');
  newContactName = signal('');
  newConnectionId = signal('');
  newFirstMessage = signal('');
  startingConversation = signal(false);
  newConversationError = signal<string | null>(null);

  templates = signal<MessageTemplate[]>([]);
  scopeLabels = SCOPE_LABELS;
  showTemplatePicker = signal(false);

  ngOnInit(): void {
    this.loadConversations();
    this.connectionService.getAll().subscribe((data) => {
      this.connections.set(data);
      if (data.length) this.newConnectionId.set(data[0].id);
    });
    this.templateService.getAll().subscribe((data) => this.templates.set(data.filter((t) => t.isActive)));
  }

  loadConversations(selectId?: string): void {
    this.conversationService.getAll().subscribe({
      next: (data) => {
        this.conversations.set(data);
        const toSelect = selectId
          ? data.find((c) => c.id === selectId)
          : this.selectedConversation() ?? data[0];
        if (toSelect) this.select(toSelect);
      },
      error: () => this.sendError.set('Não foi possível carregar as conversas. Recarregue a página.'),
    });
  }

  select(conversation: ConversationSummary): void {
    this.sendError.set(null);
    this.conversationService.getById(conversation.id).subscribe({
      next: (full) => this.selectedConversation.set(full),
      error: () => this.sendError.set('Não foi possível abrir essa conversa.'),
    });
  }

  send(): void {
    const conv = this.selectedConversation();
    const text = this.draftMessage().trim();
    if (!conv || !text) return;

    this.sendingMessage.set(true);
    this.sendError.set(null);

    this.conversationService.sendMessage(conv.id, text).subscribe({
      next: () => {
        this.draftMessage.set('');
        this.sendingMessage.set(false);
        // Em produção: atualizar via SignalR em tempo real em vez de refetch manual.
        this.conversationService.getById(conv.id).subscribe((updated) => {
          this.selectedConversation.set(updated);
        });
      },
      error: (err) => {
        this.sendingMessage.set(false);
        this.sendError.set(
          err?.error?.message ?? 'Não foi possível enviar a mensagem. Verifique se o número ainda está conectado.',
        );
      },
    });
  }

  toggleTemplatePicker(): void {
    this.showTemplatePicker.update((v) => !v);
  }

  useTemplate(template: MessageTemplate): void {
    this.draftMessage.set(template.content);
    this.showTemplatePicker.set(false);
  }

  openNewConversation(): void {
    this.newPhoneNumber.set('');
    this.newContactName.set('');
    this.newFirstMessage.set('');
    this.newConversationError.set(null);
    this.showNewConversation.set(true);
  }

  closeNewConversation(): void {
    this.showNewConversation.set(false);
  }

  startConversation(): void {
    const phoneNumber = this.newPhoneNumber().trim();
    const content = this.newFirstMessage().trim();
    const connectionId = this.newConnectionId();

    if (!connectionId) {
      this.newConversationError.set('Conecte um número de WhatsApp antes de iniciar uma conversa.');
      return;
    }
    if (phoneNumber.length < 10) {
      this.newConversationError.set('Digite um número de WhatsApp válido, com DDI e DDD.');
      return;
    }
    if (!content) {
      this.newConversationError.set('Escreva a primeira mensagem.');
      return;
    }

    this.startingConversation.set(true);
    this.newConversationError.set(null);

    this.conversationService
      .startConversation({
        whatsAppConnectionId: connectionId,
        phoneNumber,
        contactName: this.newContactName() || undefined,
        content,
      })
      .subscribe({
        next: (res) => {
          this.startingConversation.set(false);
          this.showNewConversation.set(false);
          this.loadConversations(res.conversationId);
        },
        error: (err) => {
          this.startingConversation.set(false);
          this.newConversationError.set(
            err?.error?.message ?? 'Não foi possível iniciar a conversa. Verifique se o número está conectado.',
          );
        },
      });
  }
}
