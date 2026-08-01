import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { Instrument, CreateInstrument } from '../../models/models';

@Component({
  selector: 'app-instruments',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="page-wrapper">
      <div class="page-header">
        <div>
          <h1 class="page-title-h1">Instruments</h1>
          <p class="page-desc">Manage your custom trading instruments</p>
        </div>
        <button class="btn btn-primary" (click)="openModal()">+ Add Instrument</button>
      </div>

      @if (loading()) {
        <div class="loading-state"><div class="loading-spinner"></div></div>
      } @else {
        <div class="instruments-grid">
          @for (inst of instruments(); track inst.id) {
            <div class="instrument-card" [class]="getVolatilityClass(inst.volatilityLevel)" (click)="selectInstrument(inst)">
              <div class="inst-header">
                <div class="inst-symbol">{{ inst.symbol || inst.name.slice(0,4).toUpperCase() }}</div>
                <div class="inst-badge" [class]="'vol-' + inst.volatilityLevel.toLowerCase()">{{ inst.volatilityLevel }}</div>
              </div>
              <h3 class="inst-name">{{ inst.name }}</h3>
              @if (inst.description) { <p class="inst-desc">{{ inst.description }}</p> }
              <div class="inst-stats">
                <div class="stat-item">
                  <span class="stat-label">P&amp;L</span>
                  <span class="stat-value" [class.positive-text]="inst.totalPL > 0" [class.negative-text]="inst.totalPL < 0">
                    {{ inst.totalPL | number:'1.2-2' }}
                  </span>
                </div>
                <div class="stat-item">
                  <span class="stat-label">Win Rate</span>
                  <span class="stat-value">{{ inst.winRate | number:'1.1-1' }}%</span>
                </div>
                <div class="stat-item">
                  <span class="stat-label">Trades</span>
                  <span class="stat-value">{{ inst.totalTrades }}</span>
                </div>
              </div>
              <div class="inst-lots">
                <span>Safe Lot: <strong>{{ inst.safeLotSize }}</strong></span>
                <span>Max Lot: <strong>{{ inst.maxLot }}</strong></span>
              </div>
              <div class="inst-actions" (click)="$event.stopPropagation()">
                <button class="btn-icon" (click)="openModal(inst)" title="Edit">✏️</button>
                <button class="btn-icon danger" (click)="deleteInstrument(inst.id)" title="Delete">🗑️</button>
              </div>
            </div>
          }
          @empty {
            <div class="empty-state">
              <span class="empty-icon">🎯</span>
              <h3>No instruments yet</h3>
              <p>Add your first trading instrument to get started</p>
              <button class="btn btn-primary" (click)="openModal()">Add Instrument</button>
            </div>
          }
        </div>
      }

      <!-- Modal -->
      @if (showModal()) {
        <div class="modal-overlay" (click)="closeModal()">
          <div class="modal" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>{{ editingId() ? 'Edit' : 'Add' }} Instrument</h3>
              <button class="modal-close" (click)="closeModal()">✕</button>
            </div>
            <form [formGroup]="form" (ngSubmit)="onSubmit()" class="modal-body">
              <div class="form-row">
                <div class="form-group">
                  <label>Name *</label>
                  <input type="text" formControlName="name" placeholder="e.g. GOLD, EURUSD" class="form-input" />
                </div>
                <div class="form-group">
                  <label>Symbol</label>
                  <input type="text" formControlName="symbol" placeholder="e.g. XAUUSD" class="form-input" />
                </div>
              </div>
              <div class="form-row">
                <div class="form-group">
                  <label>Safe Lot Size *</label>
                  <input type="number" step="0.01" formControlName="safeLotSize" class="form-input" />
                </div>
                <div class="form-group">
                  <label>Max Lot *</label>
                  <input type="number" step="0.01" formControlName="maxLot" class="form-input" />
                </div>
              </div>
              <div class="form-group">
                <label>Volatility Level</label>
                <select formControlName="volatilityLevel" class="form-input">
                  <option [ngValue]="1">Low</option>
                  <option [ngValue]="2">Medium</option>
                  <option [ngValue]="3">High</option>
                </select>
              </div>
              <div class="form-group">
                <label>Description</label>
                <input type="text" formControlName="description" placeholder="Brief description..." class="form-input" />
              </div>
              <div class="form-group">
                <label>Notes</label>
                <textarea formControlName="notes" rows="3" placeholder="Trading notes..." class="form-input"></textarea>
              </div>
              <div class="modal-footer">
                <button type="button" class="btn btn-ghost" (click)="closeModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">
                  @if (saving()) { <span class="spinner"></span> }
                  {{ editingId() ? 'Update' : 'Create' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `
})
export class InstrumentsComponent implements OnInit {
  instruments = signal<Instrument[]>([]);
  loading = signal(true);
  showModal = signal(false);
  editingId = signal<string | null>(null);
  saving = signal(false);

  form;

  constructor(private api: ApiService, private fb: FormBuilder, private toast: ToastService) {
    this.form = this.fb.group({
      name: ['', Validators.required],
      symbol: [''],
      safeLotSize: [0.1, [Validators.required, Validators.min(0.01)]],
      maxLot: [1, [Validators.required, Validators.min(0.01)]],
      volatilityLevel: [2, Validators.required],
      description: [''],
      notes: ['']
    });
  }

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.api.getInstruments().subscribe({
      next: (data) => { this.instruments.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openModal(inst?: Instrument): void {
    if (inst) {
      this.editingId.set(inst.id);
      this.form.patchValue({
        name: inst.name, symbol: inst.symbol || '',
        safeLotSize: inst.safeLotSize, maxLot: inst.maxLot,
        volatilityLevel: inst.volatilityLevel === 'Low' ? 1 : inst.volatilityLevel === 'Medium' ? 2 : 3,
        description: inst.description || '', notes: inst.notes || ''
      });
    } else {
      this.editingId.set(null);
      this.form.reset({ volatilityLevel: 2, safeLotSize: 0.1, maxLot: 1 });
    }
    this.showModal.set(true);
  }

  closeModal(): void { this.showModal.set(false); }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const data = this.form.value as CreateInstrument;
    const obs = this.editingId()
      ? this.api.updateInstrument(this.editingId()!, data)
      : this.api.createInstrument(data);

    obs.subscribe({
      next: () => { 
        this.closeModal(); 
        this.load(); 
        this.saving.set(false); 
        this.toast.success(`Instrument ${this.editingId() ? 'updated' : 'created'} successfully!`, 'Success');
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err.error?.error || 'Failed to save instrument.', 'Error');
      }
    });
  }

  deleteInstrument(id: string): void {
    if (!confirm('Delete this instrument?')) return;
    this.api.deleteInstrument(id).subscribe({
      next: () => {
        this.load();
        this.toast.success('Instrument deleted successfully.', 'Deleted');
      },
      error: (err) => this.toast.error(err.error?.error || 'Failed to delete instrument.', 'Error')
    });
  }

  selectInstrument(inst: Instrument): void { /* Future: show details */ }

  getVolatilityClass(level: string): string {
    return `vol-card-${level.toLowerCase()}`;
  }
}
