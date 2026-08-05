import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { MessageTemplateService } from '../../core/services/message-template.service';
import { ToastService } from '../../core/services/toast.service';
import { MessageTemplate, SCOPE_LABELS, TemplateScope } from '../../core/models/message-template.model';

@Component({
  selector: 'app-message-templates',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './message-templates.component.html',
  styleUrl: './message-templates.component.scss',
})
export class MessageTemplatesComponent implements OnInit {
  private service = inject(MessageTemplateService);
  private toast = inject(ToastService);

  templates = signal<MessageTemplate[]>([]);
  scopeLabels = SCOPE_LABELS;
  scopes: TemplateScope[] = ['Cobranca', 'Lembrete', 'BoasVindas', 'Orcamento', 'Agendamento', 'Outro'];

  showForm = signal(false);
  editingId = signal<string | null>(null);
  formName = signal('');
  formScope = signal<TemplateScope>('Lembrete');
  formContent = signal('');
  saving = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.service.getAll().subscribe({
      next: (data) => this.templates.set(data),
      error: () => this.toast.error('Não foi possível carregar os modelos.'),
    });
  }

  openCreateForm(): void {
    this.editingId.set(null);
    this.formName.set('');
    this.formScope.set('Lembrete');
    this.formContent.set('');
    this.showForm.set(true);
  }

  openEditForm(template: MessageTemplate): void {
    this.editingId.set(template.id);
    this.formName.set(template.name);
    this.formScope.set(template.scope);
    this.formContent.set(template.content);
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
  }

  save(): void {
    if (!this.formName().trim() || !this.formContent().trim()) {
      this.toast.error('Preencha o nome e o conteúdo do modelo.');
      return;
    }

    this.saving.set(true);
    const payload = { name: this.formName(), scope: this.formScope(), content: this.formContent() };
    const editingId = this.editingId();

    const request$: Observable<unknown> = editingId
      ? this.service.update(editingId, { ...payload, isActive: true })
      : this.service.create(payload);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.toast.success(editingId ? 'Modelo atualizado.' : 'Modelo criado.');
        this.load();
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Não foi possível salvar o modelo.');
      },
    });
  }

  remove(template: MessageTemplate): void {
    if (!confirm(`Remover o modelo "${template.name}"?`)) return;
    this.service.delete(template.id).subscribe({
      next: () => {
        this.toast.success('Modelo removido.');
        this.load();
      },
      error: () => this.toast.error('Não foi possível remover o modelo.'),
    });
  }
}
