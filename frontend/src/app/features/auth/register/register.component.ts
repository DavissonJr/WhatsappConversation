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

  // Passo 1: dados do cadastro
  step = signal<'form' | 'code'>('form');
  companyName = signal('');
  segment = signal('clinica');
  fullName = signal('');
  email = signal('');
  password = signal('');
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  // Passo 2: código de confirmação
  code = signal('');
  verifying = signal(false);
  codeError = signal<string | null>(null);
  resending = signal(false);
  resendMessage = signal<string | null>(null);

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
        next: () => {
          this.loading.set(false);
          this.step.set('code');
        },
        error: (err) => {
          this.loading.set(false);
          this.errorMessage.set(err?.error?.message ?? 'Não foi possível criar sua conta. Tente novamente.');
        },
      });
  }

  backToForm(): void {
    this.step.set('form');
    this.code.set('');
    this.codeError.set(null);
  }

  verifyCode(): void {
    if (this.code().trim().length !== 6) {
      this.codeError.set('Digite os 6 dígitos do código.');
      return;
    }

    this.verifying.set(true);
    this.codeError.set(null);

    this.auth.verifyRegistration(this.email(), this.code().trim()).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        this.verifying.set(false);
        this.codeError.set(err?.error?.message ?? 'Código inválido. Tente de novo.');
      },
    });
  }

  resendCode(): void {
    this.resending.set(true);
    this.resendMessage.set(null);
    this.codeError.set(null);

    this.auth.resendCode(this.email()).subscribe({
      next: () => {
        this.resending.set(false);
        this.resendMessage.set('Código novo enviado! Confira seu e-mail.');
      },
      error: () => {
        this.resending.set(false);
        this.codeError.set('Não foi possível reenviar o código.');
      },
    });
  }
}
