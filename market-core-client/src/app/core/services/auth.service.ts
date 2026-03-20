import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, ForgotPasswordCommand, LoginCommand, RegisterCommand, ResetPasswordCommand } from '../models/auth.models';


const TOKEN_KEY = 'marketcore_token';
const USER_KEY  = 'marketcore_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/Auth`;

  private _session = signal<AuthResponse | null>(this.loadFromStorage());

  readonly currentUser     = this._session.asReadonly();
  readonly isLoggedIn      = computed(() => this._session() !== null);
  readonly isAdmin         = computed(() => this._session()?.user?.role === 'Admin');
  readonly isEmailVerified = computed(() => this._session()?.user?.isEmailVerified === true);

  readonly fullName = computed(() => {
    const u = this._session()?.user;
    return u ? `${u.firstName} ${u.lastName}` : '';
  });

  constructor(private http: HttpClient) {}

  login(command: LoginCommand): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, command).pipe(
      tap(r => this.persist(r))
    );
  }

  register(command: RegisterCommand): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/register`, command);
  }

  verifyEmail(token: string): Observable<void> {
    return this.http.get<void>(`${this.apiUrl}/verify-email`, { params: { token } }).pipe(
      tap(() => {

        const session = this._session();
        if (session) {
          const updated: AuthResponse = {
            ...session,
            user: { ...session.user, isEmailVerified: true }
          };
          this.persist(updated);
        }
      })
    );
  }

  forgotPassword(command: ForgotPasswordCommand): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/forgot-password`, command);
  }

  resetPassword(command: ResetPasswordCommand): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/reset-password`, command);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._session.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private persist(response: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(USER_KEY, JSON.stringify(response));
    this._session.set(response);
  }

  private loadFromStorage(): AuthResponse | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try { return JSON.parse(raw) as AuthResponse; }
    catch { return null; }
  }
}
