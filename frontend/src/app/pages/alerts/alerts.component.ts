import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { Alert } from '../../models/models';

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="page-wrapper">
      <div class="page-header">
        <div>
          <h1 class="page-title-h1">Alert Settings</h1>
          <p class="page-desc">Configure loss limits and drawdown warnings</p>
        </div>
      </div>

      @if (loading()) {
        <div class="loading-state"><div class="loading-spinner"></div></div>
      } @else {
        <div class="alerts-layout">
          <div class="section-card alerts-form-card">
            <h3>🔔 Configure Alerts</h3>
            <form [formGroup]="form" (ngSubmit)="onSave()">
              <div class="form-group">
                <label>Daily Loss Limit ($)</label>
                <p class="field-hint">Alert triggered when today's losses exceed this amount</p>
                <input type="number" formControlName="dailyLossLimit" class="form-input" />
              </div>
              <div class="form-group">
                <label>Max Drawdown (%)</label>
                <p class="field-hint">Warning when drawdown reaches this % of account</p>
                <input type="number" step="0.5" formControlName="maxDrawdownPercent" class="form-input" />
              </div>
              <div class="form-group">
                <label>Max Trades Per Day</label>
                <p class="field-hint">Recommended maximum trades to prevent overtrading</p>
                <input type="number" formControlName="maxTradesPerDay" class="form-input" />
              </div>
              <div class="form-group checkbox-group">
                <label class="checkbox-label">
                  <input type="checkbox" formControlName="isActive" />
                  <span>Enable Alerts</span>
                </label>
              </div>
              <div class="form-group checkbox-group">
                <label class="checkbox-label">
                  <input type="checkbox" formControlName="emailAlertEnabled" />
                  <span>Email Notifications</span>
                </label>
              </div>
              @if (form.value.emailAlertEnabled) {
                <div class="form-group">
                  <label>Notification Email</label>
                  <input type="email" formControlName="email" class="form-input" placeholder="your@email.com" />
                </div>
              }
              <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">
                @if (saving()) { <span class="spinner"></span> }
                Save Alert Settings
              </button>
              @if (saved()) {
                <div class="alert alert-success mt-2">✅ Settings saved successfully!</div>
              }
            </form>
          </div>

          <div class="alerts-info-panel">
            <div class="section-card">
              <h3>ℹ️ How Alerts Work</h3>
              <div class="info-items">
                <div class="info-item">
                  <span class="info-icon">💸</span>
                  <div>
                    <h4>Daily Loss Alert</h4>
                    <p>A warning banner appears on the dashboard when your daily P&L drops below the loss limit.</p>
                  </div>
                </div>
                <div class="info-item">
                  <span class="info-icon">📉</span>
                  <div>
                    <h4>Drawdown Warning</h4>
                    <p>Color-coded indicator in Analytics shows your current vs max drawdown with critical alerts.</p>
                  </div>
                </div>
                <div class="info-item">
                  <span class="info-icon">📊</span>
                  <div>
                    <h4>Trade Count</h4>
                    <p>AI analysis detects if you're exceeding the max trades per day and flags overtrading.</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      }
    </div>
  `
})
export class AlertsComponent implements OnInit {
  loading = signal(true);
  saving = signal(false);
  saved = signal(false);

  form;

  constructor(private api: ApiService, private fb: FormBuilder, private toast: ToastService) {
    this.form = this.fb.group({
      dailyLossLimit: [200, [Validators.required, Validators.min(0)]],
      maxDrawdownPercent: [10, [Validators.required, Validators.min(1), Validators.max(100)]],
      maxTradesPerDay: [5, [Validators.required, Validators.min(1)]],
      isActive: [true],
      emailAlertEnabled: [false],
      email: ['']
    });
  }

  ngOnInit(): void {
    this.api.getAlert().subscribe({
      next: (data) => {
        if (data) this.form.patchValue(data as any);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSave(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.api.upsertAlert(this.form.value as Alert).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
        this.toast.success('Alert settings saved successfully.', 'Saved');
        setTimeout(() => this.saved.set(false), 3000);
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err.error?.error || 'Failed to save alert settings.', 'Error');
      }
    });
  }
}
