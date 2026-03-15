import { Component, inject } from '@angular/core';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast-notification',
  standalone: true,
  template: `
    <div class="toast-container">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast toast--{{ toast.type }}">
          <span class="toast-msg">{{ toast.message }}</span>
          <button class="toast-close" (click)="toastService.dismiss(toast.id)">&times;</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      bottom: 1.5rem;
      right: 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      z-index: 9999;
      max-width: 360px;
    }
    .toast {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
      padding: 0.75rem 1rem;
      border-radius: var(--radius);
      box-shadow: var(--shadow);
      font-size: 0.9rem;
      animation: slide-in 0.2s ease;
    }
    @keyframes slide-in {
      from { transform: translateX(120%); opacity: 0; }
      to { transform: translateX(0); opacity: 1; }
    }
    .toast--success { background: #E8F5E9; border-left: 4px solid var(--color-success); color: #1B5E20; }
    .toast--error   { background: #FFEBEE; border-left: 4px solid var(--color-error);   color: #B71C1C; }
    .toast--warning { background: #FFF3E0; border-left: 4px solid var(--color-warning); color: #E65100; }
    .toast--info    { background: #FFF5F5; border-left: 4px solid var(--color-primary); color: var(--color-text); }
    .toast-close {
      background: none;
      border: none;
      font-size: 1.2rem;
      cursor: pointer;
      opacity: 0.6;
      line-height: 1;
      padding: 0;
      color: inherit;
    }
    .toast-close:hover { opacity: 1; }
  `]
})
export class ToastNotificationComponent {
  readonly toastService = inject(ToastService);
}
