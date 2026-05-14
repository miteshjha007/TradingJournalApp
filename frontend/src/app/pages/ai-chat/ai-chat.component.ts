import { Component, OnInit, signal, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { AuthService } from '../../services/auth.service';
import { UserAiSettings, SaveAiSettings, AiChatSession, AiChatMessage, AiProvider } from '../../models/models';
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
        </div>

        <div class="chat-input-area">
          <textarea
            [(ngModel)]="inputText"
            placeholder="Ask about your trading performance..."
            class="chat-input"
            rows="2"
            (keydown.enter)="onEnter($event)"
          ></textarea>
          <button class="send-btn" (click)="send()" [disabled]="!inputText.trim() || streaming()">
            {{ streaming() ? '⏳' : '➤' }}
          </button>
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

  providerName(p: AiProvider): string {
    return { 1: 'OpenAI', 2: 'Claude', 3: 'Gemini', 4: 'DeepSeek', 5: 'Custom' }[p] ?? 'AI';
  }

  formatMessage(content: string): string {
    return content.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>').replace(/\n/g, '<br>');
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
    const body = JSON.stringify({ message: text, sessionId });

    fetch(`${environment.apiUrl}/ai/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
      body
    }).then(res => {
      const reader = res.body!.getReader();
      const decoder = new TextDecoder();
      let accumulated = '';

      const read = () => {
        reader.read().then(({ done, value }) => {
          if (done) {
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
              if (data.startsWith('[ERROR]')) { this.toast.error(data.replace('[ERROR] ', '')); this.streaming.set(false); return; }
              accumulated += data;
              this.streamingText.set(accumulated);
              this.shouldScroll = true;
            }
          });
          read();
        });
      };
      read();
    }).catch(() => { this.toast.error('Connection failed'); this.streaming.set(false); });
  }
}
