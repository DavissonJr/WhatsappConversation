import { Directive, ElementRef, HostListener, inject } from '@angular/core';
import { NgControl } from '@angular/forms';

/**
 * Formata o campo como telefone brasileiro enquanto o usuário digita
 * (ex: "55 11 99999-9999"), mas mantém no ngModel só os dígitos puros
 * (é isso que a API espera para mandar pelo WhatsApp).
 */
@Directive({
  selector: '[appPhoneMask]',
  standalone: true,
})
export class PhoneMaskDirective {
  private el = inject(ElementRef<HTMLInputElement>);
  private control = inject(NgControl);

  @HostListener('input', ['$event'])
  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '').slice(0, 15);

    const formatted = this.format(digits);
    input.value = formatted;

    // O model guarda só os dígitos — é o formato que a API/Evolution espera.
    this.control.control?.setValue(digits, { emitEvent: true });
  }

  private format(digits: string): string {
    if (!digits) return '';

    // Heurística simples para número BR com DDI: 55 DD NNNNNNNNN
    if (digits.startsWith('55') && digits.length > 4) {
      const ddi = digits.slice(0, 2);
      const ddd = digits.slice(2, 4);
      const rest = digits.slice(4);
      const restFormatted =
        rest.length > 5 ? `${rest.slice(0, rest.length - 4)}-${rest.slice(-4)}` : rest;
      return `+${ddi} (${ddd}) ${restFormatted}`.trim();
    }

    return digits;
  }
}
