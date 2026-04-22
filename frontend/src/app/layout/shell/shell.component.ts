import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ThemeService } from '../../services/theme.service';
import { ToastService } from '../../services/toast.service';

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
            <a [routerLink]="item.path" routerLinkActive="active" class="nav-item" [title]="item.label">
              <span class="nav-icon">{{ item.icon }}</span>
              @if (!sidebarCollapsed()) { <span class="nav-label">{{ item.label }}</span> }
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
  `
})
export class ShellComponent {
  sidebarCollapsed = signal(false);
  showLossAlert = signal(false);

  navItems = [
    { path: '/dashboard', label: 'Dashboard', icon: '📊' },
    { path: '/instruments', label: 'Instruments', icon: '🎯' },
    { path: '/trades', label: 'Trade Journal', icon: '📝' },
    { path: '/analytics', label: 'Analytics', icon: '🤖' },
    { path: '/calendar', label: 'Calendar', icon: '📅' },
    { path: '/notes', label: 'Notes', icon: '📌' },
    { path: '/risk-tool', label: 'Risk Tool', icon: '⚖️' },
    { path: '/alerts', label: 'Alerts', icon: '🔔' },
  ];

  constructor(
    public authService: AuthService, 
    public themeService: ThemeService,
    private toast: ToastService
  ) {}

  get visibleNavItems() {
    const user = this.authService.currentUser();
    if (!user) return [];
    // Admins see everything
    if (user.role === 'Admin') return this.navItems;
    // Regular users: filter by allowedSections
    const allowed = user.allowedSections ?? [];
    if (allowed.length === 0) return [];
    return this.navItems.filter(item => allowed.includes(item.label));
  }

  onLogout(): void {
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
