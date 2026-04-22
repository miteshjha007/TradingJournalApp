import { Routes } from '@angular/router';
import { authGuard, guestGuard, adminGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  {
    path: 'auth',
    canActivate: [guestGuard],
    loadChildren: () => import('./pages/auth/auth.routes').then(m => m.authRoutes)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell.component').then(m => m.ShellComponent),
    children: [
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'instruments', loadComponent: () => import('./pages/instruments/instruments.component').then(m => m.InstrumentsComponent) },
      { path: 'trades', loadComponent: () => import('./pages/trades/trades.component').then(m => m.TradesComponent) },
      { path: 'notes', loadComponent: () => import('./pages/notes/notes.component').then(m => m.NotesComponent) },
      { path: 'analytics', loadComponent: () => import('./pages/analytics/analytics.component').then(m => m.AnalyticsComponent) },
      { path: 'calendar', loadComponent: () => import('./pages/calendar/calendar.component').then(m => m.CalendarComponent) },
      { path: 'risk-tool', loadComponent: () => import('./pages/risk-tool/risk-tool.component').then(m => m.RiskToolComponent) },
      { path: 'alerts', loadComponent: () => import('./pages/alerts/alerts.component').then(m => m.AlertsComponent) },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadComponent: () => import('./pages/admin/admin.component').then(m => m.AdminComponent)
      }
    ]
  },
  { path: '**', redirectTo: '/dashboard' }
];
