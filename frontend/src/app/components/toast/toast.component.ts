import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-container">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast" [ngClass]="'toast-' + toast.type">
          <div class="toast-icon">
            @if (toast.type === 'success') { <span>✅</span> }
            @if (toast.type === 'error') { <span>🚨</span> }
            @if (toast.type === 'warning') { <span>⚠️</span> }
            @if (toast.type === 'info') { <span>ℹ️</span> }
          </div>
          <div class="toast-content">
            @if (toast.title) { <h4 class="toast-title">{{ toast.title }}</h4> }
            <p class="toast-message">{{ toast.message }}</p>
          </div>
          <button class="toast-close" (click)="toastService.remove(toast.id)">✕</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 1.5rem;
      right: 1.5rem;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      pointer-events: none;
    }
    
    .toast {
      pointer-events: auto;
      background-color: var(--bg-card);
      border-radius: var(--border-radius-sm);
      box-shadow: var(--shadow-lg);
      padding: 1rem 1.25rem;
      display: flex;
      align-items: flex-start;
      gap: 1rem;
      min-width: 300px;
      max-width: 400px;
      border-left: 4px solid var(--border-color);
      animation: slideIn 0.3s cubic-bezier(0.16, 1, 0.3, 1);
      transition: all 0.3s ease;
    }
    
    @keyframes slideIn {
      from { transform: translateX(100%); opacity: 0; }
      to { transform: translateX(0); opacity: 1; }
    }
    
    .toast-success { border-left-color: var(--success); }
    .toast-error { border-left-color: var(--danger); }
    .toast-warning { border-left-color: var(--warning); }
    .toast-info { border-left-color: var(--info); }
    
    .toast-icon {
      font-size: 1.25rem;
      flex-shrink: 0;
    }
    
    .toast-content {
      flex: 1;
    }
    
    .toast-title {
      font-weight: 600;
      font-size: 0.95rem;
      margin-bottom: 0.25rem;
      color: var(--text-main);
    }
    
    .toast-message {
      font-size: 0.85rem;
      color: var(--text-muted);
      line-height: 1.4;
    }
    
    .toast-close {
      background: none;
      border: none;
      color: var(--text-muted);
      cursor: pointer;
      font-size: 1rem;
      padding: 0.25rem;
      margin: -0.25rem;
      transition: color 0.2s;
    }
    
    .toast-close:hover {
      color: var(--danger);
    }
  `]
})
export class ToastComponent {
  constructor(public toastService: ToastService) {}
}
