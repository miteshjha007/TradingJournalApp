import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DecimalPipe, CurrencyPipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { Instrument, RiskResult, PropRiskResult } from '../../models/models';

@Component({
  selector: 'app-risk-tool',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, DecimalPipe, CurrencyPipe],
  styles: [`
    .tabs { display:flex; gap:0; border-bottom:2px solid var(--border); margin-bottom:1.5rem; }
    .tab-btn { padding:.75rem 1.5rem; background:none; border:none; border-bottom:3px solid transparent; color:var(--text-secondary); font-size:.9rem; font-weight:500; cursor:pointer; transition:all .2s; margin-bottom:-2px; }
    .tab-btn.active { color:var(--accent); border-bottom-color:var(--accent); }
    .tab-btn:hover:not(.active) { color:var(--text-primary); }
    .risk-layout { display:grid; grid-template-columns:1fr 1fr; gap:1.5rem; }
    @media(max-width:900px){ .risk-layout { grid-template-columns:1fr; } }
    .risk-form-card,.risk-result-card,.risk-guide-card { background:var(--card-bg); border:1px solid var(--border); border-radius:12px; padding:1.5rem; }
    .risk-form-card h3,.risk-result-card h3,.risk-guide-card h3 { margin:0 0 1.2rem; font-size:1rem; font-weight:600; }
    .form-group { margin-bottom:1rem; }
    .form-group label { display:block; font-size:.8rem; font-weight:500; color:var(--text-secondary); margin-bottom:.4rem; }
    .form-input { width:100%; padding:.6rem .8rem; background:var(--input-bg,#1e2740); border:1px solid var(--border); border-radius:8px; color:var(--text-primary); font-size:.9rem; box-sizing:border-box; }
    .form-row { display:grid; grid-template-columns:1fr 1fr; gap:.75rem; }
    .btn-full { width:100%; margin-top:.5rem; padding:.75rem; border:none; border-radius:8px; font-weight:600; cursor:pointer; font-size:.9rem; display:flex; align-items:center; justify-content:center; gap:.5rem; }
    .btn-primary { background:linear-gradient(135deg,#6366f1,#8b5cf6); color:#fff; }
    .btn-primary:disabled { opacity:.5; cursor:not-allowed; }
    .result-grid { display:grid; grid-template-columns:1fr 1fr; gap:.75rem; }
    .result-item { background:var(--bg,#0f1629); border-radius:8px; padding:.9rem; border:1px solid var(--border); }
    .result-item.highlight { border-color:#6366f1; background:rgba(99,102,241,.1); }
    .result-item.safe { border-color:#22c55e; background:rgba(34,197,94,.08); }
    .result-item.danger { border-color:#ef4444; background:rgba(239,68,68,.08); }
    .result-item.warning-item { border-color:#f59e0b; background:rgba(245,158,11,.08); }
    .result-label { display:block; font-size:.72rem; color:var(--text-secondary); margin-bottom:.3rem; text-transform:uppercase; letter-spacing:.04em; }
    .result-value { font-size:1.3rem; font-weight:700; color:var(--text-primary); }
    .result-value.primary { color:#6366f1; font-size:1.6rem; }
    .result-value.green { color:#22c55e; }
    .result-value.red { color:#ef4444; }
    .result-value.amber { color:#f59e0b; }
    .risk-warning { background:rgba(245,158,11,.1); border:1px solid #f59e0b; border-radius:8px; padding:.8rem 1rem; font-size:.82rem; color:#f59e0b; margin-top:1rem; line-height:1.5; }
    .risk-breached { background:rgba(239,68,68,.1); border:1px solid #ef4444; border-radius:8px; padding:.8rem 1rem; font-size:.82rem; color:#ef4444; margin-top:1rem; }
    .safe-badge { display:inline-flex; align-items:center; gap:.4rem; padding:.4rem .9rem; border-radius:999px; font-size:.8rem; font-weight:600; margin-bottom:.8rem; }
    .safe-badge.safe { background:rgba(34,197,94,.15); color:#22c55e; }
    .safe-badge.unsafe { background:rgba(239,68,68,.15); color:#ef4444; }
    .risk-meter { height:6px; background:var(--border); border-radius:3px; overflow:hidden; margin:.5rem 0 .3rem; }
    .risk-meter-fill { height:100%; border-radius:3px; transition:width .3s; }
    .risk-low { background:#22c55e; } .risk-med { background:#f59e0b; } .risk-high { background:#ef4444; }
    .risk-label-text { font-size:.75rem; color:var(--text-secondary); }
    .guide-items { display:flex; flex-direction:column; gap:.8rem; }
    .guide-item { display:flex; align-items:flex-start; gap:.8rem; }
    .guide-icon { font-size:1.2rem; }
    .guide-item h4 { margin:0 0 .2rem; font-size:.85rem; }
    .guide-item p { margin:0; font-size:.78rem; color:var(--text-secondary); }
    .pip-info { background:rgba(99,102,241,.08); border:1px solid rgba(99,102,241,.2); border-radius:8px; padding:.7rem 1rem; font-size:.78rem; color:var(--text-secondary); margin-top:.5rem; }
    .pip-info strong { color:#a78bfa; }
    .spinner { width:16px; height:16px; border:2px solid rgba(255,255,255,.3); border-top-color:#fff; border-radius:50%; animation:spin .7s linear infinite; display:inline-block; }
    @keyframes spin { to { transform:rotate(360deg); } }
    .full-width { grid-column:1/-1; }
  `],
  template: `
<div class="page-wrapper">
  <div class="page-header">
    <div>
      <h1 class="page-title-h1">Risk Management Tool</h1>
      <p class="page-desc">Prop firm lot calculator &amp; generic risk analyser</p>
    </div>
    @if (activeAccount()) {
      <div style="background:var(--card-bg); padding:0.5rem 1rem; border-radius:8px; border:1px solid var(--accent); font-size:0.85rem;">
        Active Account: <strong style="color:var(--accent)">{{activeAccount()!.name}}</strong> ({{activeAccount()!.balance | currency}})
      </div>
    }
  </div>

  <!-- Tabs -->
  <div class="tabs">
    <button class="tab-btn" [class.active]="activeTab()===0" (click)="activeTab.set(0)">⚖️ Prop Firm Calculator</button>
    <button class="tab-btn" [class.active]="activeTab()===1" (click)="activeTab.set(1)">📐 Generic Risk Tool</button>
  </div>

  <!-- TAB 0 — Prop Firm Calculator -->
  @if (activeTab()===0) {
    <div class="risk-layout">
      <div class="risk-form-card">
        <h3>🏦 Prop Firm Lot Size Calculator</h3>
        <form [formGroup]="propForm" (ngSubmit)="calcProp()">
          <div class="form-row">
            <div class="form-group">
              <label>Account Balance ($)</label>
              <input type="number" formControlName="accountBalance" class="form-input" placeholder="5000"/>
            </div>
            <div class="form-group">
              <label>Risk % per Trade</label>
              <input type="number" step="0.1" formControlName="riskPercent" class="form-input" placeholder="1.0"/>
              <div class="risk-meter">
                <div class="risk-meter-fill"
                  [style.width.%]="mathMin((propForm.value.riskPercent||0)*20,100)"
                  [class.risk-low]="(propForm.value.riskPercent||0)<=1"
                  [class.risk-med]="(propForm.value.riskPercent||0)>1&&(propForm.value.riskPercent||0)<=2"
                  [class.risk-high]="(propForm.value.riskPercent||0)>2"></div>
              </div>
              <span class="risk-label-text">{{(propForm.value.riskPercent||0)<=1?'🟢 Conservative':(propForm.value.riskPercent||0)<=2?'🟡 Moderate':'🔴 Aggressive'}}</span>
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Stop Loss (Pips / Points)</label>
              <input type="number" step="0.5" formControlName="stopLossPips" class="form-input" placeholder="e.g. 15 for XAUUSD"/>
            </div>
            <div class="form-group">
              <label>Instrument</label>
              <select formControlName="instrumentId" class="form-input" (change)="onPropInstrumentChange()">
                <option value="">-- Select Instrument --</option>
                @for (i of instruments(); track i.id) {
                  <option [value]="i.id">{{i.symbol || i.name}}</option>
                }
              </select>
            </div>
          </div>
          <div class="form-group">
            <label>Today's Realized Profit/Loss ($) <span style="color:var(--text-secondary);font-size:.72rem">(Positive for profit, negative for loss)</span></label>
            <input type="number" step="1" formControlName="todayLoss" class="form-input" placeholder="e.g. 50 or -20"/>
          </div>
          <div class="form-group">
            <label>First Trade Lot Today <span style="color:var(--text-secondary);font-size:.72rem">(for 5x Rule — leave 0 if first trade)</span></label>
            <input type="number" step="0.001" formControlName="firstTradeLotSize" class="form-input" placeholder="0.01"/>
          </div>
          @if (selectedPipInfo()) {
            <div class="pip-info">📌 <strong>{{selectedPipInfo()!.symbol}}</strong> — Pip Value: <strong>\${{selectedPipInfo()!.pipVal}} per 0.01 lot</strong> · Category: <strong>{{selectedPipInfo()!.cat}}</strong></div>
          }
          <button type="submit" class="btn btn-primary btn-full" [disabled]="propForm.invalid||calcingProp()">
            @if(calcingProp()){<span class="spinner"></span>} Calculate Prop Lot Size
          </button>
        </form>
      </div>

      @if (propResult()) {
        <div class="risk-result-card">
          <h3>📊 Prop Firm Risk Analysis</h3>
          <div class="safe-badge" [class.safe]="propResult()!.isSafe" [class.unsafe]="!propResult()!.isSafe">
            {{propResult()!.isSafe ? '✅ Safe to Trade' : '🚫 Review Before Trading'}}
          </div>
          <div class="result-grid">
            <div class="result-item highlight full-width">
              <span class="result-label">Suggested Lot Size</span>
              <span class="result-value primary">{{propResult()!.suggestedLotSize | number:'1.2-3'}}</span>
            </div>
            <div class="result-item">
              <span class="result-label">Max Loss if SL Hit</span>
              <span class="result-value" [class.red]="propResult()!.maxLossIfSLHit>propResult()!.riskAmountDollar*1.1">\${{propResult()!.maxLossIfSLHit | number:'1.2-2'}}</span>
            </div>
            <div class="result-item">
              <span class="result-label">Risk Amount</span>
              <span class="result-value">\${{propResult()!.riskAmountDollar | number:'1.2-2'}}</span>
            </div>
            <div class="result-item" [class.danger]="propResult()!.violatesFiveXRule" [class.safe]="!propResult()!.violatesFiveXRule&&propResult()!.fiveXRuleMaxLot>0">
              <span class="result-label">5x Rule Max Lot</span>
              <span class="result-value" [class.red]="propResult()!.violatesFiveXRule" [class.green]="!propResult()!.violatesFiveXRule&&propResult()!.fiveXRuleMaxLot>0">
                {{propResult()!.fiveXRuleMaxLot>0?(propResult()!.fiveXRuleMaxLot|number:'1.2-3'):'N/A (1st trade)'}}
              </span>
            </div>
            <div class="result-item" [class.danger]="propResult()!.dailyDrawdownBreached" [class.warning-item]="!propResult()!.dailyDrawdownBreached&&propResult()!.dailyDrawdownRemaining<50">
              <span class="result-label">DD Remaining Today</span>
              <span class="result-value" [class.red]="propResult()!.dailyDrawdownBreached" [class.amber]="!propResult()!.dailyDrawdownBreached&&propResult()!.dailyDrawdownRemaining<50" [class.green]="!propResult()!.dailyDrawdownBreached&&propResult()!.dailyDrawdownRemaining>=50">
                \${{propResult()!.dailyDrawdownRemaining | number:'1.2-2'}}
              </span>
            </div>
            <div class="result-item">
              <span class="result-label">DD Limit Amount</span>
              <span class="result-value">\${{propResult()!.dailyDrawdownLimitAmount | number:'1.2-2'}}</span>
            </div>
            <div class="result-item">
              <span class="result-label">Pip Value / 0.01 lot</span>
              <span class="result-value">\${{propResult()!.pipValuePer001Lot}}</span>
            </div>
            <div class="result-item">
              <span class="result-label">Category</span>
              <span class="result-value">{{propResult()!.instrumentCategory}}</span>
            </div>
          </div>
          @if (propResult()!.warning) {
            <div [class]="propResult()!.dailyDrawdownBreached?'risk-breached':'risk-warning'">{{propResult()!.warning}}</div>
          }
        </div>
      } @else {
        <div class="risk-guide-card">
          <h3>📋 Prop Firm Rules Reference</h3>
          <div class="guide-items">
            <div class="guide-item"><span class="guide-icon">🔢</span><div><h4>Pip Value Guide</h4><p>XAUUSD: $1/pip · Forex: $0.10/pip · JPY pairs: ~$0.09/pip (all per 0.01 lot)</p></div></div>
            <div class="guide-item"><span class="guide-icon">5️⃣</span><div><h4>5x Lot Rule</h4><p>Every subsequent trade lot must be ≤ 5× your first trade lot that day.</p></div></div>
            <div class="guide-item"><span class="guide-icon">📉</span><div><h4>Daily Drawdown</h4><p>Max 3% of account per day ($150 for $5k). Breaching = immediate fail.</p></div></div>
            <div class="guide-item"><span class="guide-icon">💰</span><div><h4>Max Per-Trade Loss</h4><p>Keep each trade risk ≤ 1% ($50 on $5k) to protect daily DD limit.</p></div></div>
            <div class="guide-item"><span class="guide-icon">🎯</span><div><h4>Profit Target</h4><p>$500 (10%) to pass. No single trade should contribute more than 20% ($100) of target.</p></div></div>
          </div>
        </div>
      }
    </div>
  }

  <!-- TAB 1 — Generic Risk Tool -->
  @if (activeTab()===1) {
    <div class="risk-layout">
      <div class="risk-form-card">
        <h3>⚖️ Generic Risk Calculator</h3>
        <form [formGroup]="genericForm" (ngSubmit)="calcGeneric()">
          <div class="form-group">
            <label>Account Balance ($)</label>
            <input type="number" formControlName="accountBalance" class="form-input" placeholder="10000"/>
          </div>
          <div class="form-group">
            <label>Risk Percentage (%)</label>
            <input type="number" step="0.1" formControlName="riskPercent" class="form-input" placeholder="1.0"/>
            <div class="risk-meter">
              <div class="risk-meter-fill"
                [style.width.%]="mathMin((genericForm.value.riskPercent||0)*20,100)"
                [class.risk-low]="(genericForm.value.riskPercent||0)<=1"
                [class.risk-med]="(genericForm.value.riskPercent||0)>1&&(genericForm.value.riskPercent||0)<=2"
                [class.risk-high]="(genericForm.value.riskPercent||0)>2"></div>
            </div>
            <span class="risk-label-text">{{(genericForm.value.riskPercent||0)<=1?'🟢 Conservative':(genericForm.value.riskPercent||0)<=2?'🟡 Moderate':'🔴 Aggressive'}}</span>
          </div>
          <div class="form-group">
            <label>Instrument (optional)</label>
            <select formControlName="instrumentId" class="form-input">
              <option value="">General Calculation</option>
              @for (i of instruments(); track i.id) {
                <option [value]="i.id">{{i.name}} (Safe: {{i.safeLotSize}})</option>
              }
            </select>
          </div>
          <button type="submit" class="btn btn-primary btn-full" [disabled]="genericForm.invalid||calcingGeneric()">
            @if(calcingGeneric()){<span class="spinner"></span>} Calculate Risk
          </button>
        </form>
      </div>

      @if (genericResult()) {
        <div class="risk-result-card">
          <h3>📊 Risk Analysis Results</h3>
          <div class="result-grid">
            <div class="result-item highlight full-width">
              <span class="result-label">Suggested Lot Size</span>
              <span class="result-value primary">{{genericResult()!.suggestedLotSize | number:'1.2-2'}}</span>
            </div>
            <div class="result-item"><span class="result-label">Risk Amount</span><span class="result-value">{{genericResult()!.riskAmount | currency}}</span></div>
            <div class="result-item"><span class="result-label">Max Allowed Lot</span><span class="result-value">{{genericResult()!.maxAllowedLotSize | number:'1.2-2'}}</span></div>
            <div class="result-item"><span class="result-label">Max Trades/Day</span><span class="result-value">{{genericResult()!.maxTradesPerDay}}</span></div>
            <div class="result-item full-width"><span class="result-label">Risk Level</span><span class="result-value">{{genericResult()!.riskLevel}}</span></div>
          </div>
          @if(genericResult()!.warning){<div class="risk-warning">⚠️ {{genericResult()!.warning}}</div>}
        </div>
      } @else {
        <div class="risk-guide-card">
          <h3>📋 Risk Management Guide</h3>
          <div class="guide-items">
            <div class="guide-item"><span class="guide-icon">🟢</span><div><h4>Conservative (up to 1%)</h4><p>Risk max 1% per trade. Max 10 trades/day. Best for consistent growth.</p></div></div>
            <div class="guide-item"><span class="guide-icon">🟡</span><div><h4>Moderate (1-2%)</h4><p>Risk 1-2% per trade. Max 5 trades/day. Balanced approach.</p></div></div>
            <div class="guide-item"><span class="guide-icon">🔴</span><div><h4>Aggressive (above 2%)</h4><p>Risk more than 2% per trade. Max 3 trades/day. High risk, high reward.</p></div></div>
          </div>
        </div>
      }
    </div>
  }
</div>
  `
})
export class RiskToolComponent implements OnInit {
  instruments = signal<Instrument[]>([]);
  genericResult = signal<RiskResult | null>(null);
  propResult = signal<PropRiskResult | null>(null);
  activeAccount = signal<any>(null);
  calcingGeneric = signal(false);
  calcingProp = signal(false);
  activeTab = signal(0);
  selectedPipInfo = signal<{symbol:string; pipVal:number; cat:string} | null>(null);

  genericForm;
  propForm;

  constructor(private api: ApiService, private fb: FormBuilder, private toast: ToastService) {
    this.genericForm = this.fb.group({
      accountBalance: [10000, [Validators.required, Validators.min(1)]],
      riskPercent: [1.0, [Validators.required, Validators.min(0.1), Validators.max(10)]],
      instrumentId: ['']
    });
    this.propForm = this.fb.group({
      accountBalance: [5000, [Validators.required, Validators.min(1)]],
      riskPercent: [1.0, [Validators.required, Validators.min(0.1), Validators.max(10)]],
      stopLossPips: [15, [Validators.required, Validators.min(0.1)]],
      instrumentId: [''],
      dailyDrawdownLimit: [3.0],
      todayLoss: [0],
      firstTradeLotSize: [0, [Validators.min(0)]]
    });
  }

  mathMin(a: number, b: number) { return Math.min(a, b); }

  ngOnInit() {
    this.api.getInstruments().subscribe(data => this.instruments.set(data));
    this.api.getAccounts().subscribe(accounts => {
      const active = accounts.find(a => a.isDefault) || accounts[0];
      if (active) {
        this.activeAccount.set(active);
        this.propForm.patchValue({ accountBalance: active.balance });
        this.genericForm.patchValue({ accountBalance: active.balance });
      }
    });
  }

  onPropInstrumentChange() {
    const id = this.propForm.value.instrumentId;
    if (!id) { this.selectedPipInfo.set(null); return; }
    const inst = this.instruments().find(i => i.id === id);
    if (!inst) return;
    const sym = (inst.symbol || inst.name).toUpperCase();
    let pipVal = 0.10; let cat = 'Forex';
    if (sym.includes('XAUUSD') || sym.includes('GOLD')) { pipVal = 1.00; cat = 'Metals'; }
    else if (sym.includes('XAGUSD') || sym.includes('SILVER')) { pipVal = 0.50; cat = 'Metals'; }
    else if (sym.includes('BTC')) { pipVal = 0.001; cat = 'Crypto'; }
    else if (sym.includes('ETH')) { pipVal = 0.01; cat = 'Crypto'; }
    else if (sym.includes('JPY')) { pipVal = 0.09; cat = 'Forex-JPY'; }
    else if (sym.includes('MXN') || sym.includes('TRY') || sym.includes('ZAR')) { pipVal = 0.01; cat = 'Forex-Exotic'; }
    this.selectedPipInfo.set({ symbol: sym, pipVal, cat });
  }

  calcGeneric() {
    if (this.genericForm.invalid) return;
    this.calcingGeneric.set(true);
    const v = this.genericForm.value;
    this.api.calculateRisk({
      accountBalance: v.accountBalance ?? 10000,
      riskPercent: v.riskPercent ?? 1,
      instrumentId: v.instrumentId || undefined
    }).subscribe({
      next: d => { this.genericResult.set(d); this.calcingGeneric.set(false); this.toast.success('Risk calculated.', 'Done'); },
      error: e => { this.calcingGeneric.set(false); this.toast.error(e.error?.error || 'Calculation failed.', 'Error'); }
    });
  }

  calcProp() {
    if (this.propForm.invalid) return;
    this.calcingProp.set(true);
    this.propResult.set(null);
    const v = this.propForm.value;
    const inst = v.instrumentId ? this.instruments().find(i => i.id === v.instrumentId) : null;
    this.api.calculatePropRisk({
      accountBalance: v.accountBalance ?? 5000,
      riskPercent: v.riskPercent ?? 1,
      stopLossPips: v.stopLossPips ?? 15,
      instrumentId: v.instrumentId || undefined,
      instrumentSymbol: inst?.symbol || inst?.name || '',
      firstTradeLotSize: (v.firstTradeLotSize && v.firstTradeLotSize > 0) ? v.firstTradeLotSize : undefined,
      dailyDrawdownLimit: v.dailyDrawdownLimit ?? 3,
      todayLoss: v.todayLoss ?? 0
    }).subscribe({
      next: d => { this.propResult.set(d); this.calcingProp.set(false); this.toast.success('Prop lot calculated.', 'Done'); },
      error: e => { this.calcingProp.set(false); this.toast.error(e.error?.error || 'Calculation failed.', 'Error'); }
    });
  }
}
