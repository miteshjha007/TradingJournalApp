import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { Instrument, RiskResult } from '../../models/models';

@Component({
  selector: 'app-risk-tool',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, DecimalPipe],
  template: `
    <div class="page-wrapper">
      <div class="page-header">
        <div>
          <h1 class="page-title-h1">Risk Management Tool</h1>
          <p class="page-desc">Calculate optimal lot size based on your account and risk tolerance</p>
        </div>
      </div>

      <div class="risk-layout">
        <!-- Calculator Form -->
        <div class="section-card risk-form-card">
          <h3>⚖️ Risk Calculator</h3>
          <form [formGroup]="form" (ngSubmit)="calculate()">
            <div class="form-group">
              <label>Account Balance ($)</label>
              <input type="number" formControlName="accountBalance" class="form-input" placeholder="10000" />
            </div>
            <div class="form-group">
              <label>Risk Percentage (%)</label>
              <input type="number" step="0.1" formControlName="riskPercent" class="form-input" placeholder="1.0" />
              <div class="risk-meter">
                <div class="risk-meter-fill"
                  [style.width.%]="mathMin((form.value.riskPercent || 0) * 20, 100)"
                  [class.risk-low]="(form.value.riskPercent || 0) <= 1"
                  [class.risk-med]="(form.value.riskPercent || 0) > 1 && (form.value.riskPercent || 0) <= 2"
                  [class.risk-high]="(form.value.riskPercent || 0) > 2">
                </div>
              </div>
              <span class="risk-label-text">
                {{ (form.value.riskPercent || 0) <= 1 ? '🟢 Conservative' : (form.value.riskPercent || 0) <= 2 ? '🟡 Moderate' : '🔴 Aggressive' }}
              </span>
            </div>
            <div class="form-group">
              <label>Instrument (optional)</label>
              <select formControlName="instrumentId" class="form-input">
                <option value="">General Calculation</option>
                @for (inst of instruments(); track inst.id) {
                  <option [value]="inst.id">{{ inst.name }} (Safe: {{ inst.safeLotSize }})</option>
                }
              </select>
            </div>
            <button type="submit" class="btn btn-primary btn-full" [disabled]="form.invalid || calculating()">
              @if (calculating()) { <span class="spinner"></span> } Calculate Risk
            </button>
          </form>
        </div>

        <!-- Results -->
        @if (result()) {
          <div class="section-card risk-result-card">
            <h3>📊 Risk Analysis Results</h3>
            <div class="result-grid">
              <div class="result-item highlight">
                <span class="result-label">Suggested Lot Size</span>
                <span class="result-value primary">{{ result()!.suggestedLotSize | number:'1.2-2' }}</span>
              </div>
              <div class="result-item">
                <span class="result-label">Risk Amount</span>
                <span class="result-value">{{ result()!.riskAmount | currency }}</span>
              </div>
              <div class="result-item">
                <span class="result-label">Max Allowed Lot</span>
                <span class="result-value">{{ result()!.maxAllowedLotSize | number:'1.2-2' }}</span>
              </div>
              <div class="result-item">
                <span class="result-label">Max Trades/Day</span>
                <span class="result-value">{{ result()!.maxTradesPerDay }}</span>
              </div>
              <div class="result-item">
                <span class="result-label">Risk Level</span>
                <span class="result-value risk-level" [class]="'level-' + result()!.riskLevel.toLowerCase()">{{ result()!.riskLevel }}</span>
              </div>
            </div>
            @if (result()!.warning) {
              <div class="risk-warning">⚠️ {{ result()!.warning }}</div>
            }
          </div>
        }

        <!-- Risk Guide -->
        <div class="section-card risk-guide-card">
          <h3>📋 Risk Management Guide</h3>
          <div class="guide-items">
            <div class="guide-item">
              <span class="guide-icon">🟢</span>
              <div>
                <h4>Conservative (up to 1%)</h4>
                <p>Risk max 1% per trade. Max 10 trades/day. Best for consistent growth.</p>
              </div>
            </div>
            <div class="guide-item">
              <span class="guide-icon">🟡</span>
              <div>
                <h4>Moderate (1-2%)</h4>
                <p>Risk 1-2% per trade. Max 5 trades/day. Balanced approach.</p>
              </div>
            </div>
            <div class="guide-item">
              <span class="guide-icon">🔴</span>
              <div>
                <h4>Aggressive (above 2%)</h4>
                <p>Risk more than 2% per trade. Max 3 trades/day. High risk, high reward.</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class RiskToolComponent implements OnInit {
  instruments = signal<Instrument[]>([]);
  result = signal<RiskResult | null>(null);
  calculating = signal(false);
  form;

  constructor(private api: ApiService, private fb: FormBuilder, private toast: ToastService) {
    this.form = this.fb.group({
      accountBalance: [10000, [Validators.required, Validators.min(1)]],
      riskPercent: [1.0, [Validators.required, Validators.min(0.1), Validators.max(10)]],
      instrumentId: ['']
    });
  }

  mathMin(a: number, b: number): number { return Math.min(a, b); }

  ngOnInit(): void {
    this.api.getInstruments().subscribe(data => this.instruments.set(data));
  }

  calculate(): void {
    if (this.form.invalid) return;
    this.calculating.set(true);
    const v = this.form.value;
    this.api.calculateRisk({
      accountBalance: v.accountBalance || 10000,
      riskPercent: v.riskPercent || 1,
      instrumentId: v.instrumentId || undefined
    }).subscribe({
      next: (data) => { 
        this.result.set(data); 
        this.calculating.set(false); 
        this.toast.success('Risk calculation complete.', 'Success');
      },
      error: (err) => {
        this.calculating.set(false);
        this.toast.error(err.error?.error || 'Failed to calculate risk.', 'Error');
      }
    });
  }
}
