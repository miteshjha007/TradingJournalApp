import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { BacktestRequest, BacktestResult, BacktestRuleFilter, BacktestRuleType } from '../../models/models';

@Component({
  selector: 'app-backtest',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="backtest-page">
      <div class="page-header">
        <div>
          <h1>Strategy Backtester</h1>
          <p class="subtitle">Test how your rules would have improved your past performance</p>
        </div>
      </div>

      <div class="backtest-layout">
        <!-- Config Panel -->
        <div class="config-panel">
          <h3>Configure Test</h3>

          <div class="form-group">
            <label>Test Name</label>
            <input [(ngModel)]="form.name" placeholder="e.g. Strict RRR Filter" class="form-control" />
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>From Date</label>
              <input [(ngModel)]="form.fromDate" type="date" class="form-control" />
            </div>
            <div class="form-group">
              <label>To Date</label>
              <input [(ngModel)]="form.toDate" type="date" class="form-control" />
            </div>
          </div>

          <div class="form-group">
            <label>Initial Balance ($)</label>
            <input [(ngModel)]="form.initialBalance" type="number" class="form-control" />
          </div>

          <div class="rules-section">
            <div class="rules-header">
              <label>Rules / Filters</label>
              <button class="btn-add-rule" (click)="addFilter()">+ Add</button>
            </div>

            @for (filter of form.rules; track $index; let i = $index) {
              <div class="filter-row">
                <select [(ngModel)]="filter.ruleType" class="form-control-sm" (change)="onRuleTypeChange(i)">
                  <option [value]="1">Min RRR</option>
                  <option [value]="2">Max Daily Trades</option>
                  <option [value]="3">Checklist Compliance %</option>
                  <option [value]="4">Trade Type (Long/Short)</option>
                  <option [value]="5">Time of Day (Hour)</option>
                  <option [value]="6">Max Risk %</option>
                </select>
                @if (filter.ruleType === 4) {
                  <input [(ngModel)]="filter.stringValue" placeholder="Long or Short" class="form-control-sm" />
                } @else if (filter.ruleType === 5) {
                  <input [(ngModel)]="filter.minValue" type="number" placeholder="From hour (0-23)" class="form-control-sm" />
                  <input [(ngModel)]="filter.maxValue" type="number" placeholder="To hour (0-23)" class="form-control-sm" />
                } @else {
                  <input [(ngModel)]="filter.minValue" type="number" placeholder="Min value" class="form-control-sm" />
                }
                <button class="btn-remove" (click)="removeFilter(i)">✕</button>
              </div>
            }
          </div>

          <label class="checkbox-label">
            <input type="checkbox" [(ngModel)]="form.runMonteCarlo" />
            Run Monte Carlo simulation (1000 trials)
          </label>

          <button class="btn btn-primary" (click)="runTest()" [disabled]="running()">
            {{ running() ? 'Running...' : 'Run Backtest' }}
          </button>
        </div>

        <!-- Results Panel -->
        <div class="results-panel">
          @if (result()) {
            <div class="results-grid">
              <div class="metric-card highlight">
                <div class="metric-label">Net P/L</div>
                <div class="metric-value" [class.positive]="result()!.totalPL > 0" [class.negative]="result()!.totalPL < 0">
                  {{ result()!.totalPL | currency }}
                </div>
              </div>
              <div class="metric-card">
                <div class="metric-label">Trades Used</div>
                <div class="metric-value">{{ result()!.filteredTradeCount }} / {{ result()!.tradeCount }}</div>
              </div>
              <div class="metric-card">
                <div class="metric-label">Win Rate</div>
                <div class="metric-value">{{ (result()!.winRate * 100) | number:'1.1-1' }}%</div>
              </div>
              <div class="metric-card">
                <div class="metric-label">Profit Factor</div>
                <div class="metric-value">{{ result()!.profitFactor | number:'1.2-2' }}</div>
              </div>
              <div class="metric-card">
                <div class="metric-label">Sharpe Ratio</div>
                <div class="metric-value">{{ result()!.sharpeRatio | number:'1.2-2' }}</div>
              </div>
              <div class="metric-card">
                <div class="metric-label">Sortino Ratio</div>
                <div class="metric-value">{{ result()!.sortinoRatio | number:'1.2-2' }}</div>
              </div>
              <div class="metric-card">
                <div class="metric-label">Max Drawdown</div>
                <div class="metric-value negative">{{ result()!.maxDrawdownPercent | number:'1.1-1' }}%</div>
              </div>
              <div class="metric-card">
                <div class="metric-label">Avg RRR</div>
                <div class="metric-value">{{ result()!.averageRRR | number:'1.2-2' }}</div>
              </div>
              <div class="metric-card">
                <div class="metric-label">Best Day</div>
                <div class="metric-value positive">{{ result()!.bestDay | currency }}</div>
              </div>
              <div class="metric-card">
                <div class="metric-label">Worst Day</div>
                <div class="metric-value negative">{{ result()!.worstDay | currency }}</div>
              </div>
            </div>

            <!-- Equity Curve -->
            @if (result()!.equityCurve && result()!.equityCurve.length > 1) {
              <div class="chart-section">
                <h4>Equity Curve</h4>
                <div class="equity-chart">
                  <svg viewBox="0 0 600 120" preserveAspectRatio="none">
                    <polyline [attr.points]="equityPoints()" fill="none" stroke="var(--primary)" stroke-width="2" />
                    <line x1="0" y1="60" x2="600" y2="60" stroke="var(--border-color)" stroke-width="0.5" stroke-dasharray="4" />
                  </svg>
                </div>
              </div>
            }

            <!-- Monte Carlo -->
            @if (result()!.monteCarlo) {
              <div class="monte-section">
                <h4>Monte Carlo (1000 simulations)</h4>
                <div class="monte-grid">
                  <div class="monte-card p5">
                    <div class="monte-label">Worst Case (P5)</div>
                    <div class="monte-value">{{ result()!.monteCarlo!.p5FinalBalance | currency }}</div>
                    <div class="monte-dd">Max DD: {{ result()!.monteCarlo!.p5Drawdown | number:'1.1-1' }}%</div>
                  </div>
                  <div class="monte-card median">
                    <div class="monte-label">Median</div>
                    <div class="monte-value">{{ result()!.monteCarlo!.medianFinalBalance | currency }}</div>
                    <div class="monte-dd">Max DD: {{ result()!.monteCarlo!.medianDrawdown | number:'1.1-1' }}%</div>
                  </div>
                  <div class="monte-card p95">
                    <div class="monte-label">Best Case (P95)</div>
                    <div class="monte-value">{{ result()!.monteCarlo!.p95FinalBalance | currency }}</div>
                    <div class="monte-dd">Max DD: {{ result()!.monteCarlo!.p95Drawdown | number:'1.1-1' }}%</div>
                  </div>
                </div>
                <div class="ruin-prob" [class.danger]="result()!.monteCarlo!.ruinProbability > 10">
                  Account Ruin Probability: <strong>{{ result()!.monteCarlo!.ruinProbability | number:'1.1-1' }}%</strong>
                </div>
              </div>
            }
          } @else if (!running()) {
            <div class="no-result">
              <div class="nr-icon">🔬</div>
              <p>Configure your filters and run a backtest to see results</p>
            </div>
          } @else {
            <div class="no-result">
              <div class="nr-icon">⏳</div>
              <p>Running simulation...</p>
            </div>
          }

          <!-- History -->
          @if (history().length > 0) {
            <div class="history-section">
              <h4>History</h4>
              <div class="history-list">
                @for (h of history(); track h.id) {
                  <div class="history-item" (click)="viewHistory(h)">
                    <div class="h-name">{{ h.name }}</div>
                    <div class="h-stats">
                      <span [class.pos]="h.totalPL > 0" [class.neg]="h.totalPL < 0">{{ h.totalPL | currency }}</span>
                      <span>WR: {{ (h.winRate * 100) | number:'1.0-0' }}%</span>
                      <span>{{ h.filteredTradeCount }} trades</span>
                    </div>
                    <button class="btn-del" (click)="deleteHistory(h.id, $event)">🗑️</button>
                  </div>
                }
              </div>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .backtest-page { padding: 1.5rem; max-width: 1200px; margin: 0 auto; }
    .page-header { margin-bottom: 1.5rem; }
    .page-header h1 { margin: 0 0 0.25rem; font-size: 1.6rem; }
    .subtitle { color: var(--text-muted); margin: 0; font-size: 0.9rem; }

    .backtest-layout { display: grid; grid-template-columns: 340px 1fr; gap: 1.5rem; }

    .config-panel { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--border-radius); padding: 1.5rem; display: flex; flex-direction: column; gap: 1rem; align-self: start; }
    .config-panel h3 { margin: 0 0 0.5rem; font-size: 1.1rem; }

    .form-group { display: flex; flex-direction: column; gap: 0.4rem; }
    .form-group label { font-size: 0.8rem; font-weight: 600; color: var(--text-muted); }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
    .form-control { padding: 0.6rem 0.875rem; border: 1px solid var(--border-color); border-radius: var(--border-radius); background: var(--bg-input); color: var(--text-main); font-size: 0.875rem; width: 100%; box-sizing: border-box; }
    .form-control:focus { outline: none; border-color: var(--primary); }

    .rules-section { display: flex; flex-direction: column; gap: 0.5rem; }
    .rules-header { display: flex; justify-content: space-between; align-items: center; }
    .rules-header label { font-size: 0.8rem; font-weight: 600; color: var(--text-muted); }
    .btn-add-rule { background: var(--primary); color: white; border: none; border-radius: 4px; padding: 0.25rem 0.6rem; font-size: 0.8rem; cursor: pointer; }
    .filter-row { display: flex; gap: 0.4rem; align-items: center; flex-wrap: wrap; }
    .form-control-sm { padding: 0.4rem 0.6rem; border: 1px solid var(--border-color); border-radius: 6px; background: var(--bg-input); color: var(--text-main); font-size: 0.8rem; flex: 1; min-width: 0; }
    .btn-remove { background: none; border: none; color: var(--text-muted); cursor: pointer; padding: 0.2rem; font-size: 0.85rem; }
    .btn-remove:hover { color: var(--danger); }

    .checkbox-label { display: flex; align-items: center; gap: 0.5rem; font-size: 0.85rem; cursor: pointer; }

    .btn { padding: 0.75rem; border-radius: var(--border-radius); font-size: 0.875rem; font-weight: 600; cursor: pointer; border: none; width: 100%; }
    .btn-primary { background: var(--primary); color: white; }
    .btn-primary:hover:not(:disabled) { opacity: 0.9; }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }

    /* Results */
    .results-panel { display: flex; flex-direction: column; gap: 1.5rem; }
    .results-grid { display: grid; grid-template-columns: repeat(5, 1fr); gap: 0.75rem; }
    .metric-card { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--border-radius); padding: 0.875rem 1rem; }
    .metric-card.highlight { border-color: var(--primary); background: var(--primary-light); }
    .metric-label { font-size: 0.72rem; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.04em; margin-bottom: 0.25rem; }
    .metric-value { font-size: 1.2rem; font-weight: 700; }
    .metric-value.positive { color: #10b981; }
    .metric-value.negative { color: #ef4444; }

    .chart-section { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--border-radius); padding: 1rem 1.25rem; }
    .chart-section h4 { margin: 0 0 0.75rem; font-size: 0.9rem; color: var(--text-muted); }
    .equity-chart { width: 100%; height: 120px; }
    .equity-chart svg { width: 100%; height: 100%; }

    .monte-section { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--border-radius); padding: 1rem 1.25rem; }
    .monte-section h4 { margin: 0 0 0.75rem; font-size: 0.9rem; color: var(--text-muted); }
    .monte-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 0.75rem; margin-bottom: 0.75rem; }
    .monte-card { padding: 0.75rem; border-radius: var(--border-radius); border: 1px solid var(--border-color); }
    .monte-card.p5 { border-color: #ef4444; background: rgba(239,68,68,0.05); }
    .monte-card.median { border-color: var(--primary); background: var(--primary-light); }
    .monte-card.p95 { border-color: #10b981; background: rgba(16,185,129,0.05); }
    .monte-label { font-size: 0.72rem; color: var(--text-muted); margin-bottom: 0.25rem; }
    .monte-value { font-size: 1.1rem; font-weight: 700; }
    .monte-dd { font-size: 0.75rem; color: var(--text-muted); margin-top: 0.25rem; }
    .ruin-prob { font-size: 0.85rem; color: var(--text-muted); }
    .ruin-prob.danger { color: #ef4444; }

    .no-result { text-align: center; padding: 3rem; color: var(--text-muted); background: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--border-radius); }
    .nr-icon { font-size: 2.5rem; margin-bottom: 0.75rem; }

    .history-section { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--border-radius); padding: 1rem 1.25rem; }
    .history-section h4 { margin: 0 0 0.75rem; font-size: 0.9rem; color: var(--text-muted); }
    .history-list { display: flex; flex-direction: column; gap: 0.5rem; }
    .history-item { display: flex; align-items: center; gap: 1rem; padding: 0.6rem 0.875rem; border: 1px solid var(--border-color); border-radius: var(--border-radius); cursor: pointer; transition: var(--transition); }
    .history-item:hover { border-color: var(--primary); background: var(--primary-light); }
    .h-name { flex: 1; font-size: 0.875rem; font-weight: 600; }
    .h-stats { display: flex; gap: 0.75rem; font-size: 0.8rem; color: var(--text-muted); }
    .h-stats .pos { color: #10b981; }
    .h-stats .neg { color: #ef4444; }
    .btn-del { background: none; border: none; cursor: pointer; font-size: 0.9rem; }
    .btn-del:hover { opacity: 0.7; }
  `]
})
export class BacktestComponent implements OnInit {
  result = signal<BacktestResult | null>(null);
  history = signal<BacktestResult[]>([]);
  running = signal(false);

  form: BacktestRequest = {
    name: 'My Test',
    initialBalance: 10000,
    runMonteCarlo: true,
    rules: []
  };

  constructor(private api: ApiService, private toast: ToastService) {}

  ngOnInit() {
    this.api.getBacktestHistory().subscribe({ next: (h) => this.history.set(h) });
  }

  addFilter() {
    this.form.rules.push({ ruleType: BacktestRuleType.MinRRR, minValue: 1.5 });
  }

  removeFilter(i: number) {
    this.form.rules.splice(i, 1);
  }

  onRuleTypeChange(i: number) {
    this.form.rules[i].minValue = undefined;
    this.form.rules[i].maxValue = undefined;
    this.form.rules[i].stringValue = undefined;
  }

  runTest() {
    if (!this.form.name.trim()) { this.toast.error('Test name is required'); return; }
    this.running.set(true);
    this.api.runBacktest(this.form).subscribe({
      next: (r) => {
        this.result.set(r);
        this.history.update(h => [r, ...h]);
        this.running.set(false);
        this.toast.success('Backtest complete');
      },
      error: () => { this.toast.error('Backtest failed'); this.running.set(false); }
    });
  }

  viewHistory(h: BacktestResult) {
    this.result.set(h);
  }

  deleteHistory(id: string, event: Event) {
    event.stopPropagation();
    this.api.deleteBacktest(id).subscribe({
      next: () => {
        this.history.update(h => h.filter(x => x.id !== id));
        if (this.result()?.id === id) this.result.set(null);
        this.toast.success('Deleted');
      }
    });
  }

  equityPoints(): string {
    const curve = this.result()?.equityCurve;
    if (!curve || curve.length < 2) return '';
    const min = Math.min(...curve.map(p => p.balance));
    const max = Math.max(...curve.map(p => p.balance));
    const range = max - min || 1;
    return curve.map((p, i) => {
      const x = (i / (curve.length - 1)) * 600;
      const y = 120 - ((p.balance - min) / range) * 110 - 5;
      return `${x},${y}`;
    }).join(' ');
  }
}
