import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { AdminService } from '../../core/services/admin.service';
import { ToastService } from '../../core/services/toast.service';
import { AdminTenantSummary } from '../../core/models/admin.model';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
})
export class AdminComponent implements OnInit {
  private service = inject(AdminService);
  private toast = inject(ToastService);

  tenants = signal<AdminTenantSummary[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getTenants().subscribe({
      next: (data) => {
        this.tenants.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Não foi possível carregar as empresas.');
      },
    });
  }

  toggleActive(tenant: AdminTenantSummary): void {
    const next = !tenant.isActive;
    const action = next ? 'reativar' : 'suspender';
    if (!confirm(`Tem certeza que quer ${action} "${tenant.name}"?`)) return;

    this.service.setTenantActive(tenant.id, next).subscribe({
      next: () => {
        this.toast.success(next ? 'Empresa reativada.' : 'Empresa suspensa.');
        this.load();
      },
      error: () => this.toast.error('Não foi possível atualizar essa empresa.'),
    });
  }

  activeCount(): number {
    return this.tenants().filter((t) => t.isActive).length;
  }

  totalAiCost(): number {
    return this.tenants().reduce((sum, t) => sum + t.totalAiEstimatedCostUsd, 0);
  }

  totalContacts(): number {
    return this.tenants().reduce((sum, t) => sum + t.contactCount, 0);
  }
}
