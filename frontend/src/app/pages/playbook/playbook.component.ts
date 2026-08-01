import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { PlaybookRule, CreatePlaybookRule, UpdatePlaybookRule, PlaybookCategory } from '../../models/models';
import { InfoTooltipDirective } from '../../directives/info-tooltip.directive';

@Component({
  selector: 'app-playbook',
  standalone: true,
  imports: [CommonModule, FormsModule, InfoTooltipDirective],
  template: `
    <div class="playbook-page">
      <div class="page-header">
        <div>
          <h1 [infoTooltip]="'trading-playbook'">Trading Playbook</h1>
          <p class="subtitle">Define your trading rules and track pre-trade checklist compliance</p>
        </div>
        <button class="btn btn-primary" (click)="openAdd()">+ Add Rule</button>
      </div>

      <!-- Stats Row -->
      <div class="stats-row">
        @for (cat of categories; track cat.value) {
          <div class="stat-card">
            <span class="cat-icon">{{ cat.icon }}</span>
            <div>
              <div class="stat-value">{{ rulesByCategory(cat.value).length }}</div>
              <div class="stat-label" [infoTooltip]="cat.label.toLowerCase() + '-rule'">{{ cat.label }}</div>
            </div>
          </div>
        }
        <div class="stat-card total">
          <span class="cat-icon">📋</span>
          <div>
            <div class="stat-value">{{ rules().length }}</div>
            <div class="stat-label">Total Rules</div>
          </div>
        </div>
      </div>

      <!-- Rules by Category -->
      @for (cat of categories; track cat.value) {
        @if (rulesByCategory(cat.value).length > 0) {
          <div class="category-section">
            <h3 class="category-header">
              <span>{{ cat.icon }} {{ cat.label }}</span>
              <span class="rule-count">{{ rulesByCategory(cat.value).length }} rules</span>
            </h3>
            <div class="rules-list">
              @for (rule of rulesByCategory(cat.value); track rule.id; let i = $index) {
                <div class="rule-card" [class.inactive]="!rule.isActive">
                  <div class="rule-order">
                    <button class="order-btn" (click)="moveUp(rule)" [disabled]="i === 0">▲</button>
                    <span>{{ rule.orderIndex }}</span>
                    <button class="order-btn" (click)="moveDown(rule)" [disabled]="i === rulesByCategory(cat.value).length - 1">▼</button>
                  </div>
                  <div class="rule-content">
                    <div class="rule-title">
                      {{ rule.title }}
                      @if (!rule.isActive) { <span class="inactive-badge">Inactive</span> }
                    </div>
                    @if (rule.description) {
                      <div class="rule-desc">{{ rule.description }}</div>
                    }
                  </div>
                  <div class="rule-actions">
                    <button class="btn-icon" (click)="openEdit(rule)" title="Edit">✏️</button>
                    <button class="btn-icon danger" (click)="deleteRule(rule.id)" title="Delete">🗑️</button>
                  </div>
                </div>
              }
            </div>
          </div>
        }
      }

      @if (rules().length === 0 && !loading()) {
        <div class="empty-state">
          <div class="empty-icon">📋</div>
          <h3>No rules yet</h3>
          <p>Add your first trading rule to start building your playbook</p>
          <button class="btn btn-primary" (click)="openAdd()">Add First Rule</button>
        </div>
      }

      @if (loading()) {
        <div class="loading-state">Loading rules...</div>
      }
    </div>

    <!-- Add/Edit Modal -->
    @if (showModal()) {
      <div class="modal-overlay" (click)="closeModal()">
        <div class="modal" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h3>{{ editingRule ? 'Edit Rule' : 'Add Rule' }}</h3>
            <button class="modal-close" (click)="closeModal()">✕</button>
          </div>
          <div class="modal-body">
            <div class="form-group">
              <label>Title *</label>
              <input [(ngModel)]="form.title" placeholder="e.g. Only trade with trend" class="form-control" />
            </div>
            <div class="form-group">
              <label>Description</label>
              <textarea [(ngModel)]="form.description" placeholder="Detailed explanation of this rule..." class="form-control" rows="3"></textarea>
            </div>
            <div class="form-group">
              <label>Category *</label>
              <select [(ngModel)]="form.category" class="form-control category-select">
                @for (cat of categories; track cat.value) {
                  <option [ngValue]="cat.value">{{ cat.icon }} {{ cat.label }}</option>
                }
              </select>
            </div>
            @if (editingRule) {
              <div class="form-group">
                <label class="checkbox-label">
                  <input type="checkbox" [(ngModel)]="form.isActive" />
                  Active (included in pre-trade checklist)
                </label>
              </div>
            }
          </div>
          <div class="modal-footer">
            <button class="btn btn-ghost" (click)="closeModal()">Cancel</button>
            <button class="btn btn-primary" (click)="saveRule()" [disabled]="saving()">
              {{ saving() ? 'Saving...' : (editingRule ? 'Update' : 'Create') }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .playbook-page { padding: 1.5rem; max-width: 900px; margin: 0 auto; }
    .page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 1.5rem; }
    .page-header h1 { margin: 0 0 0.25rem; font-size: 1.6rem; }
    .subtitle { color: var(--text-muted); margin: 0; font-size: 0.9rem; }

    .stats-row { display: grid; grid-template-columns: repeat(5, 1fr); gap: 1rem; margin-bottom: 2rem; }
    .stat-card { display: flex; align-items: center; gap: 0.75rem; padding: 1rem; background: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--border-radius); }
    .stat-card.total { background: var(--primary-light); border-color: var(--primary); }
    .cat-icon { font-size: 1.5rem; }
    .stat-value { font-size: 1.4rem; font-weight: 700; }
    .stat-label { font-size: 0.75rem; color: var(--text-muted); }

    .category-section { margin-bottom: 1.5rem; }
    .category-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem; font-size: 1rem; color: var(--text-muted); }
    .rule-count { font-size: 0.8rem; background: var(--bg-hover); padding: 0.2rem 0.6rem; border-radius: 9999px; }

    .rules-list { display: flex; flex-direction: column; gap: 0.5rem; }
    .rule-card { display: flex; align-items: center; gap: 1rem; padding: 0.875rem 1rem; background: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--border-radius); transition: var(--transition); }
    .rule-card:hover { border-color: var(--primary); }
    .rule-card.inactive { opacity: 0.5; }

    .rule-order { display: flex; flex-direction: column; align-items: center; gap: 0.1rem; min-width: 30px; color: var(--text-muted); font-size: 0.75rem; }
    .order-btn { background: none; border: none; cursor: pointer; color: var(--text-muted); padding: 0; line-height: 1; font-size: 0.7rem; }
    .order-btn:hover:not(:disabled) { color: var(--primary); }
    .order-btn:disabled { opacity: 0.3; cursor: not-allowed; }

    .rule-content { flex: 1; }
    .rule-title { font-weight: 600; display: flex; align-items: center; gap: 0.5rem; }
    .rule-desc { font-size: 0.8rem; color: var(--text-muted); margin-top: 0.2rem; }
    .inactive-badge { font-size: 0.65rem; background: var(--bg-hover); border: 1px solid var(--border-color); padding: 0.1rem 0.4rem; border-radius: 4px; color: var(--text-muted); font-weight: 400; }

    .rule-actions { display: flex; gap: 0.5rem; }
    .btn-icon { background: none; border: none; cursor: pointer; font-size: 1rem; padding: 0.25rem; border-radius: 4px; transition: var(--transition); }
    .btn-icon:hover { background: var(--bg-hover); }
    .btn-icon.danger:hover { background: rgba(239,68,68,0.1); }

    .empty-state { text-align: center; padding: 3rem; color: var(--text-muted); }
    .empty-icon { font-size: 3rem; margin-bottom: 1rem; }
    .empty-state h3 { margin: 0 0 0.5rem; }
    .loading-state { text-align: center; padding: 2rem; color: var(--text-muted); }

    .modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal { background: var(--bg-card); border-radius: var(--border-radius); width: 500px; max-width: 95vw; box-shadow: var(--shadow-lg); }
    .modal-header { display: flex; justify-content: space-between; align-items: center; padding: 1.25rem 1.5rem; border-bottom: 1px solid var(--border-color); }
    .modal-header h3 { margin: 0; }
    .modal-close { background: none; border: none; font-size: 1.2rem; cursor: pointer; color: var(--text-muted); }
    .modal-body { padding: 1.5rem; display: flex; flex-direction: column; gap: 1rem; }
    .modal-footer { display: flex; justify-content: flex-end; gap: 0.75rem; padding: 1rem 1.5rem; border-top: 1px solid var(--border-color); }

    .form-group { display: flex; flex-direction: column; gap: 0.4rem; }
    .form-group label { font-size: 0.85rem; font-weight: 600; color: var(--text-muted); }
    .form-control { padding: 0.625rem 0.875rem; border: 1px solid var(--border-color); border-radius: var(--border-radius); background: var(--bg-input); color: var(--text-main); font-family: var(--font-family); font-size: 0.9rem; }
    .form-control:focus { outline: none; border-color: var(--primary); }
    .category-select option { background-color: #1e293b; color: #f8fafc; } /* Force dark background for options */
    textarea.form-control { resize: vertical; }
    .checkbox-label { display: flex; align-items: center; gap: 0.5rem; cursor: pointer; font-size: 0.9rem; color: var(--text-main); }

    .btn { padding: 0.625rem 1.25rem; border-radius: var(--border-radius); font-size: 0.875rem; font-weight: 600; cursor: pointer; border: none; transition: var(--transition); }
    .btn-primary { background: var(--primary); color: white; }
    .btn-primary:hover:not(:disabled) { opacity: 0.9; }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
    .btn-ghost { background: transparent; border: 1px solid var(--border-color); color: var(--text-main); }
    .btn-ghost:hover { background: var(--bg-hover); }
  `]
})
export class PlaybookComponent implements OnInit {
  rules = signal<PlaybookRule[]>([]);
  loading = signal(true);
  saving = signal(false);
  showModal = signal(false);
  editingRule: PlaybookRule | null = null;

  form: { title: string; description: string; category: number; isActive: boolean } = {
    title: '', description: '', category: PlaybookCategory.Entry, isActive: true
  };

  categories = [
    { value: PlaybookCategory.Entry, label: 'Entry', icon: '🎯' },
    { value: PlaybookCategory.Risk, label: 'Risk', icon: '⚡' },
    { value: PlaybookCategory.Psychology, label: 'Psychology', icon: '🧠' },
    { value: PlaybookCategory.Exit, label: 'Exit', icon: '🚪' },
  ];

  constructor(private api: ApiService, private toast: ToastService) {}

  ngOnInit() { this.loadRules(); }

  loadRules() {
    this.loading.set(true);
    this.api.getPlaybookRules().subscribe({
      next: (r) => { this.rules.set(r); this.loading.set(false); },
      error: () => { this.toast.error('Failed to load rules'); this.loading.set(false); }
    });
  }

  rulesByCategory(cat: PlaybookCategory): PlaybookRule[] {
    return this.rules().filter(r => r.category === cat).sort((a, b) => a.orderIndex - b.orderIndex);
  }

  openAdd() {
    this.editingRule = null;
    this.form = { title: '', description: '', category: PlaybookCategory.Entry, isActive: true };
    this.showModal.set(true);
  }

  openEdit(rule: PlaybookRule) {
    this.editingRule = rule;
    this.form = { title: rule.title, description: rule.description ?? '', category: rule.category, isActive: rule.isActive };
    this.showModal.set(true);
  }

  closeModal() { this.showModal.set(false); }

  saveRule() {
    if (!this.form.title.trim()) { this.toast.error('Title is required'); return; }
    this.saving.set(true);
    if (this.editingRule) {
      const dto: UpdatePlaybookRule = { title: this.form.title, description: this.form.description, category: this.form.category, isActive: this.form.isActive };
      this.api.updatePlaybookRule(this.editingRule.id, dto).subscribe({
        next: (r) => {
          this.rules.update(list => list.map(x => x.id === r.id ? r : x));
          this.toast.success('Rule updated'); this.saving.set(false); this.closeModal();
        },
        error: () => { this.toast.error('Failed to update rule'); this.saving.set(false); }
      });
    } else {
      const dto: CreatePlaybookRule = { title: this.form.title, description: this.form.description, category: this.form.category };
      this.api.createPlaybookRule(dto).subscribe({
        next: (r) => {
          this.rules.update(list => [...list, r]);
          this.toast.success('Rule created'); this.saving.set(false); this.closeModal();
        },
        error: () => { this.toast.error('Failed to create rule'); this.saving.set(false); }
      });
    }
  }

  deleteRule(id: string) {
    if (!confirm('Delete this rule?')) return;
    this.api.deletePlaybookRule(id).subscribe({
      next: () => { this.rules.update(list => list.filter(r => r.id !== id)); this.toast.success('Rule deleted'); },
      error: () => this.toast.error('Failed to delete rule')
    });
  }

  moveUp(rule: PlaybookRule) {
    const catRules = this.rulesByCategory(rule.category);
    const idx = catRules.findIndex(r => r.id === rule.id);
    if (idx <= 0) return;
    const other = catRules[idx - 1];
    const newOrder = this.rules().map(r => {
      if (r.id === rule.id) return { ...r, orderIndex: other.orderIndex };
      if (r.id === other.id) return { ...r, orderIndex: rule.orderIndex };
      return r;
    });
    this.rules.set(newOrder);
    this.saveOrder();
  }

  moveDown(rule: PlaybookRule) {
    const catRules = this.rulesByCategory(rule.category);
    const idx = catRules.findIndex(r => r.id === rule.id);
    if (idx >= catRules.length - 1) return;
    const other = catRules[idx + 1];
    const newOrder = this.rules().map(r => {
      if (r.id === rule.id) return { ...r, orderIndex: other.orderIndex };
      if (r.id === other.id) return { ...r, orderIndex: rule.orderIndex };
      return r;
    });
    this.rules.set(newOrder);
    this.saveOrder();
  }

  private saveOrder() {
    const ordered = [...this.rules()].sort((a, b) => a.orderIndex - b.orderIndex).map(r => r.id);
    this.api.reorderPlaybookRules(ordered).subscribe({
      error: () => this.toast.error('Failed to save order')
    });
  }
}
