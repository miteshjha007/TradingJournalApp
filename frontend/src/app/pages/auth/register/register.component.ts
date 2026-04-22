import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
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

          <div class="auth-footer">
            <p>Already have an account? <a routerLink="/auth/login">Sign in</a></p>
          </div>
        </div>
      </div>
    </div>
  `
})
export class RegisterComponent {
  form;
  loading = signal(false);

  constructor(
    private fb: FormBuilder, 
    private authService: AuthService, 
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

