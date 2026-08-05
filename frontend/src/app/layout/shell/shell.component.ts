import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { RealtimeService } from '../../core/services/realtime.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent implements OnInit, OnDestroy {
  auth = inject(AuthService);
  private realtime = inject(RealtimeService);

  navItems = [
    { path: '/inbox', label: 'Conversas', icon: 'M2 5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H8l-4 3v-3H4a2 2 0 0 1-2-2V5Z' },
    { path: '/numeros', label: 'Números WhatsApp', icon: 'M4 3h8l4 4v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2Zm7 6H8v2h3v2H8v2h5v-2h-2V9h2V7h-5v2Z' },
    { path: '/modelos', label: 'Modelos de mensagem', icon: 'M3 4h14v3H3V4Zm0 5h10v3H3V9Zm0 5h14v3H3v-3Z' },
  ];

  ngOnInit(): void {
    this.realtime.connect();
  }

  ngOnDestroy(): void {
    this.realtime.disconnect();
  }

  logout(): void {
    this.auth.logout();
  }
}
