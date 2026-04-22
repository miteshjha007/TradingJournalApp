import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DecimalPipe, CurrencyPipe } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { AiAnalysis, PerformanceMetrics, DrawdownInfo } from '../../models/models';


@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, DecimalPipe, CurrencyPipe],
  template: `
    <div class="page-wrapper">
      <h1 class="page-title-h1">Analytics & AI Insights</h1>

      <!-- AI Score Banner -->
      @if (aiAnalysis()) {
        <div class="ai-score-banner">
          <div class="ai-score-content">
            <div class="ai-icon">🤖</div>
            <div>
              <h2>Trading Score: <span class="score-value">{{ aiAnalysis()!.overallScore }}</span></h2>
              <p>Best Instrument: <strong>{{ aiAnalysis()!.bestInstrument }}</strong> | Best Time: <strong>{{ aiAnalysis()!.bestTimeOfDay }}</strong></p>
            </div>
          </div>
        </div>
      }

      @if (loading()) {
        <div class="loading-state"><div class="loading-spinner"></div></div>
      } @else {
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
                  <span class="metric-label">Sharpe Ratio</span>
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
                  <span class="metric-label">Max Consec. Losses</span>
                  <span class="metric-value">{{ metrics()!.maxConsecutiveLosses }}</span>
                </div>
                <div class="metric-item">
                  <span class="metric-label">Profit Factor</span>
                  <span class="metric-value">{{ metrics()!.profitFactor | number:'1.2-2' }}</span>
                </div>
                <div class="metric-item">
                  <span class="metric-label">Expected Value</span>
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
    </div>
  `
})
export class AnalyticsComponent implements OnInit {
  loading = signal(true);
  aiAnalysis = signal<AiAnalysis | null>(null);
  metrics = signal<PerformanceMetrics | null>(null);
  drawdown = signal<DrawdownInfo | null>(null);

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
}
