import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConversationService } from '../../core/services/conversation.service';
import { WhatsAppConnectionService } from '../../core/services/whatsapp-connection.service';
import { Conversation, ConversationSummary } from '../../core/models/conversation.model';
import { WhatsAppConnection } from '../../core/models/whatsapp-connection.model';

@Component({
  selector: 'app-inbox',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inbox.component.html',
  styleUrl: './inbox.component.scss',
})
export class InboxComponent implements OnInit {
  private conversationService = inject(ConversationService);
  private connectionService = inject(WhatsAppConnectionService);

  conversations = signal<ConversationSummary[]>([]);
  selectedConversation = signal<Conversation | null>(null);
  draftMessage = signal('');

  connections = signal<WhatsAppConnection[]>([]);
  showNewConversation = signal(false);
  newPhoneNumber = signal('');
  newContactName = signal('');
  newConnectionId = signal('');
  newFirstMessage = signal('');
  startingConversation = signal(false);
  newConversationError = signal<string | null>(null);

  ngOnInit(): void {
    this.loadConversations();
    this.connectionService.getAll().subscribe((data) => {
      this.connections.set(data);
      if (data.length) this.newConnectionId.set(data[0].id);
    });
  }

  loadConversations(selectId?: string): void {
    this.conversationService.getAll().subscribe((data) => {
      this.conversations.set(data);
      const toSelect = selectId
        ? data.find((c) => c.id === selectId)
        : this.selectedConversation() ?? data[0];
      if (toSelect) this.select(toSelect);
    });
  }

  select(conversation: ConversationSummary): void {
    this.conversationService.getById(conversation.id).subscribe((full) => {
      this.selectedConversation.set(full);
    });
  }

  send(): void {
    const conv = this.selectedConversation();
    const text = this.draftMessage().trim();
    if (!conv || !text) return;

    this.conversationService.sendMessage(conv.id, text).subscribe(() => {
      this.draftMessage.set('');
      // Em produção: atualizar via SignalR em tempo real em vez de refetch manual.
      this.conversationService.getById(conv.id).subscribe((updated) => {
        this.selectedConversation.set(updated);
      });
    });
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

    if (!phoneNumber || !content || !connectionId) return;

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
            err?.error?.message ?? 'Não foi possível iniciar a conversa.',
          );
        },
      });
  }
}
