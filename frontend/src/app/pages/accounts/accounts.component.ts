import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule, CurrencyPipe, DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { TradingAccount, CreateTradingAccount } from '../../models/models';
import { InfoTooltipDirective } from '../../directives/info-tooltip.directive';

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, CurrencyPipe, DecimalPipe, DatePipe, InfoTooltipDirective],
  styles: [`
    .accounts-grid { display:grid; grid-template-columns:repeat(auto-fill,minmax(300px,1fr)); gap:1.5rem; margin-top:1.5rem; }
    .account-card { background:var(--card-bg); border:1px solid var(--border); border-radius:12px; padding:1.5rem; display:flex; flex-direction:column; gap:1rem; position:relative; }
    .account-card.is-default { border-color:var(--accent); background:rgba(99,102,241,0.05); }
    .default-badge { position:absolute; top:1rem; right:1rem; background:var(--accent); color:#fff; font-size:0.7rem; padding:0.2rem 0.6rem; border-radius:1rem; font-weight:600; }
    .acc-header { margin-bottom:0.5rem; }
    .acc-name { font-size:1.1rem; font-weight:600; margin:0 0 0.2rem; }
    .acc-broker { font-size:0.8rem; color:var(--text-secondary); margin:0; }
    .acc-balance { font-size:1.5rem; font-weight:700; color:var(--text-primary); }
    .acc-prop { font-size:0.8rem; display:flex; gap:0.5rem; flex-wrap:wrap; margin-top:0.5rem; }
    .prop-tag { background:rgba(245,158,11,0.1); color:#f59e0b; padding:0.2rem 0.5rem; border-radius:4px; font-weight:500; }
    .acc-actions { display:flex; gap:0.5rem; margin-top:auto; padding-top:1rem; border-top:1px solid var(--border); }
    .btn-sm { padding:0.4rem 0.8rem; font-size:0.8rem; border-radius:6px; font-weight:500; cursor:pointer; border:1px solid var(--border); background:var(--bg); color:var(--text-primary); }
    .btn-sm:hover { background:var(--border); }
    .btn-sm.delete:hover { background:rgba(239,68,68,0.1); color:#ef4444; border-color:#ef4444; }
    
    .modal-overlay { position:fixed; inset:0; background:rgba(0,0,0,0.5); display:flex; align-items:center; justify-content:center; z-index:100; backdrop-filter:blur(4px); }
    .modal-content { background:var(--card-bg); width:90%; max-width:600px; border-radius:12px; border:1px solid var(--border); max-height:90vh; display:flex; flex-direction:column; }
    .modal-header { padding:1.2rem 1.5rem; border-bottom:1px solid var(--border); display:flex; justify-content:space-between; align-items:center; }
    .modal-header h3 { margin:0; font-size:1.2rem; }
    .close-btn { background:none; border:none; color:var(--text-secondary); font-size:1.5rem; cursor:pointer; }
    .modal-body { padding:1.5rem; overflow-y:auto; }
    .modal-footer { padding:1rem 1.5rem; border-top:1px solid var(--border); display:flex; justify-content:flex-end; gap:1rem; }
    
    .form-group { margin-bottom:1rem; }
    .form-group label { display:block; font-size:0.85rem; font-weight:500; margin-bottom:0.4rem; color:var(--text-secondary); }
    .form-input { width:100%; padding:0.6rem 0.8rem; background:var(--bg); border:1px solid var(--border); border-radius:8px; color:var(--text-primary); }
    .form-row { display:grid; grid-template-columns:1fr 1fr; gap:1rem; }
    .checkbox-group { display:flex; align-items:center; gap:0.5rem; margin-top:0.5rem; }
    .prop-firm-section { background:var(--bg); border:1px solid var(--border); border-radius:8px; padding:1rem; margin-top:1rem; }
    .prop-firm-section h4 { margin:0 0 1rem; color:var(--accent); }
    
    .btn-primary { background:var(--accent); color:white; border:none; padding:0.6rem 1.2rem; border-radius:8px; cursor:pointer; font-weight:500; }
    .btn-primary:disabled { opacity:0.6; cursor:not-allowed; }
  `],
  template: `
<div class="page-wrapper">
  <div class="page-header">
    <div>
      <h1 class="page-title-h1">Trading Accounts</h1>
      <p class="page-desc">Manage your trading accounts and prop firm rules.</p>
    </div>
    <button class="btn btn-primary" (click)="openModal()">+ Add Account</button>
  </div>

  @if (isAdmin()) {
    <div class="form-group" style="max-width:300px; margin-bottom:1rem;">
      <label>Admin View: Target User ID (Leave blank for self)</label>
      <input type="text" class="form-input" [(ngModel)]="targetUserId" (change)="loadAccounts()" placeholder="User UUID">
    </div>
  }

  <div class="accounts-grid">
    @for (acc of accounts(); track acc.id) {
      <div class="account-card" [class.is-default]="acc.isDefault">
        @if (acc.isDefault) { <span class="default-badge">DEFAULT</span> }
        <div class="acc-header">
          <h3 class="acc-name">{{acc.name}}</h3>
          <p class="acc-broker">{{acc.broker || 'No Broker'}}</p>
        </div>
        <div class="acc-balance">{{acc.balance | currency:acc.currency}}</div>
        
        @if (acc.isPropFirm) {
          <div class="acc-prop">
            <span class="prop-tag">Target: {{acc.profitTargetPct}}%</span>
            <span class="prop-tag">Daily DD: {{acc.dailyDrawdownLimitPct}}%</span>
            <span class="prop-tag">Max DD: {{acc.maxOverallLossPct}}%</span>
            <span class="prop-tag">Split: {{acc.profitSplitPct}}%</span>
          </div>
        }
        
        <div class="acc-actions">
          <button class="btn-sm" (click)="openModal(acc)">Edit</button>
          <button class="btn-sm delete" (click)="deleteAccount(acc.id)">Delete</button>
        </div>
      </div>
    }
  </div>

  <!-- Modal -->
  @if (isModalOpen()) {
    <div class="modal-overlay" (click)="closeModal()">
      <div class="modal-content" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h3>{{ editingId() ? 'Edit Account' : 'Add Account' }}</h3>
          <button class="close-btn" (click)="closeModal()">&times;</button>
        </div>
        <div class="modal-body">
          <form [formGroup]="accountForm">
            @if (isAdmin()) {
              <div class="form-group">
                <label>Assign to User ID (Admin Only)</label>
                <input type="text" formControlName="userId" class="form-input" placeholder="Leave empty for yourself">
              </div>
            }
            <div class="form-row">
              <div class="form-group">
                <label>Account Name</label>
                <input type="text" formControlName="name" class="form-input" placeholder="e.g. FundedFirm $10k">
              </div>
              <div class="form-group">
                <label>Broker</label>
                <input type="text" formControlName="broker" class="form-input" placeholder="Optional">
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Initial Balance</label>
                <input type="number" formControlName="balance" class="form-input">
              </div>
              <div class="form-group">
                <label>Currency</label>
                <input type="text" formControlName="currency" class="form-input">
              </div>
            </div>
            <div class="form-group checkbox-group">
              <input type="checkbox" id="isDefault" formControlName="isDefault">
              <label for="isDefault" style="margin:0">Set as Default Account</label>
            </div>
            <div class="form-group checkbox-group">
              <input type="checkbox" id="isPropFirm" formControlName="isPropFirm">
              <label for="isPropFirm" style="margin:0; font-weight:600; color:var(--accent);" [infoTooltip]="'prop-firm-account'">This is a Prop Firm Account</label>
            </div>

            @if (accountForm.get('isPropFirm')?.value) {
              <div class="prop-firm-section">
                <h4>Prop Firm Rules</h4>
                <div class="form-row">
                  <div class="form-group">
                    <label>Daily DD Limit (%)</label>
                    <input type="number" step="0.1" formControlName="dailyDrawdownLimitPct" class="form-input">
                  </div>
                  <div class="form-group">
                    <label>Max Overall Loss (%)</label>
                    <input type="number" step="0.1" formControlName="maxOverallLossPct" class="form-input">
                  </div>
                </div>
                <div class="form-row">
                  <div class="form-group">
                    <label [infoTooltip]="'profit-target'">Profit Target (%)</label>
                    <input type="number" step="0.1" formControlName="profitTargetPct" class="form-input">
                  </div>
                  <div class="form-group">
                    <label [infoTooltip]="'profit-split'">Profit Split (%)</label>
                    <input type="number" step="1" formControlName="profitSplitPct" class="form-input">
                  </div>
                </div>
                <div class="form-row">
                  <div class="form-group">
                    <label [infoTooltip]="'risk-per-trade'">Max Risk Per Trade (% of Daily Limit)</label>
                    <input type="number" step="1" formControlName="maxRiskPerTradePctOfDailyLimit" class="form-input" placeholder="e.g. 40">
                  </div>
                  <div class="form-group">
                    <label [infoTooltip]="'lot-size'">Absolute Max Lot Size</label>
                    <input type="number" step="0.1" formControlName="maxAllowedLotSize" class="form-input">
                  </div>
                </div>
                <div class="form-row">
                  <div class="form-group checkbox-group">
                    <input type="checkbox" id="useDynamicEquity" formControlName="useDynamicEquity">
                    <label for="useDynamicEquity" style="margin:0" [infoTooltip]="'dynamic-equity'">Dynamic Equity (Add today's profit to limit)</label>
                  </div>
                  <div class="form-group checkbox-group">
                    <input type="checkbox" id="has5xLotRule" formControlName="has5xLotRule">
                    <label for="has5xLotRule" style="margin:0" [infoTooltip]="'5x-lot-rule'">Enforce 5x Lot Rule</label>
                  </div>
                </div>
                <div class="form-group">
                  <label [infoTooltip]="'daily-loss-limit'">Daily Loss Limit Basis</label>
                  <select formControlName="maxDailyLossType" class="form-input">
                    <option [ngValue]="1">Balance-Based (% of starting balance)</option>
                    <option [ngValue]="2">Equity-Based (% of current equity)</option>
                  </select>
                </div>
              </div>
            }
          </form>
        </div>
        <div class="modal-footer">
          <button class="btn-sm" (click)="closeModal()">Cancel</button>
          <button class="btn-primary" [disabled]="accountForm.invalid || isSubmitting()" (click)="saveAccount()">
            {{ isSubmitting() ? 'Saving...' : 'Save Account' }}
          </button>
        </div>
      </div>
    </div>
  }
</div>
  `
})
export class AccountsComponent implements OnInit {
  accounts = signal<TradingAccount[]>([]);
  isModalOpen = signal(false);
  editingId = signal<string | null>(null);
  isSubmitting = signal(false);
  targetUserId = '';
  
  accountForm;
  isAdmin = computed(() => this.auth.currentUser()?.role === 'Admin');

  constructor(
    private api: ApiService,
    private auth: AuthService,
    private fb: FormBuilder,
    private toast: ToastService
  ) {
    this.accountForm = this.fb.group({
      userId: [''],
      name: ['', Validators.required],
      balance: [10000, [Validators.required, Validators.min(1)]],
      currency: ['USD', Validators.required],
      broker: [''],
      isDefault: [false],
      isPropFirm: [false],
      dailyDrawdownLimitPct: [3.0],
      maxOverallLossPct: [6.0],
      profitTargetPct: [10.0],
      profitSplitPct: [80.0],
      maxRiskPerTradePctOfDailyLimit: [40.0],
      maxAllowedLotSize: [5.0],
      useDynamicEquity: [true],
      has5xLotRule: [true],
      maxDailyLossType: [1]
    });
  }

  ngOnInit() {
    this.loadAccounts();
  }

  loadAccounts() {
    const target = this.targetUserId.trim() !== '' ? this.targetUserId.trim() : undefined;
    this.api.getAccounts(target).subscribe({
      next: data => this.accounts.set(data),
      error: err => this.toast.error('Failed to load accounts')
    });
  }

  openModal(acc?: TradingAccount) {
    if (acc) {
      this.editingId.set(acc.id);
      this.accountForm.patchValue(acc);
    } else {
      this.editingId.set(null);
      this.accountForm.reset({
        balance: 10000, currency: 'USD', isDefault: this.accounts().length === 0,
        isPropFirm: false, dailyDrawdownLimitPct: 3.0, maxOverallLossPct: 6.0,
        profitTargetPct: 10.0, profitSplitPct: 80.0, maxRiskPerTradePctOfDailyLimit: 40.0,
        maxAllowedLotSize: 5.0, useDynamicEquity: true, has5xLotRule: true, maxDailyLossType: 1
      });
    }
    this.isModalOpen.set(true);
  }

  closeModal() {
    this.isModalOpen.set(false);
    this.editingId.set(null);
  }

  saveAccount() {
    if (this.accountForm.invalid) return;
    this.isSubmitting.set(true);
    const data = this.accountForm.value as CreateTradingAccount;
    // Clean up empty userId string
    if (!data.userId || data.userId.trim() === '') data.userId = undefined;
    
    const id = this.editingId();
    const req = id ? this.api.updateAccount(id, data) : this.api.createAccount(data);
    
    req.subscribe({
      next: () => {
        this.toast.success(`Account ${id ? 'updated' : 'created'} successfully!`);
        this.loadAccounts();
        this.closeModal();
        this.isSubmitting.set(false);
      },
      error: err => {
        this.toast.error(err.error?.error || 'Failed to save account');
        this.isSubmitting.set(false);
      }
    });
  }

  deleteAccount(id: string) {
    if (!confirm('Are you sure you want to delete this account?')) return;
    const target = this.targetUserId.trim() !== '' ? this.targetUserId.trim() : undefined;
    this.api.deleteAccount(id, target).subscribe({
      next: () => {
        this.toast.success('Account deleted');
        this.loadAccounts();
      },
      error: () => this.toast.error('Failed to delete account')
    });
  }
}
