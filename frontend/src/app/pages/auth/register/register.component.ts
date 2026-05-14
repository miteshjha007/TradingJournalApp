import { Component, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';
import { GoogleSigninButtonModule, SocialAuthService } from '@abacritt/angularx-social-login';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, GoogleSigninButtonModule],
  template: `
    <div class="auth-page">
      <div class="auth-container">
        <div class="auth-brand">
          <div class="brand-icon">📈</div>
          <h1 class="brand-name">TradingJournal</h1>
          <p class="brand-tagline">Start your trading journey</p>
        </div>
        <div class="auth-card">
          <h2>Create Account</h2>
          <p class="auth-subtitle">Join thousands of disciplined traders</p>

          <form [formGroup]="form" (ngSubmit)="onSubmit()">
            <div class="form-row">
              <div class="form-group">
                <label>First Name</label>
                <input type="text" formControlName="firstName" placeholder="John" class="form-input" />
              </div>
              <div class="form-group">
                <label>Last Name</label>
                <input type="text" formControlName="lastName" placeholder="Trader" class="form-input" />
              </div>
            </div>
            <div class="form-group">
              <label>Email Address</label>
              <input type="email" formControlName="email" placeholder="john@example.com" class="form-input" />
            </div>
            <div class="form-group">
              <label>Password</label>
              <input type="password" formControlName="password" placeholder="Min. 8 characters" class="form-input" />
            </div>
            <button type="submit" class="btn btn-primary btn-full" [disabled]="loading()">
              @if (loading()) { <span class="spinner"></span> } Create Account
            </button>
          </form>

          <div class="divider">
            <span>OR</span>
          </div>

          <div class="social-login" style="display: flex; justify-content: center; margin-top: 1rem; margin-bottom: 1rem;">
            <asl-google-signin-button type="standard" size="large" text="signup_with" shape="rectangular" theme="filled_blue"></asl-google-signin-button>
          </div>

          <div class="auth-footer">
            <p>Already have an account? <a routerLink="/auth/login">Sign in</a></p>
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
export class RegisterComponent implements OnInit, OnDestroy {
  form;
  loading = signal(false);
  private authSubscription!: Subscription;

  constructor(
    private fb: FormBuilder, 
    private authService: AuthService, 
    private socialAuthService: SocialAuthService,
    private router: Router,
    private toast: ToastService
  ) {
    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
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

  onSubmit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);

    this.authService.register(this.form.value as any).subscribe({
      next: () => {
        this.toast.success('Registration successful! Redirecting...', 'Welcome!');
        setTimeout(() => this.router.navigate(['/dashboard']), 1500);
      },
      error: (err) => {
        this.toast.error(err.error?.error || 'Registration failed.', 'Registration Error');
        this.loading.set(false);
      }
    });
  }
}

