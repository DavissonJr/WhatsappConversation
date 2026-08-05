import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { WhatsAppConnectionService } from '../../core/services/whatsapp-connection.service';
import { WhatsAppConnection } from '../../core/models/whatsapp-connection.model';

@Component({
  selector: 'app-whatsapp-connections',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './whatsapp-connections.component.html',
  styleUrl: './whatsapp-connections.component.scss',
})
export class WhatsAppConnectionsComponent implements OnInit {
  private service = inject(WhatsAppConnectionService);

  connections = signal<WhatsAppConnection[]>([]);
  newLabel = signal('');
  creating = signal(false);
  qrCodeFor = signal<string | null>(null);
  qrCodeBase64 = signal<string | null>(null);
  loadingQr = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.service.getAll().subscribe((data) => this.connections.set(data));
  }

  create(): void {
    const label = this.newLabel().trim();
    if (!label) return;

    this.creating.set(true);
    this.service.create(label).subscribe({
      next: (conn) => {
        this.connections.update((list) => [...list, conn]);
        this.newLabel.set('');
        this.creating.set(false);
        this.showQrCode(conn);
      },
      error: () => this.creating.set(false),
    });
  }

  showQrCode(conn: WhatsAppConnection): void {
    this.qrCodeFor.set(conn.id);
    this.loadingQr.set(true);
    this.qrCodeBase64.set(null);

    this.service.getQrCode(conn.id).subscribe({
      next: (res) => {
        this.qrCodeBase64.set(res.qrCodeBase64);
        this.loadingQr.set(false);
      },
      error: () => this.loadingQr.set(false),
    });
  }

  closeQrCode(): void {
    this.qrCodeFor.set(null);
    this.qrCodeBase64.set(null);
  }

  checkStatus(conn: WhatsAppConnection): void {
    this.service.refreshStatus(conn.id).subscribe((res) => {
      this.connections.update((list) =>
        list.map((c) => (c.id === conn.id ? { ...c, isConnected: res.isConnected } : c)),
      );
      if (res.isConnected) this.closeQrCode();
    });
  }

  disconnect(conn: WhatsAppConnection): void {
    if (!confirm(`Desconectar o número "${conn.label}"? Você poderá reconectar depois escaneando um novo QR code.`)) return;

    this.service.disconnect(conn.id).subscribe(() => {
      this.connections.update((list) =>
        list.map((c) => (c.id === conn.id ? { ...c, isConnected: false, phoneNumber: undefined } : c)),
      );
    });
  }
}
