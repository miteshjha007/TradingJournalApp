import { Component, OnInit, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ThemeService } from '../../services/theme.service';
import { ToastService } from '../../services/toast.service';
import { ChatService } from '../../services/chat.service';
import { ApiService } from '../../services/api.service';
import { DirectMessage, Streak } from '../../models/models';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterOutlet],
  template: `
    <div class="app-shell" [class.sidebar-collapsed]="sidebarCollapsed()">

      <!-- Sidebar -->
      <aside class="sidebar">
        <div class="sidebar-header">
          <div class="brand">
            <span class="brand-icon">📈</span>
            @if (!sidebarCollapsed()) { <span class="brand-name">TradeJournal</span> }
          </div>
          <button class="collapse-btn" (click)="sidebarCollapsed.set(!sidebarCollapsed())">
            {{ sidebarCollapsed() ? '→' : '←' }}
          </button>
        </div>

        <nav class="sidebar-nav">
          @for (item of visibleNavItems; track item.path) {
            <a [routerLink]="item.path" routerLinkActive="active" class="nav-item nav-item-rel" [title]="item.label">
              <span class="nav-icon">{{ item.icon }}</span>
              @if (!sidebarCollapsed()) { <span class="nav-label">{{ item.label }}</span> }
              @if (item.label === 'Forum' && chatService.globalUnreadCount() > 0) {
                <span class="nav-badge">{{ chatService.globalUnreadCount() }}</span>
              }
            </a>
          }
          @if (authService.isAdmin()) {
            <a routerLink="/admin" routerLinkActive="active" class="nav-item admin" title="Admin">
              <span class="nav-icon">🛡️</span>
              @if (!sidebarCollapsed()) { <span class="nav-label">Admin</span> }
            </a>
          }
        </nav>

        <div class="sidebar-footer">
          @if (!sidebarCollapsed()) {
            <div class="user-info">
              <div class="user-avatar">{{ initials() }}</div>
              <div class="user-details">
                <span class="user-name">{{ authService.currentUser()?.firstName }}</span>
                <span class="user-role">{{ authService.currentUser()?.role }}</span>
              </div>
            </div>
          }
          <button class="logout-btn" (click)="onLogout()" [title]="'Logout'">
            <span>🚪</span>
            @if (!sidebarCollapsed()) { <span>Logout</span> }
          </button>
        </div>
      </aside>

      <!-- Main Content -->
      <div class="main-content">
        <header class="topbar">
          <div class="topbar-left">
            <h2 class="page-title">{{ currentPageTitle() }}</h2>
          </div>
          <div class="topbar-right">
            <!-- Loss Alert Banner -->
            @if (showLossAlert()) {
              <div class="loss-alert-banner" (click)="showLossAlert.set(false)">
                ⚠️ Daily loss limit reached! Trading paused.
              </div>
            }

            <!-- Streak Badge -->
            @if (streak() && streak()!.currentStreak > 0) {
              <div class="streak-badge" [title]="'Discipline: ' + streak()!.disciplineGrade + ' (' + streak()!.disciplineScore + '/100)'">
                🔥 {{ streak()!.currentStreak }}
                @if (!sidebarCollapsed()) { <span class="streak-label">day streak</span> }
              </div>
            }

            <!-- 🔔 Notification Bell -->
            <div class="notif-wrapper" (click)="toggleNotifPanel($event)">
              <button class="notif-btn" [class.has-unread]="chatService.globalUnreadCount() > 0" title="Notifications">
                🔔
                @if (chatService.globalUnreadCount() > 0) {
                  <span class="notif-count-badge">{{ chatService.globalUnreadCount() > 99 ? '99+' : chatService.globalUnreadCount() }}</span>
                }
              </button>

              <!-- Notification Dropdown -->
              @if (showNotifPanel()) {
                <div class="notif-panel" (click)="$event.stopPropagation()">
                  <div class="notif-panel-header">
                    <span>🔔 Notifications</span>
                    <button class="notif-clear-btn" (click)="clearNotifications()" *ngIf="recentDMs().length > 0">Mark all read</button>
                  </div>

                  @if (recentDMs().length === 0) {
                    <div class="notif-empty">
                      <span>✅ You're all caught up!</span>
                    </div>
                  }

                  @for (dm of recentDMs(); track dm.id) {
                    <div class="notif-item" (click)="openDM(dm)">
                      <div class="notif-avatar">{{ dm.senderInitials }}</div>
                      <div class="notif-content">
                        <div class="notif-sender">{{ dm.senderName }}</div>
                        <div class="notif-msg">{{ dm.content | slice:0:55 }}{{ dm.content.length > 55 ? '...' : '' }}</div>
                        <div class="notif-time">{{ dm.createdAt | date:'shortTime' }}</div>
                      </div>
                      <div class="notif-unread-dot"></div>
                    </div>
                  }

                  <a routerLink="/forum" (click)="showNotifPanel.set(false)" class="notif-footer-link">
                    Open Forum →
                  </a>
                </div>
              }
            </div>

            <!-- Theme Toggle -->
            <button class="theme-toggle" (click)="themeService.toggle()" [title]="themeService.isDark() ? 'Switch to Light' : 'Switch to Dark'">
              {{ themeService.isDark() ? '☀️' : '🌙' }}
              <span class="theme-label">{{ themeService.isDark() ? 'Light' : 'Dark' }}</span>
            </button>
            <div class="user-badge">
              <span class="user-avatar-sm">{{ initials() }}</span>
              <span>{{ authService.currentUser()?.firstName }}</span>
            </div>
          </div>
        </header>

        <main class="page-content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [`
    .streak-badge {
      display: flex;
      align-items: center;
      gap: 0.3rem;
      background: linear-gradient(135deg, #f97316, #ef4444);
      color: white;
      padding: 0.25rem 0.75rem;
      border-radius: 9999px;
      font-size: 0.85rem;
      font-weight: 700;
      cursor: default;
    }
    .streak-label { font-size: 0.75rem; font-weight: 500; }

    .nav-item-rel { position: relative; }

    .nav-badge {
      position: absolute;
      top: 6px;
      right: 8px;
      min-width: 18px;
      height: 18px;
      padding: 0 4px;
      background: var(--danger);
      color: white;
      border-radius: 9999px;
      font-size: 0.68rem;
      font-weight: 700;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    /* Notification Bell */
    .notif-wrapper {
      position: relative;
    }

    .notif-btn {
      position: relative;
      background: var(--bg-hover);
      border: 1px solid var(--border-color);
      border-radius: 50%;
      width: 40px;
      height: 40px;
      font-size: 1.1rem;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: var(--transition);
      color: var(--text-main);
    }

    .notif-btn:hover,
    .notif-btn.has-unread {
      background: var(--primary-light);
      border-color: var(--primary);
      animation: ring 1.5s ease-in-out infinite;
    }

    @keyframes ring {
      0%, 100% { transform: rotate(0); }
      10%, 30% { transform: rotate(-8deg); }
      20%, 40% { transform: rotate(8deg); }
      50% { transform: rotate(0); }
    }

    .notif-count-badge {
      position: absolute;
      top: -4px;
      right: -4px;
      min-width: 18px;
      height: 18px;
      padding: 0 4px;
      background: var(--danger);
      color: white;
      border-radius: 9999px;
      font-size: 0.68rem;
      font-weight: 700;
      display: flex;
      align-items: center;
      justify-content: center;
      border: 2px solid var(--bg-card);
    }

    /* Notification Panel Dropdown */
    .notif-panel {
      position: absolute;
      top: calc(100% + 12px);
      right: 0;
      width: 340px;
      background: var(--bg-card);
      border: 1px solid var(--border-color);
      border-radius: var(--border-radius);
      box-shadow: var(--shadow-lg);
      z-index: 999;
      overflow: hidden;
      animation: slideDown 0.2s ease-out;
    }

    .notif-panel-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 1.25rem;
      border-bottom: 1px solid var(--border-color);
      font-weight: 600;
      font-size: 0.9rem;
    }

    .notif-clear-btn {
      background: none;
      border: none;
      color: var(--primary);
      font-size: 0.8rem;
      cursor: pointer;
      font-family: var(--font-family);
    }

    .notif-clear-btn:hover { text-decoration: underline; }

    .notif-empty {
      padding: 2rem;
      text-align: center;
      color: var(--text-muted);
      font-size: 0.9rem;
    }

    .notif-item {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 0.875rem 1.25rem;
      cursor: pointer;
      transition: var(--transition);
      border-bottom: 1px solid var(--border-color);
    }

    .notif-item:hover { background-color: var(--bg-hover); }
    .notif-item:last-of-type { border-bottom: none; }

    .notif-avatar {
      width: 36px;
      height: 36px;
      min-width: 36px;
      border-radius: 50%;
      background: linear-gradient(135deg, var(--primary), #a855f7);
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 700;
      font-size: 0.85rem;
    }

    .notif-content { flex: 1; min-width: 0; }

    .notif-sender {
      font-weight: 600;
      font-size: 0.875rem;
      color: var(--text-main);
    }

    .notif-msg {
      font-size: 0.8rem;
      color: var(--text-muted);
      margin-top: 0.15rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .notif-time {
      font-size: 0.72rem;
      color: var(--text-muted);
      margin-top: 0.2rem;
    }

    .notif-unread-dot {
      width: 8px;
      height: 8px;
      min-width: 8px;
      border-radius: 50%;
      background: var(--primary);
      margin-top: 6px;
    }

    .notif-footer-link {
      display: block;
      text-align: center;
      padding: 0.75rem;
      font-size: 0.85rem;
      font-weight: 500;
      color: var(--primary);
      border-top: 1px solid var(--border-color);
      background-color: var(--bg-hover);
    }

    .notif-footer-link:hover { background-color: var(--primary-light); }
  `]
})
export class ShellComponent implements OnInit {
  sidebarCollapsed = signal(false);
  showLossAlert = signal(false);
  showNotifPanel = signal(false);
  recentDMs = signal<DirectMessage[]>([]);
  streak = signal<Streak | null>(null);

  navItems = [
    { path: '/dashboard', label: 'Dashboard', icon: '📊' },
    { path: '/instruments', label: 'Instruments', icon: '🎯' },
    { path: '/trades', label: 'Trade Journal', icon: '📝' },
    { path: '/analytics', label: 'Analytics', icon: '🤖' },
    { path: '/calendar', label: 'Calendar', icon: '📅' },
    { path: '/notes', label: 'Notes', icon: '📌' },
    { path: '/risk-tool', label: 'Risk Tool', icon: '⚖️' },
    { path: '/accounts', label: 'Accounts', icon: '🏦' },
    { path: '/alerts', label: 'Alerts', icon: '🔔' },
    { path: '/playbook', label: 'Playbook', icon: '📋' },
    { path: '/ai-chat', label: 'AI Chat', icon: '🤖' },
    { path: '/backtest', label: 'Backtest', icon: '🔬' },
    { path: '/forum', label: 'Forum', icon: '💬' },
  ];

  constructor(
    public authService: AuthService,
    public themeService: ThemeService,
    private toast: ToastService,
    public chatService: ChatService,
    private apiService: ApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const token = this.authService.getToken();
    if (token) {
      this.chatService.startConnection(token);
      this.loadUnreadCount();
    }
    this.chatService.onNewDirectMessage((dm) => {
      this.recentDMs.update(list => [dm, ...list].slice(0, 10));
      this.toast.info(`New message from ${dm.senderName}`, '💌 Direct Message');
    });
    this.loadStreak();
  }

  loadStreak() {
    this.apiService.getStreak().subscribe({
      next: (s) => this.streak.set(s),
      error: () => {}
    });
  }

  // Close notification panel when clicking outside
  @HostListener('document:click')
  onDocumentClick() {
    if (this.showNotifPanel()) {
      this.showNotifPanel.set(false);
    }
  }

  toggleNotifPanel(event: Event) {
    event.stopPropagation();
    const opening = !this.showNotifPanel();
    this.showNotifPanel.set(opening);
  }

  loadUnreadCount() {
    this.apiService.getUnreadCount().subscribe({
      next: (res) => {
        this.chatService.globalUnreadCount.set(res.unreadCount);
      }
    });
  }

  clearNotifications() {
    this.chatService.globalUnreadCount.set(0);
    this.recentDMs.set([]);
    this.showNotifPanel.set(false);
  }

  openDM(dm: DirectMessage) {
    this.showNotifPanel.set(false);
    this.router.navigate(['/forum']);
  }

  get visibleNavItems() {
    const user = this.authService.currentUser();
    if (!user) return [];
    // Admins see everything
    if (user.role === 'Admin') return this.navItems;
    // Regular users: Dashboard, Accounts, and Forum always visible; rest by allowedSections
    const allowed = user.allowedSections ?? [];
    const alwaysVisible = ['Dashboard', 'Accounts', 'Forum', 'Playbook', 'AI Chat', 'Backtest', 'Trade Journal', 'Instruments', 'Analytics'];
    return this.navItems.filter(item =>
      alwaysVisible.includes(item.label) || allowed.includes(item.label)
    );
  }

  onLogout(): void {
    this.chatService.stopConnection();
    this.authService.logout();
    this.toast.info('You have been logged out.', 'Goodbye!');
  }

  initials(): string {
    const user = this.authService.currentUser();
    if (!user) return 'U';
    return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
  }

  currentPageTitle(): string {
    const path = window.location.pathname.split('/').pop() || '';
    const item = this.navItems.find(i => i.path.includes(path));
    return item?.label || 'Dashboard';
  }
}
