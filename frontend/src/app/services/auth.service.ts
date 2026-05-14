import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, UserInfo } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;

  currentUser = signal<UserInfo | null>(null);
  isLoggedIn = signal(false);

  constructor(private http: HttpClient, private router: Router) {
    this.loadFromStorage();
  }

  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, data).pipe(
      tap(res => this.storeAuth(res))
    );
  }

  login(data: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, data).pipe(
      tap(res => this.storeAuth(res))
    );
  }

  loginWithGoogle(idToken: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/google`, { idToken }).pipe(
      tap(res => this.storeAuth(res))
    );
  }

  refresh(): Observable<AuthResponse> {
    const token = localStorage.getItem('refreshToken') || '';
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken: token }).pipe(
      tap(res => this.storeAuth(res))
    );
  }

  logout(): void {
    localStorage.clear();
    this.currentUser.set(null);
    this.isLoggedIn.set(false);
    this.router.navigate(['/auth/login']);
  }

  getToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  isAdmin(): boolean {
    return this.currentUser()?.role === 'Admin';
  }

  private storeAuth(res: AuthResponse): void {
    localStorage.setItem('accessToken', res.accessToken);
    localStorage.setItem('refreshToken', res.refreshToken);
    localStorage.setItem('user', JSON.stringify(res.user));
    this.currentUser.set(res.user);
    this.isLoggedIn.set(true);
  }

  private loadFromStorage(): void {
    const user = localStorage.getItem('user');
    const token = localStorage.getItem('accessToken');
    if (user && token) {
      this.currentUser.set(JSON.parse(user));
      this.isLoggedIn.set(true);
    }
  }
}
