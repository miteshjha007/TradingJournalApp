import { Component, OnInit, signal, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { AuthService } from '../../services/auth.service';
import { UserAiSettings, SaveAiSettings, AiChatSession, AiChatMessage, AiProvider, StrategyQuery, StrategyAnalysisResult } from '../../models/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-ai-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="ai-chat-page">
      <!-- Left Panel: Sessions + Settings -->
      <aside class="sessions-panel">
        <div class="panel-header">
          <h3>AI Coach</h3>
          <button class="btn-new" (click)="newChat()" title="New Chat">+</button>
        </div>

        <button class="settings-toggle" (click)="showSettings.set(!showSettings())">
          ⚙️ AI Settings
        </button>

        @if (showSettings()) {
          <div class="settings-box">
            <div class="form-group">
              <label>Provider</label>
              <select [(ngModel)]="settingsForm.provider" class="form-control-sm">
                <option [ngValue]="1">OpenAI</option>
                <option [ngValue]="2">Anthropic (Claude)</option>
                <option [ngValue]="3">Google Gemini</option>
                <option [ngValue]="4">DeepSeek</option>
                <option [ngValue]="5">Custom</option>
              </select>
            </div>
            <div class="form-group">
              <label>API Key</label>
              <input [(ngModel)]="settingsForm.apiKey" type="password" placeholder="sk-..." class="form-control-sm" />
            </div>
            <div class="form-group">
              <label>Model (optional)</label>
              <input [(ngModel)]="settingsForm.modelName" placeholder="e.g. gpt-4o" class="form-control-sm" />
            </div>
            @if (settingsForm.provider === 5) {
              <div class="form-group">
                <label>Base URL</label>
                <input [(ngModel)]="settingsForm.customBaseUrl" placeholder="https://..." class="form-control-sm" />
              </div>
            }
            <button class="btn-save" (click)="saveSettings()" [disabled]="savingSettings()">
              {{ savingSettings() ? 'Saving...' : 'Save Settings' }}
            </button>
          </div>
        }

        <div class="sessions-list">
          <div class="sessions-label">Recent Chats</div>
          @if (sessions().length === 0) {
            <div class="no-sessions">No chat history yet</div>
          }
          @for (s of sessions(); track s.id) {
            <div class="session-item" [class.active]="currentSession()?.id === s.id" (click)="loadSession(s.id)">
              <div class="session-title">{{ s.title }}</div>
              <div class="session-meta">{{ s.messages.length }} msgs</div>
              <button class="session-del" (click)="deleteSession(s.id, $event)" title="Delete">✕</button>
            </div>
          }
        </div>
      </aside>

      <!-- Main Chat Area -->
      <div class="chat-area">
        <div class="chat-header">
          <div class="ai-indicator" [class.configured]="aiSettings()?.isConfigured">
            @if (aiSettings()?.isConfigured) {
              <span class="dot green"></span>
              <span>{{ providerName(aiSettings()!.provider) }} — {{ aiSettings()!.modelName || 'Default model' }}</span>
            } @else {
              <span class="dot red"></span>
              <span>Not configured — set API key in settings</span>
            }
          </div>
          <button class="strategy-toggle" [class.active]="strategyMode()" (click)="strategyMode.set(!strategyMode()); strategyResult.set(null)">
            🔍 {{ strategyMode() ? 'Strategy Mode ON' : 'Strategy Mode' }}
          </button>
        </div>

        <div class="messages-container" #scrollContainer>
          @if (messages().length === 0) {
            <div class="welcome">
              <div class="welcome-icon">🤖</div>
              <h3>Your AI Trading Coach</h3>
              <p>Ask me to analyze your trading patterns, suggest improvements, or explain your results.</p>
              <div class="quick-prompts">
                @for (q of quickPrompts; track q) {
                  <button class="quick-btn" (click)="sendQuick(q)">{{ q }}</button>
                }
              </div>
            </div>
          }
          @for (msg of messages(); track $index) {
            <div class="message" [class.user]="msg.role === 'user'" [class.assistant]="msg.role === 'assistant'">
              <div class="msg-avatar">{{ msg.role === 'user' ? userInitials() : '🤖' }}</div>
              <div class="msg-bubble">
                <div class="msg-content" [innerHTML]="formatMessage(msg.content)"></div>
                <div class="msg-time">{{ msg.timestamp | date:'shortTime' }}</div>
              </div>
            </div>
          }
          @if (streaming()) {
            <div class="message assistant">
              <div class="msg-avatar">🤖</div>
              <div class="msg-bubble">
                <div class="msg-content">{{ streamingText() }}<span class="cursor">▋</span></div>
              </div>
            </div>
          }

          <!-- Strategy loading indicator -->
          @if (strategyLoadingStep()) {
            <div class="message assistant">
              <div class="msg-avatar">🔍</div>
              <div class="msg-bubble">
                <div class="msg-content strategy-loading">
                  @if (strategyLoadingStep() === 'extracting') {
                    <span class="step-dot active">●</span> Extracting filters from your query...
                    <span class="step-dot">●</span> Analyzing trades
                  } @else {
                    <span class="step-dot done">✓</span> Filters extracted
                    <span class="step-dot active">●</span> Analyzing your trades...
                  }
                </div>
              </div>
            </div>
          }

          <!-- Strategy Result Card -->
          @if (strategyResult()) {
            <div class="message assistant">
              <div class="msg-avatar">🔍</div>
              <div class="msg-bubble strategy-card-bubble">
                <div class="strategy-card">
                  <div class="sc-header">🔍 Strategy Analysis</div>
                  <div class="sc-filter">{{ strategyResult()!.filters.filterSummary }}</div>

                  @if (!strategyResult()!.hasData) {
                    <div class="sc-no-data">
                      <div>📭 No trades found matching your criteria.</div>
                      <div class="sc-hint">Try: broader date range, remove the instrument filter, or rephrase your query.</div>
                    </div>
                  } @else {
                    <div class="sc-trade-count">{{ strategyResult()!.matchedTrades }} trades matched (last {{ strategyDaysBack() }} days)</div>
                    <div class="sc-stats">
                      <div class="sc-stat"><span class="sc-stat-label">Win Rate</span><span class="sc-stat-val">{{ strategyResult()!.winRate | number:'1.1-1' }}%</span></div>
                      <div class="sc-stat"><span class="sc-stat-label">Profit Factor</span><span class="sc-stat-val">{{ strategyResult()!.profitFactor | number:'1.2-2' }}</span></div>
                      <div class="sc-stat"><span class="sc-stat-label">Total P&L</span><span class="sc-stat-val" [class.pos]="strategyResult()!.totalPL>0" [class.neg]="strategyResult()!.totalPL<0">{{ strategyResult()!.totalPL | currency }}</span></div>
                      <div class="sc-stat"><span class="sc-stat-label">Avg RRR</span><span class="sc-stat-val">{{ strategyResult()!.averageRRR | number:'1.2-2' }}</span></div>
                    </div>
                    <div class="sc-meta">Sharpe: {{ strategyResult()!.sharpeRatio | number:'1.2-2' }} | Max Win: {{ strategyResult()!.maxWin | currency }} | Max Loss: {{ strategyResult()!.maxLoss | currency }}</div>

                    @if (strategyResult()!.tradePreview.length > 0) {
                      <div class="sc-preview-label">Recent Trades</div>
                      @for (t of strategyResult()!.tradePreview.slice(0,3); track t.tradeDate) {
                        <div class="sc-trade-row" [class.win]="t.result==='Win'" [class.loss]="t.result==='Loss'">
                          <span>{{ t.tradeDate | date:'MM/dd' }}</span>
                          <span>{{ t.instrumentName }}</span>
                          <span>{{ t.tradeType }}</span>
                          <span>{{ t.lotSize }}</span>
                          <span [class.pos]="t.profitLoss>0" [class.neg]="t.profitLoss<0">{{ t.profitLoss | currency }}</span>
                          <span class="sc-badge" [class.win-badge]="t.result==='Win'" [class.loss-badge]="t.result==='Loss'">{{ t.result }}</span>
                        </div>
                      }
                    }

                    <div class="sc-ai-label">AI Analysis:</div>
                    <div class="sc-ai-text">
                      @if (strategyStreamText()) { {{ strategyStreamText() }} }
                      @else if (!strategyStreamDone()) { <span class="cursor">▋</span> }
                    </div>
                  }
                </div>
              </div>
            </div>
          }
        </div>

        <div class="chat-input-area">
          @if (strategyMode()) {
            <div class="strategy-quick-prompts">
              @for (q of strategyQuickPrompts; track q) {
                <button class="quick-btn" (click)="inputText=q; analyzeStrategy()">{{ q }}</button>
              }
            </div>
            <div class="strategy-controls">
              <textarea [(ngModel)]="inputText" placeholder="Ask about your strategy... e.g. 'GOLD trades in London session last 30 days'" class="chat-input" rows="2" (keydown.enter)="$event.preventDefault(); analyzeStrategy()"></textarea>
              <div class="strategy-side">
                <select [ngModel]="strategyDaysBack()" (ngModelChange)="strategyDaysBack.set($event)" class="days-select">
                  <option [value]="7">7d</option>
                  <option [value]="14">14d</option>
                  <option [value]="30">30d</option>
                  <option [value]="60">60d</option>
                  <option [value]="90">90d</option>
                </select>
                <button class="send-btn" (click)="analyzeStrategy()" [disabled]="!inputText.trim() || !!strategyLoadingStep()">📊</button>
              </div>
            </div>
          } @else {
            <textarea [(ngModel)]="inputText" placeholder="Ask about your trading performance..." class="chat-input" rows="2" (keydown.enter)="onEnter($event)"></textarea>
            <button class="send-btn" (click)="send()" [disabled]="!inputText.trim() || streaming()">{{ streaming() ? '⏳' : '➤' }}</button>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .ai-chat-page { display: flex; height: calc(100vh - 120px); gap: 0; background: var(--bg-main); border-radius: var(--border-radius); overflow: hidden; border: 1px solid var(--border-color); }

    /* Sessions Panel */
    .sessions-panel { width: 260px; min-width: 260px; background: var(--bg-card); border-right: 1px solid var(--border-color); display: flex; flex-direction: column; overflow: hidden; }
    .panel-header { display: flex; justify-content: space-between; align-items: center; padding: 1rem 1.25rem; border-bottom: 1px solid var(--border-color); }
    .panel-header h3 { margin: 0; font-size: 1rem; }
    .btn-new { background: var(--primary); color: white; border: none; border-radius: 50%; width: 28px; height: 28px; font-size: 1.2rem; cursor: pointer; display: flex; align-items: center; justify-content: center; }

    .settings-toggle { margin: 0.75rem; padding: 0.5rem; background: var(--bg-hover); border: 1px solid var(--border-color); border-radius: var(--border-radius); font-size: 0.8rem; cursor: pointer; color: var(--text-main); width: calc(100% - 1.5rem); text-align: left; }
    .settings-toggle:hover { background: var(--primary-light); }

    .settings-box { margin: 0 0.75rem; padding: 0.75rem; background: var(--bg-main); border: 1px solid var(--border-color); border-radius: var(--border-radius); display: flex; flex-direction: column; gap: 0.6rem; }
    .form-group { display: flex; flex-direction: column; gap: 0.25rem; }
    .form-group label { font-size: 0.72rem; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.04em; }
    .form-control-sm { padding: 0.4rem 0.6rem; border: 1px solid var(--border-color); border-radius: 6px; background: var(--bg-card); color: var(--text-main); font-size: 0.8rem; width: 100%; }
    .btn-save { padding: 0.5rem; background: var(--primary); color: white; border: none; border-radius: var(--border-radius); font-size: 0.8rem; cursor: pointer; font-weight: 600; margin-top: 0.25rem; }
    .btn-save:disabled { opacity: 0.6; cursor: not-allowed; }

    .sessions-list { flex: 1; overflow-y: auto; padding: 0.5rem; }
    .sessions-label { font-size: 0.72rem; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.04em; padding: 0.5rem 0.25rem; }
    .no-sessions { font-size: 0.8rem; color: var(--text-muted); text-align: center; padding: 1rem; }
    .session-item { display: flex; align-items: center; gap: 0.5rem; padding: 0.6rem 0.75rem; border-radius: var(--border-radius); cursor: pointer; transition: var(--transition); position: relative; }
    .session-item:hover, .session-item.active { background: var(--bg-hover); }
    .session-item.active { border-left: 2px solid var(--primary); }
    .session-title { flex: 1; font-size: 0.82rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .session-meta { font-size: 0.72rem; color: var(--text-muted); }
    .session-del { background: none; border: none; color: var(--text-muted); cursor: pointer; font-size: 0.75rem; padding: 0.2rem; border-radius: 4px; }
    .session-del:hover { color: var(--danger); background: rgba(239,68,68,0.1); }

    /* Chat Area */
    .chat-area { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
    .chat-header { padding: 0.75rem 1.25rem; border-bottom: 1px solid var(--border-color); background: var(--bg-card); }
    .ai-indicator { display: flex; align-items: center; gap: 0.5rem; font-size: 0.82rem; color: var(--text-muted); }
    .dot { width: 8px; height: 8px; border-radius: 50%; }
    .dot.green { background: #10b981; }
    .dot.red { background: #ef4444; }

    .messages-container { flex: 1; overflow-y: auto; padding: 1.5rem; display: flex; flex-direction: column; gap: 1rem; }

    .welcome { text-align: center; margin: auto; max-width: 480px; }
    .welcome-icon { font-size: 3rem; margin-bottom: 0.75rem; }
    .welcome h3 { margin: 0 0 0.5rem; }
    .welcome p { color: var(--text-muted); font-size: 0.9rem; margin-bottom: 1.5rem; }
    .quick-prompts { display: flex; flex-wrap: wrap; gap: 0.5rem; justify-content: center; }
    .quick-btn { padding: 0.5rem 0.875rem; background: var(--bg-hover); border: 1px solid var(--border-color); border-radius: 9999px; font-size: 0.8rem; cursor: pointer; color: var(--text-main); transition: var(--transition); }
    .quick-btn:hover { background: var(--primary-light); border-color: var(--primary); }

    .message { display: flex; gap: 0.75rem; max-width: 80%; }
    .message.user { flex-direction: row-reverse; align-self: flex-end; }
    .message.assistant { align-self: flex-start; }
    .msg-avatar { width: 32px; height: 32px; min-width: 32px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 0.9rem; font-weight: 700; background: var(--primary); color: white; }
    .message.assistant .msg-avatar { background: var(--bg-hover); }
    .msg-bubble { max-width: calc(100% - 44px); }
    .msg-content { padding: 0.75rem 1rem; border-radius: 12px; font-size: 0.875rem; line-height: 1.6; white-space: pre-wrap; word-wrap: break-word; }
    .message.user .msg-content { background: var(--primary); color: white; border-radius: 12px 12px 2px 12px; }
    .message.assistant .msg-content { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 12px 12px 12px 2px; }
    .msg-time { font-size: 0.7rem; color: var(--text-muted); margin-top: 0.25rem; text-align: right; }
    .message.assistant .msg-time { text-align: left; }
    .cursor { animation: blink 0.8s infinite; }
    @keyframes blink { 0%, 100% { opacity: 1; } 50% { opacity: 0; } }

    .chat-input-area { display: flex; gap: 0.75rem; padding: 1rem 1.25rem; border-top: 1px solid var(--border-color); background: var(--bg-card); align-items: flex-end; }
    .chat-input { flex: 1; padding: 0.75rem; border: 1px solid var(--border-color); border-radius: var(--border-radius); background: var(--bg-input); color: var(--text-main); font-family: var(--font-family); font-size: 0.9rem; resize: none; line-height: 1.5; }
    .chat-input:focus { outline: none; border-color: var(--primary); }
    .send-btn { width: 44px; height: 44px; background: var(--primary); color: white; border: none; border-radius: 50%; font-size: 1.1rem; cursor: pointer; display: flex; align-items: center; justify-content: center; flex-shrink: 0; transition: var(--transition); }
    .send-btn:hover:not(:disabled) { opacity: 0.9; }
    .send-btn:disabled { opacity: 0.5; cursor: not-allowed; }

    /* Strategy Mode Styles */
    .strategy-toggle { margin-left: auto; padding: 0.4rem 0.8rem; border-radius: 999px; border: 1px solid var(--border-color); background: var(--bg-hover); color: var(--text-main); font-size: 0.8rem; font-weight: 600; cursor: pointer; transition: var(--transition); }
    .strategy-toggle.active { background: var(--primary); color: white; border-color: var(--primary); box-shadow: 0 0 10px rgba(99, 102, 241, 0.3); }

    .strategy-loading { display: flex; align-items: center; gap: 0.6rem; color: var(--text-muted); }
    .step-dot { font-size: 0.7rem; color: var(--text-muted); opacity: 0.4; }
    .step-dot.active { color: var(--primary); opacity: 1; animation: blink 1s infinite; }
    .step-dot.done { color: #10b981; opacity: 1; }

    .strategy-card-bubble { max-width: 90% !important; }
    .strategy-card { background: var(--bg-main); border: 1px solid var(--border-color); border-radius: 12px; overflow: hidden; display: flex; flex-direction: column; width: 100%; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }
    .sc-header { padding: 0.75rem 1rem; background: var(--bg-card); border-bottom: 1px solid var(--border-color); font-weight: 700; font-size: 0.9rem; color: var(--primary); }
    .sc-filter { padding: 0.6rem 1rem; background: var(--primary-light); color: var(--primary); font-size: 0.8rem; font-weight: 500; border-bottom: 1px solid var(--border-color); }
    .sc-no-data { padding: 2rem; text-align: center; color: var(--text-muted); }
    .sc-hint { font-size: 0.75rem; margin-top: 0.5rem; }
    .sc-trade-count { padding: 0.75rem 1rem 0; font-size: 0.75rem; color: var(--text-muted); font-weight: 600; text-transform: uppercase; }
    .sc-stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 1px; background: var(--border-color); margin: 0.75rem 1rem; border: 1px solid var(--border-color); border-radius: 8px; overflow: hidden; }
    .sc-stat { background: var(--bg-card); padding: 0.75rem; display: flex; flex-direction: column; align-items: center; gap: 0.25rem; }
    .sc-stat-label { font-size: 0.65rem; color: var(--text-muted); text-transform: uppercase; font-weight: 700; }
    .sc-stat-val { font-size: 1rem; font-weight: 700; }
    .sc-stat-val.pos { color: #10b981; }
    .sc-stat-val.neg { color: #ef4444; }
    .sc-meta { padding: 0 1rem 1rem; font-size: 0.75rem; color: var(--text-muted); text-align: center; }

    .sc-preview-label { padding: 0 1rem 0.5rem; font-size: 0.75rem; font-weight: 700; color: var(--text-muted); text-transform: uppercase; }
    .sc-trade-row { display: grid; grid-template-columns: 50px 1fr 60px 50px 80px 70px; gap: 0.5rem; padding: 0.5rem 1rem; font-size: 0.75rem; border-top: 1px solid var(--border-color); align-items: center; }
    .sc-trade-row:hover { background: var(--bg-hover); }
    .sc-badge { padding: 2px 6px; border-radius: 4px; font-size: 0.65rem; font-weight: 700; text-align: center; }
    .win-badge { background: rgba(16,185,129,0.1); color: #10b981; }
    .loss-badge { background: rgba(239,68,68,0.1); color: #ef4444; }

    .sc-ai-label { padding: 1rem 1rem 0.5rem; font-size: 0.75rem; font-weight: 700; color: var(--primary); text-transform: uppercase; border-top: 1px solid var(--border-color); }
    .sc-ai-text { padding: 0 1rem 1rem; font-size: 0.85rem; line-height: 1.5; color: var(--text-main); font-style: italic; }

    .strategy-quick-prompts { display: flex; flex-wrap: wrap; gap: 0.4rem; margin-bottom: 0.75rem; width: 100%; }
    .strategy-controls { display: flex; gap: 0.75rem; width: 100%; align-items: flex-end; }
    .strategy-side { display: flex; flex-direction: column; gap: 0.5rem; }
    .days-select { padding: 0.4rem; border-radius: 6px; border: 1px solid var(--border-color); background: var(--bg-card); color: var(--text-main); font-size: 0.75rem; cursor: pointer; }
  `]
})
export class AiChatComponent implements OnInit, AfterViewChecked {
  @ViewChild('scrollContainer') scrollContainer!: ElementRef;

  aiSettings = signal<UserAiSettings | null>(null);
  sessions = signal<AiChatSession[]>([]);
  currentSession = signal<AiChatSession | null>(null);
  messages = signal<AiChatMessage[]>([]);
  streaming = signal(false);
  streamingText = signal('');
  showSettings = signal(false);
  savingSettings = signal(false);
  inputText = '';
  strategyMode = signal(false);
  strategyDaysBack = signal(30);
  strategyLoadingStep = signal<string | null>(null); // null=idle, 'extracting'|'analyzing'
  strategyResult = signal<StrategyAnalysisResult | null>(null);
  strategyStreamText = signal('');
  strategyStreamDone = signal(false);

  strategyQuickPrompts = [
    'My London session trades last 30 days',
    'GOLD trades where RRR was above 1.5',
    'Winning trades on Monday last 60 days',
    'Trades with checklist compliance above 80%',
    'My worst performing instrument last 90 days',
    'Short trades during New York session'
  ];

  settingsForm: SaveAiSettings = { provider: AiProvider.Anthropic, apiKey: '', modelName: '' };

  quickPrompts = [
    'Analyze my recent trading performance',
    'What are my biggest weaknesses?',
    'Suggest improvements for my strategy',
    'What instruments perform best for me?',
  ];

  private shouldScroll = false;

  constructor(
    private api: ApiService,
    private toast: ToastService,
    public authService: AuthService
  ) {}

  ngOnInit() {
    this.api.getAiSettings().subscribe({ next: (s) => { this.aiSettings.set(s); this.settingsForm.provider = s.provider; this.settingsForm.modelName = s.modelName ?? ''; } });
    this.api.getAiSessions().subscribe({ next: (s) => this.sessions.set(s) });
  }

  ngAfterViewChecked() {
    if (this.shouldScroll && this.scrollContainer) {
      const el = this.scrollContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }

  userInitials(): string {
    const u = this.authService.currentUser();
    return u ? `${u.firstName[0]}${u.lastName[0]}`.toUpperCase() : 'U';
  }

  formatMessage(content: string): string {
    if (!content) return '';
    // Basic formatting: bold and line breaks
    return content
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\n/g, '<br/>');
  }

  providerName(p: AiProvider): string {
    switch (p) {
      case AiProvider.OpenAI: return 'OpenAI';
      case AiProvider.Anthropic: return 'Anthropic';
      case AiProvider.Gemini: return 'Gemini';
      case AiProvider.DeepSeek: return 'DeepSeek';
      default: return 'AI';
    }
  }

  saveSettings() {
    this.savingSettings.set(true);
    const payload: SaveAiSettings = { ...this.settingsForm, provider: Number(this.settingsForm.provider) as AiProvider };
    this.api.saveAiSettings(payload).subscribe({
      next: () => {
        this.toast.success('AI settings saved');
        this.api.getAiSettings().subscribe({ next: (s) => this.aiSettings.set(s) });
        this.savingSettings.set(false);
        this.showSettings.set(false);
      },
      error: (err) => { this.toast.error(err?.error?.message || 'Failed to save settings'); this.savingSettings.set(false); }
    });
  }

  newChat() {
    this.currentSession.set(null);
    this.messages.set([]);
  }

  loadSession(id: string) {
    this.api.getAiSession(id).subscribe({
      next: (s) => { this.currentSession.set(s); this.messages.set(s.messages); this.shouldScroll = true; }
    });
  }

  deleteSession(id: string, event: Event) {
    event.stopPropagation();
    this.api.deleteAiSession(id).subscribe({
      next: () => {
        this.sessions.update(list => list.filter(s => s.id !== id));
        if (this.currentSession()?.id === id) { this.currentSession.set(null); this.messages.set([]); }
      }
    });
  }

  sendQuick(prompt: string) {
    this.inputText = prompt;
    this.send();
  }

  onEnter(event: Event) {
    const e = event as KeyboardEvent;
    if (!e.shiftKey) { e.preventDefault(); this.send(); }
  }

  send() {
    const text = this.inputText.trim();
    if (!text || this.streaming()) return;

    const userMsg: AiChatMessage = { role: 'user', content: text, timestamp: new Date().toISOString() };
    this.messages.update(list => [...list, userMsg]);
    this.inputText = '';
    this.streaming.set(true);
    this.streamingText.set('');
    this.shouldScroll = true;

    const token = localStorage.getItem('accessToken') ?? '';
    const sessionId = this.currentSession()?.id;

    const requestBody = { message: text, sessionId };
    console.log('[AI Chat] Sending request:', { url: `${environment.apiUrl}/ai/chat`, body: requestBody });

    fetch(`${environment.apiUrl}/ai/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
      body: JSON.stringify(requestBody)
    }).then(async res => {
      if (!res.ok) {
        const errText = await res.text();
        console.error('[AI Chat] HTTP error from server:', { status: res.status, statusText: res.statusText, body: errText });
        this.toast.error(`Request failed (${res.status}): ${errText || res.statusText}`);
        this.streaming.set(false);
        return;
      }
      const reader = res.body!.getReader();
      const decoder = new TextDecoder();
      let accumulated = '';

      const read = () => {
        reader.read().then(({ done, value }) => {
          if (done) {
            console.log('[AI Chat] Stream complete. Total response length:', accumulated.length);
            const assistantMsg: AiChatMessage = { role: 'assistant', content: accumulated, timestamp: new Date().toISOString() };
            this.messages.update(list => [...list, assistantMsg]);
            this.streaming.set(false);
            this.streamingText.set('');
            this.shouldScroll = true;
            this.api.getAiSessions().subscribe({ next: (s) => this.sessions.set(s) });
            return;
          }
          const chunk = decoder.decode(value);
          chunk.split('\n').forEach(line => {
            if (line.startsWith('data: ')) {
              const data = line.slice(6);
              if (data === '[DONE]') return;
              if (data.startsWith('[ERROR]')) {
                const errMsg = data.replace('[ERROR] ', '');
                console.error('[AI Chat] Error from AI provider stream:', errMsg);
                this.toast.error(errMsg);
                this.streaming.set(false);
                return;
              }
              accumulated += data;
              this.streamingText.set(accumulated);
              this.shouldScroll = true;
            }
          });
          read();
        }).catch(streamErr => {
          console.error('[AI Chat] Stream read error:', streamErr);
          this.toast.error('Stream read failed');
          this.streaming.set(false);
        });
      };
      read();
    }).catch(fetchErr => {
      console.error('[AI Chat] Fetch/network error:', fetchErr);
      this.toast.error('Connection failed');
      this.streaming.set(false);
    });
  }
  analyzeStrategy() {
    const text = this.inputText.trim();
    if (!text || this.strategyLoadingStep()) return;

    // Add user message to chat
    const userMsg: AiChatMessage = { role: 'user', content: `🔍 Strategy: ${text}`, timestamp: new Date().toISOString() };
    this.messages.update(list => [...list, userMsg]);
    this.inputText = '';
    this.strategyResult.set(null);
    this.strategyStreamText.set('');
    this.strategyStreamDone.set(false);
    this.shouldScroll = true;

    // Step 1
    this.strategyLoadingStep.set('extracting');
    const token = localStorage.getItem('accessToken') ?? '';
    const dto: StrategyQuery = { userMessage: text, daysBack: this.strategyDaysBack() };

    this.api.analyzeStrategy(dto).subscribe({
      next: (result) => {
        // Step 2 briefly visible after HTTP returns (filter extraction is done inside analyzeStrategy on server)
        this.strategyLoadingStep.set('analyzing');
        setTimeout(() => {
          this.strategyResult.set(result);
          this.strategyLoadingStep.set(null);
          this.shouldScroll = true;

          if (!result.hasData) return;

          // Auto-stream LLM insight
          const { reader } = this.api.streamStrategyInsight(result, text, token);
          reader.then(r => {
            const decoder = new TextDecoder();
            const read = () => r.read().then(({ done, value }) => {
              if (done) { this.strategyStreamDone.set(true); return; }
              decoder.decode(value).split('\n').forEach(line => {
                if (line.startsWith('data: ')) {
                  const data = line.slice(6);
                  if (data === '[DONE]') { this.strategyStreamDone.set(true); return; }
                  if (!data.startsWith('[ERROR]')) {
                    this.strategyStreamText.update(t => t + data);
                    this.shouldScroll = true;
                  }
                }
              });
              read();
            }).catch(() => this.strategyStreamDone.set(true));
            read();
          }).catch(() => this.strategyStreamDone.set(true));
        }, 400);
      },
      error: (err) => {
        this.strategyLoadingStep.set(null);
        this.toast.error(err?.error?.error || 'Strategy analysis failed');
      }
    });
  }
}
