import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="auth-page">
      <div class="auth-container">
        <div class="auth-brand">
          <div class="brand-icon">📈</div>
          <h1 class="brand-name">TradingJournal</h1>
          <p class="brand-tagline">Master your trading discipline</p>
        </div>
        <div class="auth-card">
          <h2>Welcome back</h2>
          <p class="auth-subtitle">Sign in to your trading journal</p>

          <form [formGroup]="form" (ngSubmit)="onSubmit()">
            <div class="form-group">
              <label>Email Address</label>
              <input type="email" formControlName="email" placeholder="trader@example.com" class="form-input" />
            </div>
            <div class="form-group" style="position: relative;">
              <label>Password</label>
              <input [type]="showPassword() ? 'text' : 'password'" formControlName="password" placeholder="••••••••" class="form-input" style="padding-right: 2.5rem;" />
              <button type="button" class="password-toggle" (click)="togglePassword()" style="position: absolute; right: 0.5rem; top: 2rem; background: none; border: none; cursor: pointer; color: var(--text-muted); font-size: 1.1rem; padding: 0;">
                @if(showPassword()) { <span>👁️‍🗨️</span> } @else { <span>👁️</span> }
              </button>
            </div>
            <button type="submit" class="btn btn-primary btn-full" [disabled]="loading()">
              @if (loading()) { <span class="spinner"></span> } Sign In
            </button>
          </form>

          <div class="auth-footer">
            <p>Don't have an account? <a routerLink="/auth/register">Create one</a></p>
          </div>

          <div class="demo-creds">
            <p><strong>Demo:</strong> trader&#64;tradingjournal.com / Trader&#64;123</p>
          </div>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent {
  form;
  loading = signal(false);
  showPassword = signal(false);

  constructor(
    private fb: FormBuilder, 
    private authService: AuthService, 
    private router: Router,
    private toast: ToastService
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  togglePassword(): void {
    this.showPassword.set(!this.showPassword());
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);

    this.authService.login(this.form.value as any).subscribe({
      next: () => {
        this.toast.success('Successfully signed in!', 'Welcome Back');
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.toast.error(err.error?.error || 'Login failed. Please try again.', 'Authentication Error');
        this.loading.set(false);
      }
    });
  }
}

