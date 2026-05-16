import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DecimalPipe, CurrencyPipe } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { AiAnalysis, PerformanceMetrics, DrawdownInfo, HeatmapData, ShadowProfile } from '../../models/models';
import { InfoTooltipDirective } from '../../directives/info-tooltip.directive';


@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, DecimalPipe, CurrencyPipe, InfoTooltipDirective],
  template: `
    <div class="page-wrapper">
      <h1 class="page-title-h1">Analytics & AI Insights</h1>

      <!-- Tab Nav -->
      <div class="tab-nav" style="margin-bottom:1.5rem">
        <button class="tab-btn" [class.active]="activeTab() === 'insights'" (click)="activeTab.set('insights')">🤖 AI Insights</button>
        <button class="tab-btn" [class.active]="activeTab() === 'heatmap'" (click)="loadHeatmap()">🔥 Trade Heatmap</button>
        <button class="tab-btn" [class.active]="activeTab() === 'shadow'" (click)="loadShadow()">🪞 Journal DNA</button>
      </div>

      @if (loading()) {
        <div class="loading-state"><div class="loading-spinner"></div></div>
      }

      <!-- AI Insights Tab -->
      @if (activeTab() === 'insights' && !loading()) {

      <!-- AI Score Banner -->
      @if (aiAnalysis()) {
        <div class="ai-score-banner">
          <div class="ai-score-content">
            <div class="ai-icon">🤖</div>
            <div>
              <h2>Trading Score: <span class="score-value" [infoTooltip]="'trading-score'">{{ aiAnalysis()!.overallScore }}</span></h2>
              <p>Best Instrument: <strong>{{ aiAnalysis()!.bestInstrument }}</strong> | Best Time: <strong>{{ aiAnalysis()!.bestTimeOfDay }}</strong></p>
            </div>
          </div>
        </div>
      }

        <div class="analytics-grid">
          <!-- AI Insights -->
          <div class="section-card span-full">
            <h3>🤖 AI Insights & Recommendations</h3>
            <div class="insights-grid">
              @for (insight of aiAnalysis()?.insights; track insight.title) {
                <div class="insight-card" [class]="'severity-' + insight.severity.toLowerCase()">
                  <div class="insight-header">
                    <span class="insight-icon">{{ insight.icon }}</span>
                    <div>
                      <span class="insight-severity">{{ insight.severity }}</span>
                      <h4>{{ insight.title }}</h4>
                    </div>
                  </div>
                  <p class="insight-message">{{ insight.message }}</p>
                  <div class="insight-rec">
                    <span class="rec-label">💡 Recommendation:</span>
                    <span>{{ insight.recommendation }}</span>
                  </div>
                </div>
              }
            </div>
          </div>

          <!-- Performance Metrics -->
          <div class="section-card">
            <h3>📊 Performance Metrics</h3>
            @if (metrics()) {
              <div class="metrics-grid">
                <div class="metric-item">
                  <span class="metric-label" [infoTooltip]="'sharpe-ratio'">Sharpe Ratio</span>
                  <span class="metric-value">{{ metrics()!.sharpeRatio | number:'1.2-2' }}</span>
                </div>
                <div class="metric-item">
                  <span class="metric-label">Avg Win</span>
                  <span class="metric-value positive-text">{{ metrics()!.averageWin | currency }}</span>
                </div>
                <div class="metric-item">
                  <span class="metric-label">Avg Loss</span>
                  <span class="metric-value negative-text">{{ metrics()!.averageLoss | currency }}</span>
                </div>
                <div class="metric-item">
                  <span class="metric-label">Largest Win</span>
                  <span class="metric-value positive-text">{{ metrics()!.largestWin | currency }}</span>
                </div>
                <div class="metric-item">
                  <span class="metric-label">Largest Loss</span>
                  <span class="metric-value negative-text">{{ metrics()!.largestLoss | currency }}</span>
                </div>
                <div class="metric-item">
                  <span class="metric-label">Max Consec. Wins</span>
                  <span class="metric-value">{{ metrics()!.maxConsecutiveWins }}</span>
                </div>
                <div class="metric-item">
                  <span class="metric-label" [infoTooltip]="'consecutive-losses'">Max Consec. Losses</span>
                  <span class="metric-value">{{ metrics()!.maxConsecutiveLosses }}</span>
                </div>
                <div class="metric-item">
                  <span class="metric-label" [infoTooltip]="'profit-factor'">Profit Factor</span>
                  <span class="metric-value">{{ metrics()!.profitFactor | number:'1.2-2' }}</span>
                </div>
                <div class="metric-item">
                  <span class="metric-label" [infoTooltip]="'expected-value'">Expected Value</span>
                  <span class="metric-value" [class.positive-text]="metrics()!.expectedValue > 0" [class.negative-text]="metrics()!.expectedValue < 0">
                    {{ metrics()!.expectedValue | currency }}
                  </span>
                </div>
              </div>
            }
          </div>

          <!-- Drawdown Card -->
          <div class="section-card">
            <h3>📉 Drawdown Analysis</h3>
            @if (drawdown()) {
              <div class="drawdown-content">
                <div class="drawdown-gauge">
                  <div class="gauge-track">
                    <div class="gauge-fill" [style.width.%]="drawdown()!.currentDrawdownPercent" [class.critical]="drawdown()!.isCritical" [class.warning]="drawdown()!.isWarning && !drawdown()!.isCritical"></div>
                  </div>
                  <span>{{ drawdown()!.currentDrawdownPercent | number:'1.1-1' }}% Current Drawdown</span>
                </div>
                <div class="drawdown-stats">
                  <div class="stat-row">
                    <span>Current Drawdown</span>
                    <span class="negative-text">{{ drawdown()!.currentDrawdown | currency }}</span>
                  </div>
                  <div class="stat-row">
                    <span>Max Drawdown</span>
                    <span class="negative-text">{{ drawdown()!.maxDrawdown | currency }}</span>
                  </div>
                  <div class="stat-row">
                    <span>Max Drawdown %</span>
                    <span>{{ drawdown()!.maxDrawdownPercent | number:'1.1-1' }}%</span>
                  </div>
                  <div class="stat-row">
                    <span>Account Balance</span>
                    <span>{{ drawdown()!.accountBalance | currency }}</span>
                  </div>
                </div>
                @if (drawdown()!.isCritical) {
                  <div class="drawdown-alert critical">🚨 Critical drawdown level reached!</div>
                } @else if (drawdown()!.isWarning) {
                  <div class="drawdown-alert warning">⚠️ Approaching drawdown limit</div>
                } @else {
                  <div class="drawdown-alert safe">✅ Drawdown within safe limits</div>
                }
              </div>
            }
          </div>
        </div>
      }

      <!-- Heatmap Tab -->
      @if (activeTab() === 'heatmap') {
        <div class="section-card">
          <h3>🔥 Trading Performance Heatmap</h3>
          <p style="color:var(--text-muted);font-size:0.85rem;margin-bottom:1rem">Hour-of-day vs Day-of-week. Color = avg P/L (green = best, red = worst).</p>

          @if (heatmap()) {
            <!-- Session Bands -->
            <div class="session-bands">
              @for (s of heatmap()!.sessions; track s.name) {
                <div class="session-band" [style.background]="sessionColor(s.name)" style="color:white">
                  <span [infoTooltip]="sessionTooltipKey(s.name)">{{ s.name }}</span>: {{ s.totalPL | currency }} ({{ s.tradeCount }} trades)
                </div>
              }
            </div>

            <!-- Heatmap Grid Header (Hours) -->
            <div class="heatmap-grid">
              <div class="heatmap-label"></div>
              @for (h of hours; track h) {
                <div class="heatmap-label" style="text-align:center">{{ h }}</div>
              }
              @for (day of days; track day.value) {
                <div class="heatmap-label" style="font-weight:600">{{ day.label }}</div>
                @for (h of hours; track h) {
                  <div class="heatmap-cell"
                    [style.background]="cellColor(day.value, h)"
                    [title]="cellTooltip(day.value, h)">
                  </div>
                }
              }
            </div>

            @if (heatmap()!.bestSlot || heatmap()!.worstSlot) {
              <div style="display:flex;gap:1rem;margin-top:1rem">
                @if (heatmap()!.bestSlot) {
                  <div style="padding:0.75rem;background:rgba(16,185,129,0.1);border:1px solid #10b981;border-radius:8px;font-size:0.8rem">
                    ✅ Best: {{ dayName(heatmap()!.bestSlot!.dayOfWeek) }} {{ heatmap()!.bestSlot!.hour }}:00 — {{ heatmap()!.bestSlot!.avgPL | currency }}
                  </div>
                }
                @if (heatmap()!.worstSlot) {
                  <div style="padding:0.75rem;background:rgba(239,68,68,0.1);border:1px solid #ef4444;border-radius:8px;font-size:0.8rem">
                    ❌ Worst: {{ dayName(heatmap()!.worstSlot!.dayOfWeek) }} {{ heatmap()!.worstSlot!.hour }}:00 — {{ heatmap()!.worstSlot!.avgPL | currency }}
                  </div>
                }
              </div>
            }
          } @else {
            <div class="loading-state"><div class="loading-spinner"></div></div>
          }
        </div>
      }

      <!-- Shadow / Journal DNA Tab -->
      @if (activeTab() === 'shadow') {
        <div class="analytics-grid">
          @if (shadow()) {
            <div class="section-card span-full">
              <h3>🧬 Trading DNA</h3>
              <div class="dna-score" style="font-size:1.3rem;font-weight:700;margin-bottom:1rem">
                Consistency Score: <span [style.color]="disciplineColor(shadow()!.consistencyScore)">{{ shadow()!.consistencyScore }}/100</span>
              </div>
              <div class="dna-string" style="font-family:monospace;font-size:0.8rem;color:var(--text-muted);word-break:break-all;margin-bottom:1.5rem">{{ shadow()!.dna }}</div>
            </div>

            <div class="section-card">
              <h3>✅ Winning Patterns</h3>
              @for (r of shadow()!.winningRules; track r.description) {
                <div class="dna-rule positive" style="display:flex;justify-content:space-between;padding:0.6rem;border-bottom:1px solid var(--border-color)">
                  <span>{{ r.description }}</span>
                  <span style="color:#10b981;font-weight:600">+{{ r.impact | currency }}</span>
                </div>
              }
            </div>

            <div class="section-card">
              <h3>❌ Losing Patterns</h3>
              @for (r of shadow()!.losingRules; track r.description) {
                <div class="dna-rule negative" style="display:flex;justify-content:space-between;padding:0.6rem;border-bottom:1px solid var(--border-color)">
                  <span>{{ r.description }}</span>
                  <span style="color:#ef4444;font-weight:600">{{ r.impact | currency }}</span>
                </div>
              }
            </div>

            <div class="section-card">
              <h3>🏆 Best Setups</h3>
              @for (p of shadow()!.bestPatterns; track p.label) {
                <div style="display:flex;justify-content:space-between;align-items:center;padding:0.6rem;border-bottom:1px solid var(--border-color)">
                  <div>
                    <div style="font-weight:600">{{ p.label }}: {{ p.value }}</div>
                    <div style="font-size:0.75rem;color:var(--text-muted)">{{ p.tradeCount }} trades</div>
                  </div>
                  <span style="color:#10b981;font-weight:700">{{ p.avgPL | currency }}</span>
                </div>
              }
            </div>

            <div class="section-card">
              <h3>⚠️ Worst Setups</h3>
              @for (p of shadow()!.worstPatterns; track p.label) {
                <div style="display:flex;justify-content:space-between;align-items:center;padding:0.6rem;border-bottom:1px solid var(--border-color)">
                  <div>
                    <div style="font-weight:600">{{ p.label }}: {{ p.value }}</div>
                    <div style="font-size:0.75rem;color:var(--text-muted)">{{ p.tradeCount }} trades</div>
                  </div>
                  <span style="color:#ef4444;font-weight:700">{{ p.avgPL | currency }}</span>
                </div>
              }
            </div>
          } @else {
            <div class="section-card span-full">
              <div class="loading-state"><div class="loading-spinner"></div></div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class AnalyticsComponent implements OnInit {
  loading = signal(true);
  aiAnalysis = signal<AiAnalysis | null>(null);
  metrics = signal<PerformanceMetrics | null>(null);
  drawdown = signal<DrawdownInfo | null>(null);
  heatmap = signal<HeatmapData | null>(null);
  shadow = signal<ShadowProfile | null>(null);
  activeTab = signal<'insights' | 'heatmap' | 'shadow'>('insights');

  days = [
    { value: 0, label: 'Sun' }, { value: 1, label: 'Mon' }, { value: 2, label: 'Tue' },
    { value: 3, label: 'Wed' }, { value: 4, label: 'Thu' }, { value: 5, label: 'Fri' }, { value: 6, label: 'Sat' }
  ];
  hours = Array.from({ length: 24 }, (_, i) => i);

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    Promise.all([
      this.api.getAiInsights().toPromise(),
      this.api.getPerformance().toPromise(),
      this.api.getDrawdown().toPromise()
    ]).then(([ai, metrics, dd]) => {
      this.aiAnalysis.set(ai || null);
      this.metrics.set(metrics || null);
      this.drawdown.set(dd || null);
      this.loading.set(false);
    }).catch(() => this.loading.set(false));
  }

  loadHeatmap() {
    this.activeTab.set('heatmap');
    if (this.heatmap()) return;
    this.api.getHeatmap().subscribe({ next: (h) => this.heatmap.set(h), error: () => {} });
  }

  loadShadow() {
    this.activeTab.set('shadow');
    if (this.shadow()) return;
    this.api.getShadowProfile().subscribe({ next: (s) => this.shadow.set(s), error: () => {} });
  }

  cellColor(day: number, hour: number): string {
    const cell = this.heatmap()?.cells.find(c => c.dayOfWeek === day && c.hour === hour);
    if (!cell || cell.tradeCount === 0) return 'var(--bg-hover)';
    const i = cell.intensity;
    if (i > 0) return `rgba(16,185,129,${Math.min(i, 1) * 0.8 + 0.1})`;
    return `rgba(239,68,68,${Math.min(Math.abs(i), 1) * 0.8 + 0.1})`;
  }

  cellTooltip(day: number, hour: number): string {
    const cell = this.heatmap()?.cells.find(c => c.dayOfWeek === day && c.hour === hour);
    if (!cell || cell.tradeCount === 0) return 'No trades';
    return `${cell.tradeCount} trades | WR: ${(cell.winRate * 100).toFixed(0)}% | Avg: $${cell.avgPL.toFixed(2)}`;
  }

  dayName(d: number): string {
    return ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'][d] ?? '';
  }

  sessionColor(name: string): string {
    if (name.includes('London')) return '#6366f1';
    if (name.includes('New York') || name.includes('NY')) return '#10b981';
    if (name.includes('Asia')) return '#f59e0b';
    return '#8b5cf6';
  }

  sessionTooltipKey(name: string): string {
    if (name.includes('London')) return 'london-session';
    if (name.includes('New York') || name.includes('NY')) return 'new-york-session';
    if (name.includes('Asia')) return 'asia-session';
    return '';
  }

  disciplineColor(score: number): string {
    if (score >= 80) return '#10b981';
    if (score >= 60) return '#f59e0b';
    return '#ef4444';
  }
}
