import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConversationService } from '../../core/services/conversation.service';
import { Conversation } from '../../core/models/conversation.model';

@Component({
  selector: 'app-inbox',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inbox.component.html',
  styleUrl: './inbox.component.scss',
})
export class InboxComponent implements OnInit {
  private conversationService = inject(ConversationService);

  conversations = signal<Conversation[]>([]);
  selectedConversation = signal<Conversation | null>(null);
  draftMessage = signal('');

  ngOnInit(): void {
    this.conversationService.getAll().subscribe((data) => {
      this.conversations.set(data);
      if (data.length) this.select(data[0]);
    });
  }

  select(conversation: Conversation): void {
    this.selectedConversation.set(conversation);
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
}
