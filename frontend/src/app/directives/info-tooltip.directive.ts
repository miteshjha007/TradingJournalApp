import { Directive, ElementRef, Input, OnDestroy, OnInit, Renderer2, inject } from '@angular/core';
import { TRADING_TERMS } from '../data/trading-terms.data';

let tooltipStylesInjected = false;

function injectTooltipStyles(): void {
  if (tooltipStylesInjected) return;
  tooltipStylesInjected = true;
  const style = document.createElement('style');
  style.id = 'tj-tooltip-styles';
  style.textContent = `
    .tj-tooltip-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 15px;
      height: 15px;
      border-radius: 50%;
      background: var(--accent-primary, #89b4fa);
      color: var(--bg-secondary, #1e1e2e);
      font-size: 9px;
      font-weight: 700;
      font-style: normal;
      margin-left: 4px;
      cursor: help;
      vertical-align: middle;
      flex-shrink: 0;
      line-height: 1;
      user-select: none;
      transition: opacity 0.15s ease;
      font-family: sans-serif;
    }
    .tj-tooltip-icon:hover { opacity: 0.8; }

    .tj-tooltip-card {
      position: fixed;
      z-index: 99999;
      background: var(--bg-secondary, #1e1e2e);
      border: 1px solid var(--border-color, #313244);
      border-radius: 10px;
      padding: 12px 14px;
      width: 280px;
      box-shadow: 0 8px 24px rgba(0,0,0,0.35);
      pointer-events: none;
      animation: tj-fade-in 0.15s ease;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    }
    .tj-tooltip-term {
      font-weight: 600;
      font-size: 13px;
      color: var(--accent-primary, #89b4fa);
      margin-bottom: 6px;
    }
    .tj-tooltip-def {
      font-size: 12px;
      color: var(--text-primary, #cdd6f4);
      line-height: 1.5;
      margin-bottom: 8px;
    }
    .tj-tooltip-row {
      display: flex;
      justify-content: space-between;
      gap: 8px;
      font-size: 11px;
      margin-bottom: 3px;
    }
    .tj-tooltip-label {
      color: var(--text-secondary, #a6adc8);
      flex-shrink: 0;
    }
    .tj-tooltip-value {
      color: var(--text-primary, #cdd6f4);
      font-weight: 500;
      text-align: right;
    }
    .tj-tooltip-tip {
      font-size: 11px;
      color: var(--accent-success, #a6e3a1);
      margin-top: 8px;
      border-top: 1px solid var(--border-color, #313244);
      padding-top: 6px;
      line-height: 1.4;
    }
    @keyframes tj-fade-in {
      from { opacity: 0; transform: translateY(4px); }
      to   { opacity: 1; transform: translateY(0); }
    }
  `;
  document.head.appendChild(style);
}

@Directive({
  selector: '[infoTooltip]',
  standalone: true
})
export class InfoTooltipDirective implements OnInit, OnDestroy {
  @Input('infoTooltip') termKey: string = '';

  private el = inject(ElementRef);
  private renderer = inject(Renderer2);

  private iconEl: HTMLElement | null = null;
  private tooltipEl: HTMLElement | null = null;

  // Bound event listeners for cleanup
  private enterListener: (() => void) | null = null;
  private leaveListener: (() => void) | null = null;
  private touchStartListener: (() => void) | null = null;
  private documentTouchListener: ((e: TouchEvent) => void) | null = null;

  ngOnInit(): void {
    injectTooltipStyles();
    this.createIcon();
  }

  ngOnDestroy(): void {
    this.removeTooltip();
    if (this.iconEl) {
      this.enterListener?.();
      this.leaveListener?.();
      this.touchStartListener?.();
      this.iconEl.remove();
      this.iconEl = null;
    }
    if (this.documentTouchListener) {
      document.removeEventListener('touchstart', this.documentTouchListener);
      this.documentTouchListener = null;
    }
  }

  private createIcon(): void {
    const icon = this.renderer.createElement('span') as HTMLElement;
    icon.className = 'tj-tooltip-icon';
    icon.textContent = 'ⓘ';
    icon.setAttribute('role', 'button');
    icon.setAttribute('aria-label', `Info: ${this.termKey}`);

    // Mouse events
    this.enterListener = this.renderer.listen(icon, 'mouseenter', (e: MouseEvent) => {
      this.showTooltip(e.clientX, e.clientY);
    });
    this.leaveListener = this.renderer.listen(icon, 'mouseleave', () => {
      this.removeTooltip();
    });

    // Touch/tap support for mobile
    this.touchStartListener = this.renderer.listen(icon, 'touchstart', (e: TouchEvent) => {
      e.preventDefault(); // prevent ghost click
      e.stopPropagation();
      const touch = e.touches[0];
      if (this.tooltipEl) {
        this.removeTooltip();
      } else {
        this.showTooltip(touch.clientX, touch.clientY);
        // Dismiss on next touch outside
        const handler = (ev: TouchEvent) => {
          if (!this.tooltipEl?.contains(ev.target as Node) && ev.target !== icon) {
            this.removeTooltip();
          }
          document.removeEventListener('touchstart', handler);
          this.documentTouchListener = null;
        };
        this.documentTouchListener = handler;
        setTimeout(() => document.addEventListener('touchstart', handler), 50);
      }
    });

    this.iconEl = icon;
    // Append icon after the host element
    const host = this.el.nativeElement as HTMLElement;
    host.style.display = host.style.display || 'inline-flex';
    host.style.alignItems = host.style.alignItems || 'center';
    host.appendChild(icon);
  }

  private showTooltip(x: number, y: number): void {
    this.removeTooltip();
    const info = TRADING_TERMS[this.termKey];

    const card = document.createElement('div');
    card.className = 'tj-tooltip-card';

    const termEl = document.createElement('div');
    termEl.className = 'tj-tooltip-term';
    termEl.textContent = info ? info.term : this.termKey;
    card.appendChild(termEl);

    const defEl = document.createElement('div');
    defEl.className = 'tj-tooltip-def';
    defEl.textContent = info ? info.definition : 'Term definition coming soon.';
    card.appendChild(defEl);

    if (info?.formula) {
      const row = document.createElement('div');
      row.className = 'tj-tooltip-row';
      row.innerHTML = `<span class="tj-tooltip-label">Formula</span><span class="tj-tooltip-value">${info.formula}</span>`;
      card.appendChild(row);
    }

    if (info?.goodValue) {
      const row = document.createElement('div');
      row.className = 'tj-tooltip-row';
      row.innerHTML = `<span class="tj-tooltip-label">Good value</span><span class="tj-tooltip-value">${info.goodValue}</span>`;
      card.appendChild(row);
    }

    if (info?.tip) {
      const tipEl = document.createElement('div');
      tipEl.className = 'tj-tooltip-tip';
      tipEl.textContent = `💡 ${info.tip}`;
      card.appendChild(tipEl);
    }

    document.body.appendChild(card);
    this.tooltipEl = card;

    // Position tooltip — above icon, or below if near top
    const cardWidth = 280;
    const cardHeight = card.offsetHeight || 180;
    const margin = 10;
    const viewH = window.innerHeight;
    const viewW = window.innerWidth;

    let left = x - cardWidth / 2;
    let top = y - cardHeight - margin;

    // Clamp horizontally
    if (left + cardWidth > viewW - margin) left = viewW - cardWidth - margin;
    if (left < margin) left = margin;

    // Flip below if not enough space above
    if (top < margin) top = y + margin;

    card.style.left = `${left}px`;
    card.style.top = `${top}px`;
  }

  private removeTooltip(): void {
    if (this.tooltipEl) {
      this.tooltipEl.remove();
      this.tooltipEl = null;
    }
  }
}
