import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { CalendarDay } from '../../models/models';

@Component({
  selector: 'app-calendar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page-wrapper">
      <div class="page-header">
        <div>
          <h1 class="page-title-h1">Trading Calendar</h1>
          <p class="page-desc">Visual overview of your daily trading performance</p>
        </div>
        <div class="calendar-nav">
          <button class="btn btn-ghost" (click)="changeMonth(-1)">← Prev</button>
          <h3 class="current-month">{{ months[currentMonth] }} {{ currentYear }}</h3>
          <button class="btn btn-ghost" (click)="changeMonth(1)">Next →</button>
        </div>
      </div>

      <!-- Legend -->
      <div class="calendar-legend">
        <span class="legend-item"><span class="legend-dot profit"></span> Profit Day</span>
        <span class="legend-item"><span class="legend-dot loss"></span> Loss Day</span>
        <span class="legend-item"><span class="legend-dot neutral"></span> No Trades</span>
      </div>

      <!-- Calendar Grid -->
      @if (loading()) {
        <div class="loading-state"><div class="loading-spinner"></div></div>
      } @else {
        <div class="calendar-wrapper">
          <div class="calendar-header">
            @for (day of weekDays; track day) {
              <div class="calendar-weekday">{{ day }}</div>
            }
          </div>
          <div class="calendar-grid">
            @for (cell of calendarCells(); track $index) {
              <div class="calendar-cell"
                [class.empty-cell]="!cell.day"
                [class.profit-day]="cell.data && cell.data.totalPL > 0"
                [class.loss-day]="cell.data && cell.data.totalPL < 0"
                [class.today]="cell.isToday">
                @if (cell.day) {
                  <span class="cell-day">{{ cell.day }}</span>
                  @if (cell.data) {
                    <span class="cell-pl" [class.positive-text]="cell.data.totalPL > 0" [class.negative-text]="cell.data.totalPL < 0">
                      {{ cell.data.totalPL > 0 ? '+' : '' }}{{ cell.data.totalPL | number:'1.0-0' }}
                    </span>
                    <span class="cell-trades">{{ cell.data.tradeCount }} trade{{ cell.data.tradeCount !== 1 ? 's' : '' }}</span>
                  }
                }
              </div>
            }
          </div>
        </div>

        <!-- Monthly Summary -->
        <div class="monthly-summary">
          <div class="summary-card">
            <span class="summary-label">Profit Days</span>
            <span class="summary-value positive-text">{{ profitDays() }}</span>
          </div>
          <div class="summary-card">
            <span class="summary-label">Loss Days</span>
            <span class="summary-value negative-text">{{ lossDays() }}</span>
          </div>
          <div class="summary-card">
            <span class="summary-label">Total Trades</span>
            <span class="summary-value">{{ totalTrades() }}</span>
          </div>
          <div class="summary-card">
            <span class="summary-label">Monthly P&amp;L</span>
            <span class="summary-value" [class.positive-text]="monthlyPL() > 0" [class.negative-text]="monthlyPL() < 0">
              {{ monthlyPL() | number:'1.2-2' }}
            </span>
          </div>
        </div>
      }
    </div>
  `
})
export class CalendarComponent implements OnInit {
  loading = signal(true);
  calendarData = signal<CalendarDay[]>([]);
  calendarCells = signal<any[]>([]);

  weekDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
  months = ['January','February','March','April','May','June','July','August','September','October','November','December'];

  currentYear = new Date().getFullYear();
  currentMonth = new Date().getMonth();

  profitDays = signal(0);
  lossDays = signal(0);
  totalTrades = signal(0);
  monthlyPL = signal(0);

  constructor(private api: ApiService) {}

  ngOnInit(): void { this.load(); }

  changeMonth(delta: number): void {
    this.currentMonth += delta;
    if (this.currentMonth > 11) { this.currentMonth = 0; this.currentYear++; }
    if (this.currentMonth < 0) { this.currentMonth = 11; this.currentYear--; }
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getCalendar(this.currentYear, this.currentMonth + 1).subscribe({
      next: (data) => {
        this.calendarData.set(data);
        this.buildCalendar(data);
        this.profitDays.set(data.filter(d => d.totalPL > 0).length);
        this.lossDays.set(data.filter(d => d.totalPL < 0).length);
        this.totalTrades.set(data.reduce((s, d) => s + d.tradeCount, 0));
        this.monthlyPL.set(data.reduce((s, d) => s + d.totalPL, 0));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  buildCalendar(data: CalendarDay[]): void {
    const firstDay = new Date(this.currentYear, this.currentMonth, 1).getDay();
    const daysInMonth = new Date(this.currentYear, this.currentMonth + 1, 0).getDate();
    const today = new Date();
    const cells: any[] = [];

    for (let i = 0; i < firstDay; i++) cells.push({ day: null });

    for (let d = 1; d <= daysInMonth; d++) {
      const dateStr = `${this.currentYear}-${String(this.currentMonth + 1).padStart(2,'0')}-${String(d).padStart(2,'0')}`;
      const dayData = data.find(item => item.date.startsWith(dateStr));
      const isToday = today.getDate() === d && today.getMonth() === this.currentMonth && today.getFullYear() === this.currentYear;
      cells.push({ day: d, data: dayData, isToday });
    }

    this.calendarCells.set(cells);
  }
}
