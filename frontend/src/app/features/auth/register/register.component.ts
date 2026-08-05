import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: '../auth-shared.scss',
})
export class RegisterComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  companyName = signal('');
  segment = signal('clinica');
  fullName = signal('');
  email = signal('');
  password = signal('');
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  segments = [
    { value: 'clinica', label: 'Clínica' },
    { value: 'oficina', label: 'Oficina mecânica' },
    { value: 'advocacia', label: 'Escritório de advocacia' },
    { value: 'imobiliaria', label: 'Imobiliária' },
    { value: 'outro', label: 'Outro' },
  ];

  submit(): void {
    if (!this.companyName() || !this.fullName() || !this.email() || !this.password()) return;

    this.loading.set(true);
    this.errorMessage.set(null);

    this.auth
      .register({
        companyName: this.companyName(),
        segment: this.segment(),
        fullName: this.fullName(),
        email: this.email(),
        password: this.password(),
      })
      .subscribe({
        next: () => this.router.navigate(['/inbox']),
        error: (err) => {
          this.loading.set(false);
          this.errorMessage.set(err?.error?.message ?? 'Não foi possível criar sua conta. Tente novamente.');
        },
      });
  }
}
