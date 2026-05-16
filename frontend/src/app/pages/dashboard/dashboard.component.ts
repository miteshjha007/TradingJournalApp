import { Component, OnInit, signal, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule, DecimalPipe, CurrencyPipe } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { DashboardSummary, PropFirmStatus, Streak } from '../../models/models';
import { Chart, registerables } from 'chart.js';
import { InfoTooltipDirective } from '../../directives/info-tooltip.directive';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, DecimalPipe, CurrencyPipe, InfoTooltipDirective],
  template: `
    <div class="page-wrapper">
      <!-- Loss Alert -->
      @if (dashboard()?.dailyLossLimitBreached) {
        <div class="alert-banner critical">
          🚨 Daily Loss Limit Breached! Consider stopping trading for today.
          Limit: {{ dashboard()?.dailyLossLimit | currency }} | Today's Loss: {{ dashboard()?.todayPL | currency }}
        </div>
      }
      
      <!-- Prop Firm Kill Switch -->
      @if (dashboard()?.isPropFirm && (dashboard()?.todayPL ?? 0) < -(dashboard()?.dailyLossLimit ?? 0) * 0.8) {
        <div class="alert-banner critical" style="background:#ef4444; color:white; border:2px solid #b91c1c;">
          🛑 DANGER: You are approaching your Prop Firm Daily Drawdown Limit! Stop trading immediately.
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
              <span class="kpi-label" [infoTooltip]="'win-rate'">Win Rate</span>
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
              <span class="kpi-label" [infoTooltip]="'risk-reward-ratio'">Avg RRR</span>
              <span class="kpi-value">{{ dashboard()?.averageRiskRewardRatio | number:'1.2-2' }}</span>
              <span class="kpi-sub">Risk:Reward</span>
            </div>
          </div>

          <div class="kpi-card" [class.danger]="(dashboard()?.currentDrawdown ?? 0) > 0">
            <div class="kpi-icon">📉</div>
            <div class="kpi-content">
              <span class="kpi-label" [infoTooltip]="'current-drawdown'">Drawdown</span>
              <span class="kpi-value negative-text">{{ dashboard()?.currentDrawdown | currency }}</span>
              <span class="kpi-sub">Max: <span [infoTooltip]="'max-drawdown'">{{ dashboard()?.maxDrawdown | currency }}</span></span>
            </div>
          </div>

          <div class="kpi-card" [class.positive]="(dashboard()?.todayPL ?? 0) > 0" [class.negative]="(dashboard()?.todayPL ?? 0) < 0">
            <div class="kpi-icon">📅</div>
            <div class="kpi-content">
              <span class="kpi-label" [infoTooltip]="'daily-loss-limit'">Today P&amp;L</span>
              <span class="kpi-value" [class.positive-text]="(dashboard()?.todayPL ?? 0) > 0" [class.negative-text]="(dashboard()?.todayPL ?? 0) < 0">
                {{ dashboard()?.todayPL | currency }}
              </span>
            </div>
          </div>
        </div>

        <!-- Prop Firm Live Rule Engine Card -->
        @if (propFirmStatus()) {
          <div class="prop-firm-engine-card" [style.border-color]="propFirmStatus()!.statusColor + '44'">
            <!-- Header -->
            <div class="pfe-header">
              <div class="pfe-title-row">
                <span style="font-size:1.4rem">🏦</span>
                <div>
                  <div class="pfe-firm-name">{{ propFirmStatus()!.firmName }}</div>
                  <div class="pfe-plan-name">{{ propFirmStatus()!.planName }}</div>
                </div>
              </div>
              <div class="pfe-status-badge" [style.background]="propFirmStatus()!.statusColor">
                @switch (propFirmStatus()!.accountStatus) {
                  @case ('SAFE')            { <span>&#x1F7E2; SAFE TO TRADE</span> }
                  @case ('WARNING')         { <span>&#x1F7E1; WARNING</span> }
                  @case ('CRITICAL')        { <span>&#x1F7E0; CRITICAL</span> }
                  @case ('BREACHED_DAILY')  { <span>&#x1F534; STOP &mdash; DAILY LIMIT</span> }
                  @case ('BREACHED_OVERALL'){ <span>&#x1F534; ACCOUNT AT RISK</span> }
                  @case ('PASSED')          { <span>&#x1F389; CHALLENGE PASSED!</span> }
                }
              </div>
            </div>

            <!-- Active Warnings -->
            @if (propFirmStatus()!.activeWarnings.length > 0) {
              <div class="pfe-warnings">
                @for (w of propFirmStatus()!.activeWarnings; track w) {
                  <div class="pfe-warning-item" [style.border-left-color]="propFirmStatus()!.statusColor">{{ w }}</div>
                }
              </div>
            }

            <!-- Progress Bars Grid -->
            <div class="pfe-bars-grid">
              <!-- Daily Loss -->
              <div class="pfe-bar-item">
                <div class="pfe-bar-header">
                  <span>&#x1F4C9; <span [infoTooltip]="'daily-loss-limit'">Daily Loss Used</span></span>
                  <span class="pfe-bar-value" [style.color]="propFirmStatus()!.dailyLossUsedPct >= 70 ? propFirmStatus()!.statusColor : ''">
                    &#36;{{ propFirmStatus()!.dailyLossUsed | number:'1.2-2' }} / &#36;{{ propFirmStatus()!.dailyLossLimit | number:'1.2-2' }}
                  </span>
                </div>
                <div class="pfe-progress-track">
                  <div class="pfe-progress-fill"
                    [style.width]="min100(propFirmStatus()!.dailyLossUsedPct) + '%'"
                    [style.background]="getBarColor(propFirmStatus()!.dailyLossUsedPct)">
                  </div>
                </div>
                <div class="pfe-bar-sub">{{ propFirmStatus()!.dailyLossUsedPct | number:'1.1-1' }}% used &nbsp;&middot;&nbsp; &#36;{{ propFirmStatus()!.remainingDailyBudget | number:'1.2-2' }} remaining</div>
              </div>

              <!-- Overall Drawdown -->
              <div class="pfe-bar-item">
                <div class="pfe-bar-header">
                  <span>&#x1F4CA; Overall Drawdown</span>
                  <span class="pfe-bar-value" [style.color]="propFirmStatus()!.totalDrawdownPct >= 70 ? propFirmStatus()!.statusColor : ''">
                    &#36;{{ propFirmStatus()!.totalDrawdown | number:'1.2-2' }} / &#36;{{ propFirmStatus()!.maxDrawdownLimit | number:'1.2-2' }}
                  </span>
                </div>
                <div class="pfe-progress-track">
                  <div class="pfe-progress-fill"
                    [style.width]="min100(propFirmStatus()!.totalDrawdownPct) + '%'"
                    [style.background]="getBarColor(propFirmStatus()!.totalDrawdownPct)">
                  </div>
                </div>
                <div class="pfe-bar-sub">{{ propFirmStatus()!.totalDrawdownPct | number:'1.1-1' }}% used &nbsp;&middot;&nbsp; &#36;{{ propFirmStatus()!.remainingOverallBudget | number:'1.2-2' }} remaining</div>
              </div>

              <!-- Profit Target -->
              <div class="pfe-bar-item">
                <div class="pfe-bar-header">
                  <span>&#x1F3AF; <span [infoTooltip]="'profit-target'">Profit Target</span></span>
                  <span class="pfe-bar-value positive-text">
                    &#36;{{ propFirmStatus()!.profitEarned | number:'1.2-2' }} / &#36;{{ propFirmStatus()!.profitTarget | number:'1.2-2' }}
                  </span>
                </div>
                <div class="pfe-progress-track">
                  <div class="pfe-progress-fill"
                    [style.width]="min100(propFirmStatus()!.profitEarnedPct) + '%'"
                    style="background: #10b981">
                  </div>
                </div>
                <div class="pfe-bar-sub">{{ propFirmStatus()!.profitEarnedPct | number:'1.1-1' }}% achieved &nbsp;&middot;&nbsp; Est. payout: &#36;{{ propFirmStatus()!.estimatedPayout | number:'1.2-2' }}</div>
              </div>

              <!-- Trading Days -->
              @if (propFirmStatus()!.minTradingDaysRequired > 0) {
                <div class="pfe-bar-item">
                  <div class="pfe-bar-header">
                    <span>📅 Trading Days</span>
                    <span class="pfe-bar-value">{{ propFirmStatus()!.tradingDaysCompleted }} / {{ propFirmStatus()!.minTradingDaysRequired }} days</span>
                  </div>
                  <div class="pfe-progress-track">
                    <div class="pfe-progress-fill"
                      [style.width]="min100((propFirmStatus()!.tradingDaysCompleted / propFirmStatus()!.minTradingDaysRequired) * 100) + '%'"
                      style="background:#6366f1">
                    </div>
                  </div>
                </div>
              }
            </div>

            <!-- Rule Flags Row -->
            <div class="pfe-flags">
              <span class="pfe-flag" [class.flag-ok]="propFirmStatus()!.newsTradeAllowed" [class.flag-no]="!propFirmStatus()!.newsTradeAllowed">
                {{ propFirmStatus()!.newsTradeAllowed ? 'News Trading OK' : 'No News Trading' }}
              </span>
              <span class="pfe-flag" [class.flag-ok]="propFirmStatus()!.weekendHoldingAllowed" [class.flag-no]="!propFirmStatus()!.weekendHoldingAllowed">
                {{ propFirmStatus()!.weekendHoldingAllowed ? 'Weekend Hold OK' : 'No Weekend Hold' }}
              </span>
              <span class="pfe-flag" [class.flag-ok]="!propFirmStatus()!.has5xLotRule" [class.flag-no]="propFirmStatus()!.has5xLotRule">
                <span [infoTooltip]="'5x-lot-rule'">{{ propFirmStatus()!.has5xLotRule ? '5x Lot Rule ON' : 'No 5x Rule' }}</span>
              </span>
              <span class="pfe-flag" [class.flag-ok]="propFirmStatus()!.useDynamicEquity">
                <span [infoTooltip]="'dynamic-equity'">{{ propFirmStatus()!.useDynamicEquity ? 'Dynamic Equity' : 'Static Equity' }}</span>
              </span>
            </div>
          </div>
        }

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
            <h3 [infoTooltip]="'equity-curve'">📈 Equity Curve</h3>
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

        <!-- Streak & Discipline Score Card -->
        @if (streak()) {
          <div class="streak-card">
            <div class="streak-left">
              <div class="streak-fire">🔥</div>
              <div class="streak-info">
                <div class="streak-num" [infoTooltip]="'discipline-streak'">{{ streak()!.currentStreak }} day streak</div>
                <div class="streak-sub">Best: {{ streak()!.longestStreak }} days</div>
                @if (!streak()!.tradedToday) {
                  <div class="streak-warn">⚠️ Trade today to keep your streak!</div>
                }
              </div>
            </div>
            <div class="streak-right">
              <div class="discipline-ring">
                <svg viewBox="0 0 60 60">
                  <circle cx="30" cy="30" r="26" fill="none" stroke="var(--border-color)" stroke-width="5"/>
                  <circle cx="30" cy="30" r="26" fill="none"
                    [attr.stroke]="disciplineColor(streak()!.disciplineScore)"
                    stroke-width="5"
                    stroke-linecap="round"
                    stroke-dasharray="163.4"
                    [attr.stroke-dashoffset]="163.4 * (1 - streak()!.disciplineScore / 100)"
                    transform="rotate(-90 30 30)"/>
                </svg>
                <div class="ring-text">
                  <div class="ring-score">{{ streak()!.disciplineScore }}</div>
                  <div class="ring-grade">{{ streak()!.disciplineGrade }}</div>
                </div>
              </div>
              <div class="discipline-label" [infoTooltip]="'trading-score'">Discipline Score</div>
            </div>
          </div>
        }

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
  propFirmStatus = signal<PropFirmStatus | null>(null);
  streak = signal<Streak | null>(null);
  loading = signal(true);
  drawdownWarning = signal(false);
  private equityChartInstance: Chart | null = null;
  private monthlyChartInstance: Chart | null = null;

  constructor(private api: ApiService, private toast: ToastService) {}

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

    this.api.getStreak().subscribe({ next: (s) => this.streak.set(s), error: () => {} });

    // Load prop firm status and fire toast warnings
    this.api.getPropFirmStatus().subscribe({
      next: (status) => {
        this.propFirmStatus.set(status);
        if (!status) return;

        if (status.accountStatus === 'BREACHED_OVERALL') {
          this.toast.show('error', '🔴 ACCOUNT AT RISK: Overall drawdown limit breached!', 'Prop Firm Alert', 0);
        } else if (status.accountStatus === 'BREACHED_DAILY') {
          this.toast.show('error', '🔴 STOP TRADING: Daily drawdown limit breached!', 'Prop Firm Alert', 0);
        } else if (status.accountStatus === 'PASSED') {
          this.toast.show('success', '🎉 Challenge PASSED! Request your payout now!', 'Congratulations!', 8000);
        } else if (status.accountStatus === 'CRITICAL') {
          this.toast.show('warning', `🟠 CRITICAL: ${status.dailyLossUsedPct.toFixed(1)}% daily limit used — trade carefully!`, 'Prop Firm Alert', 6000);
        } else if (status.accountStatus === 'WARNING') {
          this.toast.warning(`⚠️ ${status.dailyLossUsedPct.toFixed(1)}% of daily drawdown used. Remaining: $${status.remainingDailyBudget.toFixed(2)}`, 'Prop Firm Warning');
        }
      },
      error: () => {} // Non-critical — silently ignore
    });
  }

  ngAfterViewInit(): void {}

  disciplineColor(score: number): string {
    if (score >= 80) return '#10b981';
    if (score >= 60) return '#f59e0b';
    return '#ef4444';
  }

  getProfitTargetDollar(): number {
    const db = this.dashboard();
    if (!db) return 0;
    return db.accountBalance * (db.profitTarget / 100);
  }

  getProfitProgress(): number {
    const db = this.dashboard();
    if (!db || db.totalProfitLoss <= 0) return 0;
    const target = this.getProfitTargetDollar();
    return target > 0 ? (db.totalProfitLoss / target) * 100 : 0;
  }

  getDailyLossProgress(): number {
    const db = this.dashboard();
    if (!db || db.todayPL >= 0) return 0;
    const limit = db.dailyLossLimit;
    return limit > 0 ? (Math.abs(db.todayPL) / limit) * 100 : 0;
  }

  getEstimatedPayout(): number {
    const db = this.dashboard();
    if (!db || db.totalProfitLoss <= 0) return 0;
    return db.totalProfitLoss * (db.profitSplit / 100);
  }

  /** Cap progress bar width at 100% */
  min100(val: number): number { return Math.min(val, 100); }

  /** Traffic-light color for progress bars */
  getBarColor(pct: number): string {
    if (pct >= 90) return '#dc2626';
    if (pct >= 70) return '#f97316';
    if (pct >= 50) return '#f59e0b';
    return '#10b981';
  }

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
