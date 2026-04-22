import { Component, OnInit, signal, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule, DecimalPipe, CurrencyPipe } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { DashboardSummary } from '../../models/models';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, DecimalPipe, CurrencyPipe],
  template: `
    <div class="page-wrapper">
      <!-- Loss Alert -->
      @if (dashboard()?.dailyLossLimitBreached) {
        <div class="alert-banner critical">
          🚨 Daily Loss Limit Breached! Consider stopping trading for today.
          Limit: {{ dashboard()?.dailyLossLimit | currency }} | Today's Loss: {{ dashboard()?.todayPL | currency }}
        </div>
      }

      <!-- Drawdown Warning -->
      @if (drawdownWarning()) {
        <div class="alert-banner warning">
          ⚠️ High Drawdown Alert: {{ dashboard()?.currentDrawdown | currency }} current drawdown
        </div>
      }

      @if (loading()) {
        <div class="loading-state">
          <div class="loading-spinner"></div>
          <p>Loading your dashboard...</p>
        </div>
      } @else {
        <!-- KPI Cards Row 1 -->
        <div class="kpi-grid">
          <div class="kpi-card" [class.positive]="(dashboard()?.totalProfitLoss ?? 0) > 0" [class.negative]="(dashboard()?.totalProfitLoss ?? 0) < 0">
            <div class="kpi-icon">💰</div>
            <div class="kpi-content">
              <span class="kpi-label">Total P&amp;L</span>
              <span class="kpi-value" [class.positive-text]="(dashboard()?.totalProfitLoss ?? 0) > 0" [class.negative-text]="(dashboard()?.totalProfitLoss ?? 0) < 0">
                {{ (dashboard()?.totalProfitLoss ?? 0) | currency }}
              </span>
            </div>
          </div>

          <div class="kpi-card">
            <div class="kpi-icon">🏆</div>
            <div class="kpi-content">
              <span class="kpi-label">Win Rate</span>
              <span class="kpi-value">{{ dashboard()?.winRate | number:'1.1-1' }}%</span>
              <span class="kpi-sub">{{ dashboard()?.winCount }}W / {{ dashboard()?.lossCount }}L</span>
            </div>
          </div>

          <div class="kpi-card">
            <div class="kpi-icon">📊</div>
            <div class="kpi-content">
              <span class="kpi-label">Total Trades</span>
              <span class="kpi-value">{{ dashboard()?.totalTrades }}</span>
              <span class="kpi-sub">Today: {{ dashboard()?.todayTradeCount }}</span>
            </div>
          </div>

          <div class="kpi-card">
            <div class="kpi-icon">⚖️</div>
            <div class="kpi-content">
              <span class="kpi-label">Avg RRR</span>
              <span class="kpi-value">{{ dashboard()?.averageRiskRewardRatio | number:'1.2-2' }}</span>
              <span class="kpi-sub">Risk:Reward</span>
            </div>
          </div>

          <div class="kpi-card" [class.danger]="(dashboard()?.currentDrawdown ?? 0) > 0">
            <div class="kpi-icon">📉</div>
            <div class="kpi-content">
              <span class="kpi-label">Drawdown</span>
              <span class="kpi-value negative-text">{{ dashboard()?.currentDrawdown | currency }}</span>
              <span class="kpi-sub">Max: {{ dashboard()?.maxDrawdown | currency }}</span>
            </div>
          </div>

          <div class="kpi-card" [class.positive]="(dashboard()?.todayPL ?? 0) > 0" [class.negative]="(dashboard()?.todayPL ?? 0) < 0">
            <div class="kpi-icon">📅</div>
            <div class="kpi-content">
              <span class="kpi-label">Today P&amp;L</span>
              <span class="kpi-value" [class.positive-text]="(dashboard()?.todayPL ?? 0) > 0" [class.negative-text]="(dashboard()?.todayPL ?? 0) < 0">
                {{ dashboard()?.todayPL | currency }}
              </span>
            </div>
          </div>
        </div>

        <!-- Period Summary -->
        <div class="period-grid">
          <div class="period-card">
            <span class="period-label">This Week</span>
            <span class="period-value" [class.positive-text]="(dashboard()?.weekPL ?? 0) > 0" [class.negative-text]="(dashboard()?.weekPL ?? 0) < 0">
              {{ dashboard()?.weekPL | currency }}
            </span>
          </div>
          <div class="period-card">
            <span class="period-label">This Month</span>
            <span class="period-value" [class.positive-text]="(dashboard()?.monthPL ?? 0) > 0" [class.negative-text]="(dashboard()?.monthPL ?? 0) < 0">
              {{ dashboard()?.monthPL | currency }}
            </span>
          </div>
          <div class="period-card">
            <span class="period-label">Account Balance</span>
            <span class="period-value">{{ dashboard()?.accountBalance | currency }}</span>
          </div>
        </div>

        <!-- Charts Row -->
        <div class="charts-grid">
          <div class="chart-card">
            <h3>📈 Equity Curve</h3>
            <div class="chart-container">
              <canvas #equityChart></canvas>
            </div>
          </div>
          <div class="chart-card">
            <h3>📊 Monthly P&amp;L</h3>
            <div class="chart-container">
              <canvas #monthlyChart></canvas>
            </div>
          </div>
        </div>

        <!-- Instrument Performance -->
        <div class="section-card">
          <h3>🎯 Instrument Performance</h3>
          <div class="instrument-perf-grid">
            @for (instr of dashboard()?.instrumentPerformance; track instr.instrumentName) {
              <div class="instr-perf-card" [class.positive]="instr.totalPL > 0" [class.negative]="instr.totalPL < 0">
                <div class="instr-name">{{ instr.instrumentName }}</div>
                <div class="instr-stats">
                  <span class="instr-pl" [class.positive-text]="instr.totalPL > 0" [class.negative-text]="instr.totalPL < 0">{{ instr.totalPL | currency }}</span>
                  <span class="instr-winrate">{{ instr.winRate | number:'1.1-1' }}% WR</span>
                  <span class="instr-trades">{{ instr.totalTrades }} trades</span>
                </div>
              </div>
            }
          </div>
        </div>
      }
    </div>
  `
})
export class DashboardComponent implements OnInit, AfterViewInit {
  @ViewChild('equityChart') equityChartRef!: ElementRef;
  @ViewChild('monthlyChart') monthlyChartRef!: ElementRef;

  dashboard = signal<DashboardSummary | null>(null);
  loading = signal(true);
  drawdownWarning = signal(false);
  private equityChartInstance: Chart | null = null;
  private monthlyChartInstance: Chart | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getDashboard().subscribe({
      next: (data) => {
        this.dashboard.set(data);
        this.loading.set(false);
        this.drawdownWarning.set(data.currentDrawdown > data.maxDrawdown * 0.7);
        setTimeout(() => this.initCharts(), 100);
      },
      error: () => this.loading.set(false)
    });
  }

  ngAfterViewInit(): void {}

  private initCharts(): void {
    const data = this.dashboard();
    if (!data) return;

    // Equity Curve
    if (this.equityChartRef && data.equityCurve.length > 0) {
      if (this.equityChartInstance) this.equityChartInstance.destroy();
      const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
      const textColor = isDark ? '#a0aec0' : '#4a5568';
      const gridColor = isDark ? 'rgba(255,255,255,0.05)' : 'rgba(0,0,0,0.05)';

      this.equityChartInstance = new Chart(this.equityChartRef.nativeElement, {
        type: 'line',
        data: {
          labels: data.equityCurve.map(p => new Date(p.date).toLocaleDateString()),
          datasets: [{
            label: 'Account Balance',
            data: data.equityCurve.map(p => p.balance),
            borderColor: '#6366f1',
            backgroundColor: 'rgba(99, 102, 241, 0.1)',
            fill: true,
            tension: 0.4,
            pointRadius: 3,
            pointHoverRadius: 6
          }]
        },
        options: {
          responsive: true, maintainAspectRatio: false,
          plugins: { legend: { labels: { color: textColor } } },
          scales: {
            x: { ticks: { color: textColor, maxTicksLimit: 8 }, grid: { color: gridColor } },
            y: { ticks: { color: textColor, callback: v => '$' + v }, grid: { color: gridColor } }
          }
        }
      });
    }

    // Monthly P&L
    if (this.monthlyChartRef && data.monthlyPL.length > 0) {
      if (this.monthlyChartInstance) this.monthlyChartInstance.destroy();
      const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
      const textColor = isDark ? '#a0aec0' : '#4a5568';
      const gridColor = isDark ? 'rgba(255,255,255,0.05)' : 'rgba(0,0,0,0.05)';

      this.monthlyChartInstance = new Chart(this.monthlyChartRef.nativeElement, {
        type: 'bar',
        data: {
          labels: data.monthlyPL.map(m => m.month),
          datasets: [{
            label: 'P&L',
            data: data.monthlyPL.map(m => m.profitLoss),
            backgroundColor: data.monthlyPL.map(m => m.profitLoss >= 0 ? 'rgba(16,185,129,0.8)' : 'rgba(239,68,68,0.8)'),
            borderRadius: 6,
            borderSkipped: false
          }]
        },
        options: {
          responsive: true, maintainAspectRatio: false,
          plugins: { legend: { labels: { color: textColor } } },
          scales: {
            x: { ticks: { color: textColor }, grid: { color: gridColor } },
            y: { ticks: { color: textColor, callback: v => '$' + v }, grid: { color: gridColor } }
          }
        }
      });
    }
  }
}
