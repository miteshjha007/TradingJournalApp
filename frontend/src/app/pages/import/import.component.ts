import { Component, OnInit, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import {
  CsvImportPreview, CsvImportRequest, CsvImportConfirm, ImportResult,
  Mt5WebhookConfig, UpdateMt5Config, ImportLog, Instrument, TradingAccount
} from '../../models/models';

type ImportStep = 'upload' | 'preview' | 'confirm' | 'done';

@Component({
  selector: 'app-import',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="import-page">
      <div class="page-header">
        <h1>Import Trades</h1>
        <p>Import trades from MT5 CSV exports or set up automatic webhook import</p>
      </div>

      <div class="tabs">
        <button class="tab" [class.active]="activeTab() === 'csv'" (click)="activeTab.set('csv')">CSV Import</button>
        <button class="tab" [class.active]="activeTab() === 'mt5'" (click)="activeTab.set('mt5')">MT5 Webhook</button>
        <button class="tab" [class.active]="activeTab() === 'history'" (click)="activeTab.set('history')">Import History</button>
      </div>

      <!-- CSV Import Tab -->
      <div class="tab-content" *ngIf="activeTab() === 'csv'">
        <ng-container *ngIf="csvStep() === 'upload'">
          <div class="upload-section">
            <div class="info-card">
              <h3>How to export from MT5:</h3>
              <ol>
                <li>Open MT5 - Account History tab</li>
                <li>Right-click - Save as Report (Detailed)</li>
                <li>Select HTML or CSV format</li>
                <li>Upload that file here</li>
              </ol>
            </div>

            <div class="drop-zone" [class.dragover]="isDragging()"
                 (dragover)="onDragOver($event)"
                 (dragleave)="onDragLeave($event)"
                 (drop)="onDrop($event)"
                 (click)="fileInput.click()">
              <input #fileInput type="file" accept=".csv,.htm,.html" (change)="onFileSelected($event)" hidden>
              <div class="drop-icon">Folder</div>
              <p>Drag and drop your MT5 history file here</p>
              <p class="sub">or click to browse (.csv, .htm, .html)</p>
            </div>

            <div class="file-info" *ngIf="selectedFile()">
              <span>{{ selectedFile()?.name }}</span>
              <span class="file-size">({{ (selectedFile()?.size || 0) / 1024 | number:'1.1-1' }} KB)</span>
            </div>

            <div class="options-row">
              <div class="option">
                <label>Assign all trades to account:</label>
                <select [(ngModel)]="csvTradingAccountId">
                  <option value="">None - assign later</option>
                  <option *ngFor="let account of accounts()" [value]="account.id">{{ account.name }}</option>
                </select>
              </div>
            </div>

            <button class="btn-primary" [disabled]="!selectedFile() || csvLoading()" (click)="previewImport()">
              {{ csvLoading() ? 'Parsing...' : 'Preview Import' }}
            </button>
          </div>
        </ng-container>

        <ng-container *ngIf="csvStep() === 'preview' || csvStep() === 'confirm'">
          <div class="preview-section">
            <div class="format-badge">Detected: {{ csvPreview()?.csvFormat || 'Unknown' }} Format</div>

            <div class="trade-table-section" *ngIf="csvPreview()?.validTrades?.length">
              <h3>Ready to Import ({{ csvPreview()?.validTrades?.length }} trades)</h3>
              <div class="table-wrap">
                <table class="trade-table">
                  <thead>
                    <tr>
                      <th>Symbol</th><th>Mapped To</th><th>Type</th><th>Lot</th>
                      <th>Entry</th><th>Exit</th><th>PL</th><th>Date</th><th>Duration</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let trade of csvPreview()?.validTrades; let i = index" [class.no-instrument]="!trade.instrumentId">
                      <td>{{ trade.symbol }}</td>
                      <td>
                        <span *ngIf="trade.instrumentId">{{ trade.mappedInstrumentName }}</span>
                        <span *ngIf="!trade.instrumentId" class="warning">No match</span>
                      </td>
                      <td>{{ trade.tradeType }}</td>
                      <td>{{ trade.lotSize }}</td>
                      <td>{{ trade.entryPrice }}</td>
                      <td>{{ trade.exitPrice }}</td>
                      <td [class.positive]="trade.profitLoss > 0" [class.negative]="trade.profitLoss < 0">
                        {{ trade.profitLoss | number:'1.2' }}
                      </td>
                      <td>{{ trade.openTime | date:'short' }}</td>
                      <td>{{ trade.durationMinutes }}m</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <div class="trade-table-section duplicates" *ngIf="csvPreview()?.duplicateTrades?.length">
              <h3>Duplicates Found ({{ csvPreview()?.duplicateTrades?.length }} trades)</h3>
              <label class="checkbox">
                <input type="checkbox" [(ngModel)]="skipDuplicates"> Skip duplicates (recommended)
              </label>
            </div>

            <div class="trade-table-section errors" *ngIf="csvPreview()?.errors?.length">
              <h3>Parse Errors ({{ csvPreview()?.errors?.length }} rows)</h3>
            </div>

            <div class="summary-bar">
              <span>{{ csvPreview()?.validTrades?.length || 0 }} ready</span>
              <span>{{ csvPreview()?.duplicateTrades?.length || 0 }} duplicates</span>
              <span>{{ csvPreview()?.errors?.length || 0 }} errors</span>
            </div>

            <div class="warning-banner" *ngIf="hasUnmatchedInstruments()">
              Some trades have no matching instrument. Please add them in Instruments first.
            </div>

            <div class="actions">
              <button class="btn-secondary" (click)="csvStep.set('upload')">Back</button>
              <button class="btn-primary" [disabled]="hasUnmatchedInstruments() || csvLoading()" (click)="confirmImport()">
                {{ csvLoading() ? 'Importing...' : 'Confirm Import' }}
              </button>
            </div>
          </div>
        </ng-container>

        <ng-container *ngIf="csvStep() === 'done'">
          <div class="done-section">
            <div class="success-card">
              <h2>Import Complete</h2>
              <div class="result-stats">
                <div class="stat">
                  <span class="label">Inserted</span>
                  <span class="value success">{{ importResult()?.inserted }}</span>
                </div>
                <div class="stat">
                  <span class="label">Skipped</span>
                  <span class="value warning">{{ importResult()?.skipped }}</span>
                </div>
                <div class="stat">
                  <span class="label">Failed</span>
                  <span class="value error">{{ importResult()?.failed }}</span>
                </div>
              </div>
              <p class="summary">{{ importResult()?.summary }}</p>
              <div class="actions">
                <button class="btn-secondary" (click)="goToTrades()">View in Trade Journal</button>
                <button class="btn-primary" (click)="resetCsv()">Import Another File</button>
              </div>
            </div>
          </div>
        </ng-container>
      </div>

      <!-- MT5 Webhook Tab -->
      <div class="tab-content" *ngIf="activeTab() === 'mt5'">
        <div *ngIf="mt5Loading()" class="loading">Loading configuration...</div>
        <div *ngIf="!mt5Loading()" class="webhook-section">
          <div class="card webhook-url-card">
            <h3>Your Webhook URL</h3>
            <div class="url-box">
              <input readonly [value]="mt5Config()?.webhookUrl || ''">
              <button class="btn-icon" (click)="copyToClipboard(mt5Config()?.webhookUrl || '')">Copy</button>
            </div>

            <h3>Your Secret Token</h3>
            <div class="url-box token">
              <input [type]="showToken() ? 'text' : 'password'" readonly [value]="mt5Config()?.webhookToken || ''">
              <button class="btn-icon" (click)="showToken.set(!showToken())">{{ showToken() ? 'Hide' : 'Show' }}</button>
              <button class="btn-icon" (click)="copyToClipboard(mt5Config()?.webhookToken || '')">Copy</button>
            </div>

            <div class="status-row">
              <span class="status-badge" [class.active]="mt5Config()?.isActive" [class.inactive]="!mt5Config()?.isActive">
                {{ mt5Config()?.isActive ? 'Active' : 'Inactive' }}
              </span>
              <button class="btn-small" (click)="toggleActive()">{{ mt5Config()?.isActive ? 'Disable' : 'Enable' }}</button>
            </div>

            <div class="stats-row">
              <span>Last used: {{ mt5Config()?.lastUsedAt | date:'short' || 'Never' }}</span>
              <span>Total imported: {{ mt5Config()?.totalTradesImported || 0 }}</span>
            </div>

            <button class="btn-warning" (click)="showRegenerateConfirm.set(true)">Regenerate Token</button>

            <div class="confirm-dialog" *ngIf="showRegenerateConfirm()">
              <p>This will break your existing MT5 EA connection. Are you sure?</p>
              <div class="confirm-actions">
                <button class="btn-secondary" (click)="showRegenerateConfirm.set(false)">Cancel</button>
                <button class="btn-danger" (click)="regenerateToken()">Yes, Regenerate</button>
              </div>
            </div>
          </div>

          <div class="card instructions-card">
            <h3>MT5 Setup Instructions</h3>
            <div class="step"><strong>1. Enable WebRequests</strong><p>Tools - Options - Expert Advisors. Check Allow WebRequest and add: {{ mt5Config()?.webhookUrl }}</p></div>
            <div class="step"><strong>2. Download the EA</strong><button class="btn-download" (click)="downloadEA()">Download TradingJournalEA.mq5</button></div>
            <div class="step"><strong>3. Install</strong><p>Place .mq5 in MT5 experts folder</p></div>
            <div class="step"><strong>4. Attach</strong><p>Drag EA onto chart and enter your token</p></div>
          </div>

          <div class="card mapping-card">
            <h3>Instrument Symbol Mapping</h3>
            <div class="mapping-row" *ngFor="let mapping of instrumentMappings(); let i = index">
              <input [(ngModel)]="mapping.mt5Symbol" placeholder="e.g. XAUUSD">
              <select [(ngModel)]="mapping.instrumentId">
                <option value="">Select instrument</option>
                <option *ngFor="let inst of instruments()" [value]="inst.id">{{ inst.name }}</option>
              </select>
              <button class="btn-icon delete" (click)="removeMapping(i)">X</button>
            </div>
            <button class="btn-add" (click)="addMapping()">+ Add Mapping</button>
            <button class="btn-primary" (click)="saveMappings()">Save Mappings</button>
          </div>

          <div class="card account-card">
            <h3>Default Trading Account</h3>
            <select [(ngModel)]="defaultAccountId" (change)="saveDefaultAccount()">
              <option value="">None - assign later</option>
              <option *ngFor="let account of accounts()" [value]="account.id">{{ account.name }}</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Import History Tab -->
      <div class="tab-content" *ngIf="activeTab() === 'history'">
        <div *ngIf="historyLoading()" class="loading">Loading history...</div>
        <div *ngIf="!historyLoading() && history().length === 0" class="empty-state">
          <p>No imports yet. Use CSV Import or MT5 Webhook above.</p>
        </div>
        <div *ngIf="history().length > 0" class="history-table-wrap">
          <table class="history-table">
            <thead>
              <tr><th>Date</th><th>Source</th><th>Received</th><th>Inserted</th><th>Skipped</th><th>Failed</th><th>Status</th></tr>
            </thead>
            <tbody>
              <tr *ngFor="let log of history()">
                <td>{{ log.createdAt | date:'short' }}</td>
                <td><span class="source-badge">{{ log.source }}</span></td>
                <td>{{ log.totalReceived }}</td>
                <td>{{ log.totalInserted }}</td>
                <td>{{ log.totalSkipped }}</td>
                <td>{{ log.totalFailed }}</td>
                <td><span class="status-badge" [class.success]="log.status === 'Success'" [class.failed]="log.status === 'Failed'">{{ log.status }}</span></td>
              </tr>
            </tbody>
          </table>
        </div>
        <button class="btn-secondary" (click)="loadMoreHistory()" *ngIf="history().length > 0">Load More</button>
      </div>
    </div>
  `,
  styles: [`
    .import-page { padding: 1.5rem; max-width: 1200px; margin: 0 auto; }
    .page-header { margin-bottom: 1.5rem; }
    .page-header h1 { font-size: 1.75rem; color: var(--text-main); margin-bottom: 0.25rem; }
    .page-header p { color: var(--text-muted); }

    .tabs { display: flex; gap: 0.5rem; margin-bottom: 1.5rem; border-bottom: 1px solid var(--border-color); }
    .tab { padding: 0.75rem 1.25rem; background: none; border: none; color: var(--text-muted); cursor: pointer; font-size: 0.95rem; border-bottom: 2px solid transparent; }
    .tab:hover { color: var(--text-main); }
    .tab.active { color: var(--primary); border-bottom-color: var(--primary); }

    .tab-content { animation: fadeIn 0.2s ease; }
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

    .info-card { background: var(--bg-hover); border-radius: var(--border-radius); padding: 1rem 1.25rem; margin-bottom: 1.5rem; }
    .info-card h3 { font-size: 1rem; margin-bottom: 0.5rem; }
    .info-card ol { margin: 0; padding-left: 1.25rem; color: var(--text-muted); }
    .info-card li { margin-bottom: 0.25rem; }

    .drop-zone { border: 2px dashed var(--border-color); border-radius: var(--border-radius); padding: 2.5rem; text-align: center; cursor: pointer; transition: var(--transition); background: var(--bg-card); }
    .drop-zone:hover, .drop-zone.dragover { border-color: var(--primary); background: var(--primary-light); }
    .drop-icon { font-size: 2.5rem; margin-bottom: 0.5rem; }
    .drop-zone p { margin: 0; color: var(--text-main); }
    .drop-zone .sub { font-size: 0.85rem; color: var(--text-muted); margin-top: 0.25rem; }

    .file-info { display: flex; align-items: center; gap: 0.5rem; margin-top: 1rem; padding: 0.75rem; background: var(--bg-hover); border-radius: var(--border-radius); }
    .file-size { color: var(--text-muted); font-size: 0.85rem; }

    .options-row { margin-top: 1rem; }
    .option { display: flex; align-items: center; gap: 1rem; }
    .option label { color: var(--text-muted); font-size: 0.9rem; }
    .option select { padding: 0.5rem; border-radius: var(--border-radius); border: 1px solid var(--border-color); background: var(--bg-card); color: var(--text-main); }

    .btn-primary { margin-top: 1.5rem; padding: 0.75rem 1.5rem; background: var(--primary); color: white; border: none; border-radius: var(--border-radius); cursor: pointer; font-size: 0.95rem; }
    .btn-primary:hover:not(:disabled) { background: var(--primary-dark); }
    .btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn-secondary { padding: 0.75rem 1.5rem; background: var(--bg-hover); color: var(--text-main); border: 1px solid var(--border-color); border-radius: var(--border-radius); cursor: pointer; }
    .btn-warning { padding: 0.5rem 1rem; background: var(--warning); color: #000; border: none; border-radius: var(--border-radius); cursor: pointer; }
    .btn-danger { padding: 0.5rem 1rem; background: var(--danger); color: white; border: none; border-radius: var(--border-radius); cursor: pointer; }
    .btn-small { padding: 0.35rem 0.75rem; background: var(--bg-hover); border: 1px solid var(--border-color); border-radius: var(--border-radius); cursor: pointer; font-size: 0.8rem; }
    .btn-icon { padding: 0.35rem; background: var(--bg-hover); border: 1px solid var(--border-color); border-radius: var(--border-radius); cursor: pointer; }
    .btn-download { padding: 0.5rem 1rem; background: var(--primary); color: white; border: none; border-radius: var(--border-radius); cursor: pointer; }
    .btn-add { padding: 0.5rem; background: none; border: 1px dashed var(--border-color); border-radius: var(--border-radius); cursor: pointer; color: var(--text-muted); width: 100%; margin-top: 0.5rem; }
    .btn-add:hover { border-color: var(--primary); color: var(--primary); }

    .format-badge { display: inline-block; padding: 0.35rem 0.75rem; background: var(--primary-light); color: var(--primary); border-radius: 9999px; font-size: 0.85rem; margin-bottom: 1rem; }

    .trade-table-section { margin-bottom: 1.5rem; }
    .trade-table-section h3 { font-size: 1rem; margin-bottom: 0.75rem; }
    .trade-table-section.duplicates { background: rgba(245, 158, 11, 0.1); padding: 1rem; border-radius: var(--border-radius); }
    .trade-table-section.errors { background: rgba(239, 68, 68, 0.1); padding: 1rem; border-radius: var(--border-radius); }

    .table-wrap { overflow-x: auto; }
    .trade-table, .history-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
    .trade-table th, .trade-table td, .history-table th, .history-table td { padding: 0.6rem 0.75rem; text-align: left; border-bottom: 1px solid var(--border-color); }
    .trade-table th, .history-table th { background: var(--bg-hover); font-weight: 600; color: var(--text-muted); }
    .trade-table .positive { color: var(--success); }
    .trade-table .negative { color: var(--danger); }
    .trade-table .no-instrument { background: rgba(245, 158, 11, 0.1); }
    .trade-table .warning { color: var(--warning); }

    .checkbox { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.75rem; cursor: pointer; }

    .summary-bar { display: flex; gap: 1.5rem; padding: 1rem; background: var(--bg-hover); border-radius: var(--border-radius); margin-bottom: 1rem; }
    .warning-banner { padding: 0.75rem 1rem; background: rgba(245, 158, 11, 0.15); border: 1px solid var(--warning); border-radius: var(--border-radius); color: var(--warning); margin-bottom: 1rem; }

    .actions { display: flex; gap: 1rem; justify-content: flex-end; }

    .success-card { text-align: center; padding: 2rem; background: var(--bg-card); border-radius: var(--border-radius); border: 1px solid var(--success); }
    .result-stats { display: flex; justify-content: center; gap: 2rem; margin: 1.5rem 0; }
    .stat { text-align: center; }
    .stat .label { display: block; color: var(--text-muted); font-size: 0.85rem; margin-bottom: 0.25rem; }
    .stat .value { font-size: 1.5rem; font-weight: 700; }
    .stat .value.success { color: var(--success); }
    .stat .value.warning { color: var(--warning); }
    .stat .value.error { color: var(--danger); }
    .summary { color: var(--text-muted); margin-bottom: 1.5rem; }

    .card { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--border-radius); padding: 1.25rem; margin-bottom: 1.5rem; }
    .card h3 { font-size: 1.1rem; margin-bottom: 1rem; }

    .url-box { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 1rem; }
    .url-box input { flex: 1; padding: 0.6rem; background: var(--bg-hover); border: 1px solid var(--border-color); border-radius: var(--border-radius); font-family: monospace; font-size: 0.85rem; color: var(--text-main); }

    .status-row { display: flex; align-items: center; gap: 1rem; margin: 1rem 0; }
    .status-badge { padding: 0.35rem 0.75rem; border-radius: 9999px; font-size: 0.8rem; font-weight: 600; }
    .status-badge.active { background: rgba(34, 197, 94, 0.15); color: var(--success); }
    .status-badge.inactive { background: rgba(239, 68, 68, 0.15); color: var(--danger); }
    .status-badge.success { background: rgba(34, 197, 94, 0.15); color: var(--success); }
    .status-badge.failed { background: rgba(239, 68, 68, 0.15); color: var(--danger); }

    .stats-row { display: flex; gap: 2rem; color: var(--text-muted); font-size: 0.85rem; margin-bottom: 1rem; }
    .confirm-dialog { margin-top: 1rem; padding: 1rem; background: rgba(239, 68, 68, 0.1); border-radius: var(--border-radius); }
    .confirm-dialog p { margin-bottom: 0.75rem; color: var(--danger); }
    .confirm-actions { display: flex; gap: 0.5rem; }

    .step { margin-bottom: 1.25rem; }
    .step strong { display: block; margin-bottom: 0.25rem; }
    .step p { color: var(--text-muted); font-size: 0.85rem; margin: 0; }

    .mapping-row { display: flex; gap: 0.5rem; margin-bottom: 0.5rem; }
    .mapping-row input, .mapping-row select { padding: 0.5rem; background: var(--bg-hover); border: 1px solid var(--border-color); border-radius: var(--border-radius); color: var(--text-main); flex: 1; }
    .btn-icon.delete { color: var(--danger); }

    .source-badge { padding: 0.25rem 0.5rem; border-radius: 4px; font-size: 0.8rem; background: rgba(59, 130, 246, 0.15); }

    .loading, .empty-state { text-align: center; padding: 3rem; color: var(--text-muted); }
    .history-table-wrap { overflow-x: auto; margin-bottom: 1rem; }
  `]
})
export class ImportComponent implements OnInit {
  activeTab = signal<'csv' | 'mt5' | 'history'>('csv');
  csvStep = signal<ImportStep>('upload');
  selectedFile = signal<File | null>(null);
  csvContent = signal<string>('');
  csvPreview = signal<CsvImportPreview | null>(null);
  csvLoading = signal(false);
  importResult = signal<ImportResult | null>(null);
  csvTradingAccountId = signal<string>('');
  skipDuplicates = signal(true);
  isDragging = signal(false);

  mt5Config = signal<Mt5WebhookConfig | null>(null);
  mt5Loading = signal(false);
  showToken = signal(false);
  showRegenerateConfirm = signal(false);
  defaultAccountId = signal<string>('');
  instruments = signal<Instrument[]>([]);
  accounts = signal<TradingAccount[]>([]);

  history = signal<ImportLog[]>([]);
  historyPage = signal(1);
  historyLoading = signal(false);
  instrumentMappings = signal<{ mt5Symbol: string; instrumentId: string }[]>([]);

  constructor(private api: ApiService, private toast: ToastService) {
    effect(() => {
      if (this.activeTab() === 'mt5' && !this.mt5Config()) {
        this.loadMt5Config();
      }
      if (this.activeTab() === 'history' && this.history().length === 0) {
        this.loadHistory();
      }
    });
  }

  ngOnInit() {
    this.loadInstruments();
    this.loadAccounts();
  }

  loadInstruments() { this.api.getInstruments().subscribe({ next: (data) => this.instruments.set(data) }); }
  loadAccounts() { this.api.getAccounts().subscribe({ next: (data) => this.accounts.set(data) }); }

  onDragOver(e: DragEvent) { e.preventDefault(); this.isDragging.set(true); }
  onDragLeave(e: DragEvent) { e.preventDefault(); this.isDragging.set(false); }
  onDrop(e: DragEvent) { e.preventDefault(); this.isDragging.set(false); const files = e.dataTransfer?.files; if (files?.length) this.handleFile(files[0]); }
  onFileSelected(e: Event) { const input = e.target as HTMLInputElement; if (input.files?.length) this.handleFile(input.files[0]); }

  handleFile(file: File) {
    this.selectedFile.set(file);
    const reader = new FileReader();
    reader.onload = (event) => this.csvContent.set(event.target?.result as string);
    reader.readAsText(file);
  }

  previewImport() {
    if (!this.csvContent()) return;
    this.csvLoading.set(true);
    this.api.previewCsvImport({ csvContent: this.csvContent(), tradingAccountId: this.csvTradingAccountId() || undefined }).subscribe({
      next: (preview) => { this.csvPreview.set(preview); this.csvStep.set('preview'); this.csvLoading.set(false); this.toast.success(preview.totalRows + ' rows parsed', 'CSV Preview'); },
      error: () => { this.csvLoading.set(false); this.toast.error('Failed to parse CSV', 'Import Error'); }
    });
  }

  hasUnmatchedInstruments(): boolean { return this.csvPreview()?.validTrades?.some(t => !t.instrumentId) || false; }

  confirmImport() {
    this.csvLoading.set(true);
    this.api.confirmCsvImport({ csvContent: this.csvContent(), tradingAccountId: this.csvTradingAccountId() || undefined, skipDuplicates: this.skipDuplicates() }).subscribe({
      next: (result) => { this.importResult.set(result); this.csvStep.set('done'); this.csvLoading.set(false); this.toast.success(result.inserted + ' trades imported!', 'Import Complete'); },
      error: () => { this.csvLoading.set(false); this.toast.error('Import failed', 'Import Error'); }
    });
  }

  resetCsv() { this.csvStep.set('upload'); this.selectedFile.set(null); this.csvContent.set(''); this.csvPreview.set(null); this.importResult.set(null); }

  goToTrades() { window.location.href = '/trades'; }

  loadMt5Config() {
    this.mt5Loading.set(true);
    this.api.getMt5Config().subscribe({
      next: (config) => { this.mt5Config.set(config); this.defaultAccountId.set(config.defaultTradingAccountId || ''); this.parseMappings(config.instrumentMappings); this.mt5Loading.set(false); },
      error: () => this.mt5Loading.set(false)
    });
  }

  parseMappings(mappings: { [key: string]: string }) {
    const arr: { mt5Symbol: string; instrumentId: string }[] = [];
    for (const [mt5, instName] of Object.entries(mappings)) { const inst = this.instruments().find(i => i.name === instName); arr.push({ mt5Symbol: mt5, instrumentId: inst?.id || '' }); }
    if (arr.length === 0) arr.push({ mt5Symbol: '', instrumentId: '' });
    this.instrumentMappings.set(arr);
  }

  addMapping() { this.instrumentMappings.update(m => [...m, { mt5Symbol: '', instrumentId: '' }]); }
  removeMapping(index: number) { this.instrumentMappings.update(m => m.filter((_, i) => i !== index)); }

  saveMappings() {
    const mappings: { [key: string]: string } = {};
    for (const m of this.instrumentMappings()) { if (m.mt5Symbol && m.instrumentId) { const inst = this.instruments().find(i => i.id === m.instrumentId); if (inst) mappings[m.mt5Symbol] = inst.name; } }
    this.api.updateMt5Config({ isActive: this.mt5Config()?.isActive || true, description: this.mt5Config()?.description, defaultTradingAccountId: this.defaultAccountId() || undefined, instrumentMappings: mappings }).subscribe({
      next: (config) => { this.mt5Config.set(config); this.toast.success('MT5 config saved!', 'Webhook Config'); },
      error: () => this.toast.error('Failed to save config', 'Error')
    });
  }

  toggleActive() {
    const config = this.mt5Config(); if (!config) return;
    this.api.updateMt5Config({ isActive: !config.isActive, description: config.description, defaultTradingAccountId: config.defaultTradingAccountId || undefined, instrumentMappings: config.instrumentMappings }).subscribe({
      next: (updated) => { this.mt5Config.set(updated); this.toast.success(updated.isActive ? 'Webhook enabled' : 'Webhook disabled', 'Config'); }
    });
  }

  regenerateToken() {
    this.api.regenerateMt5Token().subscribe({
      next: (config) => { this.mt5Config.set(config); this.showRegenerateConfirm.set(false); this.toast.success('Token regenerated - update your MT5 EA!', 'Config'); },
      error: () => this.toast.error('Failed to regenerate token', 'Error')
    });
  }

  saveDefaultAccount() { this.saveMappings(); }
  downloadEA() { window.open('/api/import/mt5-ea-download', '_blank'); }
  copyToClipboard(text: string) { navigator.clipboard.writeText(text).then(() => this.toast.success('Copied to clipboard!', 'Copy')); }

  loadHistory() {
    this.historyLoading.set(true);
    this.api.getImportHistory(this.historyPage(), 20).subscribe({
      next: (logs) => { if (this.historyPage() === 1) this.history.set(logs); else this.history.update(h => [...h, ...logs]); this.historyLoading.set(false); },
      error: () => this.historyLoading.set(false)
    });
  }

  loadMoreHistory() { this.historyPage.update(p => p + 1); this.loadHistory(); }
}