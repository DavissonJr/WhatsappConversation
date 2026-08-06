import { Component, inject } from '@angular/core';
import { LoadingService } from '../../core/services/loading.service';

@Component({
  selector: 'app-loading-overlay',
  standalone: true,
  template: `
    @if (loading.isLoading()) {
      <div class="loading-overlay">
        <div class="spinner"></div>
      </div>
    }
  `,
  styleUrl: './loading-overlay.component.scss',
})
export class LoadingOverlayComponent {
  loading = inject(LoadingService);
}
