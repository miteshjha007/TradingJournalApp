import { Component, Input, Output, EventEmitter, ElementRef, HostListener, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface SelectOption {
  value: string;
  label: string;
  subLabel?: string;
}

@Component({
  selector: 'app-searchable-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="searchable-select-container" [class.open]="isOpen()" [class.sm]="isSmall">
      <div class="select-trigger" (click)="toggleOpen()">
        <span class="selected-text" [class.placeholder]="!selectedOption() && (!showAllOption || value !== '')">
          {{ displayText() }}
        </span>
        <span class="arrow-icon">▼</span>
      </div>

      @if (isOpen()) {
        <div class="select-dropdown" (click)="$event.stopPropagation()">
          <div class="search-box">
            <input 
              type="text" 
              [placeholder]="searchPlaceholder" 
              [ngModel]="searchQuery()" 
              (ngModelChange)="searchQuery.set($event)"
              class="search-input"
              #searchInput
            />
          </div>
          <div class="options-list">
            @if (showAllOption) {
              <div 
                class="option-item" 
                [class.selected]="value === ''"
                (click)="selectOption('', allOptionLabel)"
              >
                <span>{{ allOptionLabel }}</span>
              </div>
            }
            @for (opt of filteredOptions(); track opt.value) {
              <div 
                class="option-item" 
                [class.selected]="value === opt.value"
                (click)="selectOption(opt.value, opt.label)"
              >
                <span class="opt-label">{{ opt.label }}</span>
                @if (opt.subLabel) {
                  <span class="opt-sub">{{ opt.subLabel }}</span>
                }
              </div>
            } @empty {
              <div class="no-options">No matching options</div>
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .searchable-select-container {
      position: relative;
      display: inline-block;
      width: 100%;
    }

    .select-trigger {
      display: flex;
      align-items: center;
      justify-content: space-between;
      width: 100%;
      padding: 0.75rem 1rem;
      border-radius: var(--border-radius-sm, 6px);
      border: 1px solid var(--border-color, rgba(255, 255, 255, 0.15));
      background-color: var(--input-bg, #0c101d);
      color: var(--text-main, #f4f4f5);
      font-family: inherit;
      font-size: 0.95rem;
      box-sizing: border-box;
      cursor: pointer;
      user-select: none;
      min-height: 42px;
      transition: var(--transition, all 0.2s ease);
    }

    .select-trigger:hover {
      border-color: var(--primary, #c084fc);
    }

    .searchable-select-container.open .select-trigger {
      border-color: var(--primary, #c084fc);
      box-shadow: 0 0 0 3px var(--primary-light, rgba(168, 85, 247, 0.25));
    }

    /* Small variant for filter bar */
    .searchable-select-container.sm .select-trigger {
      padding: 0.4rem 0.75rem;
      font-size: 0.85rem;
      min-height: 32px;
      height: 32px;
    }

    .selected-text {
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .selected-text.placeholder {
      color: var(--text-muted, #a1a1aa);
    }

    .arrow-icon {
      font-size: 0.65rem;
      color: var(--text-muted, #a1a1aa);
      margin-left: 0.5rem;
      transition: transform 0.2s ease;
      flex-shrink: 0;
    }

    .searchable-select-container.open .arrow-icon {
      transform: rotate(180deg);
      color: var(--primary, #c084fc);
    }

    .select-dropdown {
      position: absolute;
      top: calc(100% + 6px);
      left: 0;
      right: 0;
      min-width: 220px;
      background: var(--bg-card, #18181b);
      border: 1px solid rgba(168, 85, 247, 0.4);
      box-shadow: 0 12px 30px -5px rgba(0, 0, 0, 0.9), 0 0 15px rgba(168, 85, 247, 0.15);
      border-radius: var(--border-radius-sm, 8px);
      z-index: 99999;
      padding: 0.5rem;
      box-sizing: border-box;
    }

    .search-box {
      padding-bottom: 0.4rem;
      border-bottom: 1px solid rgba(255, 255, 255, 0.1);
      margin-bottom: 0.35rem;
    }

    .search-input {
      width: 100%;
      background: var(--input-bg, #09090b);
      border: 1px solid rgba(255, 255, 255, 0.18);
      color: var(--text-main, #f4f4f5);
      padding: 0.4rem 0.65rem;
      font-size: 0.85rem;
      border-radius: 5px;
      box-sizing: border-box;
      outline: none;
      transition: border-color 0.2s ease;
    }

    .search-input:focus {
      border-color: var(--primary, #c084fc);
      box-shadow: 0 0 8px rgba(168, 85, 247, 0.3);
    }

    .options-list {
      max-height: 220px;
      overflow-y: auto;
      display: flex;
      flex-direction: column;
      gap: 0.15rem;
      padding-right: 0.2rem;
    }

    .options-list::-webkit-scrollbar {
      width: 5px;
    }

    .options-list::-webkit-scrollbar-track {
      background: rgba(255, 255, 255, 0.05);
      border-radius: 3px;
    }

    .options-list::-webkit-scrollbar-thumb {
      background: rgba(168, 85, 247, 0.5);
      border-radius: 3px;
    }

    .options-list::-webkit-scrollbar-thumb:hover {
      background: rgba(168, 85, 247, 0.8);
    }

    .option-item {
      padding: 0.5rem 0.65rem;
      font-size: 0.85rem;
      border-radius: 5px;
      cursor: pointer;
      color: #e4e4e7;
      display: flex;
      justify-content: space-between;
      align-items: center;
      transition: background 0.15s ease, color 0.15s ease;
    }

    .option-item:hover {
      background: rgba(168, 85, 247, 0.25);
      color: #f4f4f5;
    }

    .option-item.selected {
      background: rgba(168, 85, 247, 0.3);
      color: var(--primary, #c084fc);
      font-weight: 600;
    }

    .opt-label {
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .opt-sub {
      font-size: 0.75rem;
      color: var(--text-muted, #a1a1aa);
      margin-left: 0.5rem;
      background: rgba(255, 255, 255, 0.06);
      padding: 0.1rem 0.35rem;
      border-radius: 4px;
    }

    .no-options {
      padding: 0.75rem;
      font-size: 0.8rem;
      color: var(--text-muted, #a1a1aa);
      text-align: center;
    }
  `]
})
export class SearchableSelectComponent {
  private _value = signal<string>('');
  private _options = signal<SelectOption[]>([]);

  @Input() set value(v: string) {
    this._value.set(v || '');
  }
  get value(): string {
    return this._value();
  }

  @Input() set options(opts: SelectOption[]) {
    this._options.set(opts || []);
  }
  get options(): SelectOption[] {
    return this._options();
  }

  @Input() placeholder: string = 'Select...';
  @Input() searchPlaceholder: string = '🔍 Search instruments...';
  @Input() showAllOption: boolean = false;
  @Input() allOptionLabel: string = 'All';
  @Input() isSmall: boolean = false;

  @Output() valueChange = new EventEmitter<string>();

  isOpen = signal(false);
  searchQuery = signal('');

  constructor(private elementRef: ElementRef) {}

  filteredOptions = computed(() => {
    const opts = this._options();
    const query = this.searchQuery().toLowerCase().trim();
    if (!query) return opts;
    return opts.filter(opt => 
      opt.label.toLowerCase().includes(query) || 
      (opt.subLabel && opt.subLabel.toLowerCase().includes(query)) ||
      opt.value.toLowerCase().includes(query)
    );
  });

  selectedOption = computed(() => {
    const val = this._value();
    const opts = this._options();
    return opts.find(opt => opt.value === val);
  });

  displayText = computed(() => {
    const sel = this.selectedOption();
    if (sel) return sel.label;
    if (this.showAllOption && (!this._value() || this._value() === '')) return this.allOptionLabel;
    return this.placeholder;
  });

  toggleOpen(): void {
    const nextState = !this.isOpen();
    this.isOpen.set(nextState);
    if (!nextState) {
      this.searchQuery.set('');
    }
  }

  selectOption(val: string, label: string): void {
    this._value.set(val);
    this.valueChange.emit(val);
    this.isOpen.set(false);
    this.searchQuery.set('');
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
      this.searchQuery.set('');
    }
  }
}
