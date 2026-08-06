import { CommonModule } from '@angular/common';
import {
  AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild, effect, inject,
} from '@angular/core';
import { Chart, registerables } from 'chart.js';
import { DashboardService } from '../../core/services/dashboard.service';
import { ThemeService } from '../../core/services/theme.service';
import { ToastService } from '../../core/services/toast.service';
import { DashboardSummary } from '../../core/models/dashboard.model';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  private service = inject(DashboardService);
  private toast = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  theme = inject(ThemeService);

  @ViewChild('contactsChart') contactsChartRef?: ElementRef<HTMLCanvasElement>;
  @ViewChild('messagesChart') messagesChartRef?: ElementRef<HTMLCanvasElement>;
  @ViewChild('conversationsChart') conversationsChartRef?: ElementRef<HTMLCanvasElement>;
  @ViewChild('appointmentsChart') appointmentsChartRef?: ElementRef<HTMLCanvasElement>;
  @ViewChild('proposalsChart') proposalsChartRef?: ElementRef<HTMLCanvasElement>;

  data?: DashboardSummary;
  loading = true;

  private charts: Chart[] = [];

  constructor() {
    // Redesenha os gráficos com as cores certas sempre que o tema mudar.
    effect(() => {
      this.theme.theme();
      if (this.data) this.renderCharts();
    });
  }

  ngOnInit(): void {
    this.service.getSummary().subscribe({
      next: (data) => {
        this.data = data;
        this.loading = false;
        // IMPORTANTE: detectChanges() força o Angular a criar os elementos
        // <canvas> no DOM AGORA (eles só existem depois que "data" existe,
        // por causa do @if no template) — sem isso, os @ViewChild ainda
        // estariam undefined nesse ponto e os gráficos ficariam em branco.
        this.cdr.detectChanges();
        this.renderCharts();
      },
      error: () => {
        this.loading = false;
        this.toast.error('Não foi possível carregar o dashboard.');
      },
    });
  }

  ngAfterViewInit(): void {
    if (this.data) this.renderCharts();
  }

  ngOnDestroy(): void {
    this.destroyCharts();
  }

  private destroyCharts(): void {
    this.charts.forEach((c) => c.destroy());
    this.charts = [];
  }

  private colors() {
    const styles = getComputedStyle(document.documentElement);
    return {
      text: styles.getPropertyValue('--text-secondary').trim() || '#64748b',
      grid: styles.getPropertyValue('--border-color').trim() || '#e2e8f0',
      signal: '#10b981',
      amber: '#f59e0b',
      rose: '#f43f5e',
      blue: '#3b82f6',
      purple: '#8b5cf6',
      gray: '#94a3b8',
    };
  }

  private renderCharts(): void {
    if (!this.data) return;
    this.destroyCharts();

    const c = this.colors();
    const commonScales = {
      x: { ticks: { color: c.text, font: { size: 11 } }, grid: { display: false } },
      y: { ticks: { color: c.text, font: { size: 11 }, precision: 0 }, grid: { color: c.grid } },
    };
    const legendLabels = { color: c.text, font: { size: 12 }, padding: 12 };

    // ---- Novos contatos (linha) ----
    if (this.contactsChartRef) {
      this.charts.push(new Chart(this.contactsChartRef.nativeElement, {
        type: 'line',
        data: {
          labels: this.data.newContactsByDay.map((d) => this.formatDay(d.date)),
          datasets: [{
            label: 'Novos contatos',
            data: this.data.newContactsByDay.map((d) => d.count),
            borderColor: c.signal,
            backgroundColor: c.signal + '33',
            fill: true,
            tension: 0.3,
            pointRadius: 2,
          }],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: commonScales,
        },
      }));
    }

    // ---- Mensagens por dia (barras agrupadas) ----
    if (this.messagesChartRef) {
      this.charts.push(new Chart(this.messagesChartRef.nativeElement, {
        type: 'bar',
        data: {
          labels: this.data.messagesByDay.map((d) => this.formatDay(d.date)),
          datasets: [
            { label: 'Recebidas', data: this.data.messagesByDay.map((d) => d.inbound), backgroundColor: c.blue },
            { label: 'Enviadas', data: this.data.messagesByDay.map((d) => d.outbound), backgroundColor: c.signal },
          ],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { labels: legendLabels, position: 'top' } },
          scales: commonScales,
        },
      }));
    }

    // ---- Conversas por status (rosca) ----
    if (this.conversationsChartRef) {
      const cv = this.data.conversationsByStatus;
      this.charts.push(new Chart(this.conversationsChartRef.nativeElement, {
        type: 'doughnut',
        data: {
          labels: ['Abertas', 'Aguardando humano', 'Fechadas'],
          datasets: [{
            data: [cv.open, cv.waitingHuman, cv.closed],
            backgroundColor: [c.signal, c.amber, c.gray],
            borderWidth: 0,
          }],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { labels: legendLabels, position: 'bottom' } },
        },
      }));
    }

    // ---- Agendamentos por status (rosca) ----
    if (this.appointmentsChartRef) {
      const ap = this.data.appointmentsByStatus;
      this.charts.push(new Chart(this.appointmentsChartRef.nativeElement, {
        type: 'doughnut',
        data: {
          labels: ['Agendado', 'Confirmado', 'Concluído', 'Cancelado', 'Não compareceu'],
          datasets: [{
            data: [ap.scheduled, ap.confirmed, ap.completed, ap.cancelled, ap.noShow],
            backgroundColor: [c.blue, c.signal, c.gray, c.rose, c.amber],
            borderWidth: 0,
          }],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { labels: legendLabels, position: 'bottom' } },
        },
      }));
    }

    // ---- Propostas por status (rosca) ----
    if (this.proposalsChartRef) {
      const pr = this.data.proposalsByStatus;
      this.charts.push(new Chart(this.proposalsChartRef.nativeElement, {
        type: 'doughnut',
        data: {
          labels: ['Rascunho', 'Enviada', 'Aceita', 'Recusada', 'Expirada'],
          datasets: [{
            data: [pr.draft, pr.sentToClient, pr.accepted, pr.rejected, pr.expired],
            backgroundColor: [c.gray, c.blue, c.signal, c.rose, c.amber],
            borderWidth: 0,
          }],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { labels: legendLabels, position: 'bottom' } },
        },
      }));
    }
  }

  private formatDay(iso: string): string {
    const d = new Date(iso);
    return d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' });
  }
}
