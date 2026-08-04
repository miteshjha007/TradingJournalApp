import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { Trade, CreateTrade, TradeFilter, Instrument } from '../../models/models';
import { InfoTooltipDirective } from '../../directives/info-tooltip.directive';

@Component({
  selector: 'app-trades',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, DatePipe, DecimalPipe, InfoTooltipDirective],
  template: `
    <div class="page-wrapper full-height-layout">
      <div class="page-header">
        <div>
          <h1 class="page-title-h1">Trade Journal</h1>
          <p class="page-desc">{{ pagedTrades().totalCount }} total trades</p>
        </div>
        <div class="header-actions">
          <a [href]="exportUrl()" class="btn btn-ghost" download>⬇️ Export CSV</a>
          <button class="btn btn-primary" (click)="openModal()">+ Add Trade</button>
        </div>
      </div>

      <!-- Filters -->
      <div class="filter-bar">
        <div class="filter-group">
          <label>From Date</label>
          <input type="date" [(ngModel)]="filter.fromDate" class="form-input sm" (change)="applyFilter()" />
        </div>
        <div class="filter-group">
          <label>To Date</label>
          <input type="date" [(ngModel)]="filter.toDate" class="form-input sm" (change)="applyFilter()" />
        </div>
        <div class="filter-group">
          <label>Instrument</label>
          <select [(ngModel)]="filter.instrumentId" class="form-input sm" (change)="applyFilter()">
            <option value="">All</option>
            @for (inst of instruments(); track inst.id) {
              <option [value]="inst.id">{{ inst.name }}</option>
            }
          </select>
        </div>
        <div class="filter-group">
          <label>Result</label>
          <select [(ngModel)]="filter.result" class="form-input sm" (change)="applyFilter()">
            <option value="">All</option>
            <option value="Win">Win</option>
            <option value="Loss">Loss</option>
            <option value="BreakEven">BreakEven</option>
          </select>
        </div>
        <div class="filter-group">
          <label>Type</label>
          <select [(ngModel)]="filter.tradeType" class="form-input sm" (change)="applyFilter()">
            <option value="">All</option>
            <option value="Buy">Buy</option>
            <option value="Sell">Sell</option>
          </select>
        </div>
        <div class="filter-group">
          <label>Rows</label>
          <select [(ngModel)]="filter.pageSize" class="form-input sm" (change)="onPageSizeChange()">
            <option [ngValue]="10">10</option>
            <option [ngValue]="20">20</option>
            <option [ngValue]="50">50</option>
            <option [ngValue]="100">100</option>
          </select>
        </div>
        <button class="btn btn-ghost sm" (click)="clearFilter()">Clear</button>
      </div>

      <!-- Trade Table -->
      @if (loading()) {
        <div class="loading-state"><div class="loading-spinner"></div></div>
      } @else {
        <div class="table-card table-scroll-area">
          <table class="data-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Instrument</th>
                <th class="text-center">Type</th>
                <th class="text-center">Lot</th>
                <th class="text-right">Entry</th>
                <th class="text-right">Exit</th>
                <th class="text-right">SL</th>
                <th class="text-right">TP</th>
                <th class="text-right">
                  <div [infoTooltip]="'profit-and-loss'">P&amp;L ⓘ</div>
                  <div [infoTooltip]="'risk-reward-ratio'" class="sub-header">RRR ⓘ</div>
                </th>
                <th class="text-center">Result</th>
                <th>Tags</th>
                <th>Checklist</th>
                <th class="text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (trade of pagedTrades().trades; track trade.id) {
                <tr [class.win-row]="trade.result === 'Win'" [class.loss-row]="trade.result === 'Loss'">
                  <td>{{ trade.tradeDate | date:'dd MMM yy' }}</td>
                  <td><span class="badge">{{ trade.instrumentName }}</span></td>
                  <td class="text-center"><span class="type-badge" [class.buy]="trade.tradeType === 'Buy'" [class.sell]="trade.tradeType === 'Sell'">{{ trade.tradeType }}</span></td>
                  <td class="text-center">{{ trade.lotSize }}</td>
                  <td class="text-right">{{ trade.entryPrice | number:'1.2-5' }}</td>
                  <td class="text-right">{{ trade.exitPrice | number:'1.2-5' }}</td>
                  <td class="text-right">{{ trade.stopLoss | number:'1.2-5' }}</td>
                  <td class="text-right">{{ trade.takeProfit | number:'1.2-5' }}</td>
                  <td class="text-right">
                    <div class="pl-cell" [class.positive-text]="(trade.profitLoss || 0) > 0" [class.negative-text]="(trade.profitLoss || 0) < 0">
                      {{ trade.profitLoss || 0 | number:'1.2-2' }}
                    </div>
                    <div class="rrr-sub">
                      {{ trade.riskRewardRatio || 0 | number:'1.2-2' }}
                    </div>
                  </td>
                  <td class="text-center"><span class="result-badge" [class]="'result-' + (trade.result || '').toLowerCase()">{{ trade.result || 'Pending' }}</span></td>
                  <td class="tags-cell">
                    @if (trade.tags) {
                      @for (tag of trade.tags.split(','); track tag) {
                        <span class="tag">{{ tag.trim() }}</span>
                      }
                    }
                    @if (trade.ruleViolations && trade.ruleViolations.length > 0) {
                      <div style="margin-top:0.4rem; display:flex; flex-direction:column; gap:0.2rem;">
                        @for (violation of trade.ruleViolations; track violation) {
                          <span class="tag" style="background:rgba(239,68,68,0.1); color:#ef4444; border:1px solid rgba(239,68,68,0.3); font-size:0.7rem;">
                            ⚠️ {{ violation }}
                          </span>
                        }
                      </div>
                    }
                  </td>
                  <td>
                    @if (trade.checklistCompliancePercent != null) {
                      <span class="compliance-badge" [class.compliance-high]="trade.checklistCompliancePercent >= 80" [class.compliance-mid]="trade.checklistCompliancePercent >= 50 && trade.checklistCompliancePercent < 80" [class.compliance-low]="trade.checklistCompliancePercent < 50">
                        {{ trade.checklistCompliancePercent | number:'1.0-0' }}%
                      </span>
                    }
                    @if (trade.chartImageUrl) {
                      <span title="Chart uploaded" style="margin-left:4px">📷</span>
                    }
                  </td>
                  <td class="actions-cell text-right">
                    <button class="btn-icon" (click)="openModal(trade)">✏️</button>
                    @if (trade.id) {
                      <button class="btn-icon danger" (click)="deleteTrade(trade.id)">🗑️</button>
                    }
                  </td>
                </tr>
              }
              @empty {
                <tr>
                  <td colspan="14" class="empty-cell">No trades found. Click "+ Add Trade" to log your first trade.</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        @if (pagedTrades().totalCount > 0) {
          <div class="pagination-container">
            <div class="pagination-info">
              Showing <strong>{{ startRow() }}</strong> – <strong>{{ endRow() }}</strong> of <strong>{{ pagedTrades().totalCount }}</strong> trades
            </div>
            <div class="pagination-controls">
              <div class="page-size-selector">
                <label>Rows per page:</label>
                <select [(ngModel)]="filter.pageSize" class="form-input sm" (change)="onPageSizeChange()">
                  <option [ngValue]="10">10</option>
                  <option [ngValue]="20">20</option>
                  <option [ngValue]="50">50</option>
                  <option [ngValue]="100">100</option>
                </select>
              </div>
              <div class="page-buttons">
                <button class="btn btn-ghost sm" [disabled]="filter.page <= 1" (click)="goToPage(filter.page - 1)">← Prev</button>
                <span class="page-info">Page {{ filter.page }} of {{ pagedTrades().totalPages || 1 }}</span>
                <button class="btn btn-ghost sm" [disabled]="filter.page >= pagedTrades().totalPages" (click)="goToPage(filter.page + 1)">Next →</button>
              </div>
            </div>
          </div>
        }
      }

      <!-- Add/Edit Modal -->
      @if (showModal()) {
        <div class="modal-overlay" (click)="closeModal()">
          <div class="modal modal-lg" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>{{ editingId() ? 'Edit' : 'Add' }} Trade</h3>
              <button class="modal-close" (click)="closeModal()">✕</button>
            </div>
            <form [formGroup]="form" (ngSubmit)="onSubmit()" class="modal-body">
              <div class="form-row">
                <div class="form-group">
                  <label>Instrument *</label>
                  <select formControlName="instrumentId" class="form-input">
                    @for (inst of instruments(); track inst.id) {
                      <option [value]="inst.id">{{ inst.name }}</option>
                    }
                  </select>
                </div>
                <div class="form-group">
                  <label>Trade Date *</label>
                  <input type="datetime-local" formControlName="tradeDate" class="form-input" />
                </div>
              </div>
              <div class="form-row">
                <div class="form-group">
                  <label>Trade Type *</label>
                  <select formControlName="tradeType" class="form-input">
                    <option [ngValue]="1">Buy</option>
                    <option [ngValue]="2">Sell</option>
                  </select>
                </div>
                <div class="form-group">
                  <label>Lot Size *</label>
                  <input type="number" step="0.01" formControlName="lotSize" class="form-input" />
                </div>
                <div class="form-group">
                  <label>Risk %</label>
                  <input type="number" step="0.1" formControlName="riskPercentage" class="form-input" />
                </div>
              </div>
              <div class="form-row">
                <div class="form-group">
                  <label>Entry Price *</label>
                  <input type="number" step="0.00001" formControlName="entryPrice" class="form-input" />
                </div>
                <div class="form-group">
                  <label>Exit Price *</label>
                  <input type="number" step="0.00001" formControlName="exitPrice" class="form-input" />
                </div>
              </div>
              <div class="form-row">
                <div class="form-group">
                  <label>Stop Loss</label>
                  <input type="number" step="0.00001" formControlName="stopLoss" class="form-input" />
                </div>
                <div class="form-group">
                  <label>Take Profit</label>
                  <input type="number" step="0.00001" formControlName="takeProfit" class="form-input" />
                </div>
                <div class="form-group">
                  <label>Duration (min)</label>
                  <input type="number" formControlName="tradeDurationMinutes" class="form-input" />
                </div>
              </div>
              <div class="form-group">
                <label>Tags</label>
                <input type="text" formControlName="tags" placeholder="Breakout, Scalp, Swing (comma-separated)" class="form-input" />
              </div>
              <div class="form-group">
                <label>Notes</label>
                <textarea formControlName="notes" rows="3" placeholder="Trade notes..." class="form-input"></textarea>
              </div>
              <div class="modal-footer">
                <button type="button" class="btn btn-ghost" (click)="closeModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">
                  @if (saving()) { <span class="spinner"></span> }
                  {{ editingId() ? 'Update' : 'Save Trade' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `
})
export class TradesComponent implements OnInit {
  pagedTrades = signal({ trades: [] as Trade[], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 });
  instruments = signal<Instrument[]>([]);
  loading = signal(true);
  showModal = signal(false);
  editingId = signal<string | null>(null);
  saving = signal(false);

  filter: TradeFilter = { page: 1, pageSize: 10 };
  form;

  constructor(private api: ApiService, private fb: FormBuilder, private toast: ToastService) {
    this.form = this.fb.group({
      instrumentId: ['', Validators.required],
      tradeDate: ['', Validators.required],
      tradeType: [1, Validators.required],
      lotSize: [0.1, [Validators.required, Validators.min(0.01)]],
      entryPrice: [0, Validators.required],
      exitPrice: [0, Validators.required],
      stopLoss: [0],
      takeProfit: [0],
      riskPercentage: [1],
      tradeDurationMinutes: [0],
      tags: [''],
      notes: ['']
    });
  }

  ngOnInit(): void {
    this.api.getInstruments().subscribe(data => this.instruments.set(data));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getTrades(this.filter).subscribe({
      next: (data) => { this.pagedTrades.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  applyFilter(): void { this.filter.page = 1; this.load(); }
  clearFilter(): void { this.filter = { page: 1, pageSize: 10 }; this.load(); }
  goToPage(page: number): void { this.filter.page = page; this.load(); }

  onPageSizeChange(): void {
    this.filter.page = 1;
    this.load();
  }

  startRow(): number {
    if (this.pagedTrades().totalCount === 0) return 0;
    return (this.filter.page - 1) * (this.filter.pageSize || 10) + 1;
  }

  endRow(): number {
    return Math.min(this.filter.page * (this.filter.pageSize || 10), this.pagedTrades().totalCount);
  }

  exportUrl(): string { return this.api.exportTrades(this.filter); }

  openModal(trade?: Trade): void {
    if (trade && trade.id) {
      this.editingId.set(trade.id);
      this.form.patchValue({
        instrumentId: trade.instrumentId,
        tradeDate: new Date(trade.tradeDate).toISOString().slice(0, 16),
        tradeType: trade.tradeType === 'Buy' ? 1 : 2,
        lotSize: trade.lotSize, entryPrice: trade.entryPrice,
        exitPrice: trade.exitPrice, stopLoss: trade.stopLoss,
        takeProfit: trade.takeProfit, riskPercentage: trade.riskPercentage,
        tradeDurationMinutes: trade.tradeDurationMinutes,
        tags: trade.tags || '', notes: trade.notes || ''
      });
    } else {
      this.editingId.set(null);
      this.form.reset({ tradeType: 1, lotSize: 0.1, riskPercentage: 1, tradeDate: new Date().toISOString().slice(0, 16) });
    }
    this.showModal.set(true);
  }

  closeModal(): void { this.showModal.set(false); }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const data = this.form.value as CreateTrade;
    const obs = this.editingId()
      ? this.api.updateTrade(this.editingId()!, data)
      : this.api.createTrade(data);

    obs.subscribe({
      next: () => { 
        this.closeModal(); 
        this.load(); 
        this.saving.set(false); 
        this.toast.success(`Trade ${this.editingId() ? 'updated' : 'created'} successfully!`, 'Success');
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err.error?.error || 'Failed to save trade.', 'Error');
      }
    });
  }

  deleteTrade(id: string): void {
    if (!confirm('Delete this trade?')) return;
    this.api.deleteTrade(id).subscribe({
      next: () => {
        this.load();
        this.toast.success('Trade deleted successfully.', 'Deleted');
      },
      error: (err) => this.toast.error(err.error?.error || 'Failed to delete trade.', 'Error')
    });
  }
}
