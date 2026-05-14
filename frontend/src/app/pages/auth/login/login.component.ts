import { Component, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';
import { GoogleSigninButtonModule, SocialAuthService } from '@abacritt/angularx-social-login';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, GoogleSigninButtonModule],
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

          <div class="divider">
            <span>OR</span>
          </div>

          <div class="social-login" style="display: flex; justify-content: center; margin-top: 1rem; margin-bottom: 1rem;">
            <asl-google-signin-button type="standard" size="large" text="signin_with" shape="rectangular" theme="filled_blue"></asl-google-signin-button>
          </div>

          <div class="auth-footer">
            <p>Don't have an account? <a routerLink="/auth/register">Create one</a></p>
          </div>

          <div class="demo-creds">
            <p><strong>Demo:</strong> trader&#64;tradingjournal.com / Trader&#64;123</p>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .divider {
      display: flex;
      align-items: center;
      text-align: center;
      margin: 1.5rem 0;
      color: var(--text-muted);
      font-size: 0.9rem;
    }
    .divider::before, .divider::after {
      content: '';
      flex: 1;
      border-bottom: 1px solid var(--border-color);
    }
    .divider span {
      padding: 0 10px;
    }
  `]
})
export class LoginComponent implements OnInit, OnDestroy {
  form;
  loading = signal(false);
  showPassword = signal(false);
  private authSubscription!: Subscription;

  constructor(
    private fb: FormBuilder, 
    private authService: AuthService, 
    private socialAuthService: SocialAuthService,
    private router: Router,
    private toast: ToastService
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  ngOnInit() {
    this.authSubscription = this.socialAuthService.authState.subscribe((user) => {
      if (user) {
        this.loading.set(true);
        this.authService.loginWithGoogle(user.idToken!).subscribe({
          next: () => {
            this.toast.success('Successfully signed in with Google!', 'Welcome Back');
            this.router.navigate(['/dashboard']);
          },
          error: (err) => {
            this.toast.error(err.error?.error || 'Google login failed.', 'Authentication Error');
            this.loading.set(false);
          }
        });
      }
    });
  }

  ngOnDestroy() {
    if (this.authSubscription) {
      this.authSubscription.unsubscribe();
    }
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

