import {
  Component, OnInit, inject,
  ChangeDetectionStrategy, ChangeDetectorRef, signal
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';

type State = 'pending' | 'verifying' | 'success' | 'error';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="verify-page">
      <div class="verify-card">

        <div class="verify-logo">M</div>

        @switch (state()) {

          @case ('pending') {
            <h2>Check your inbox</h2>
            <p class="sub">
              We sent a 6-digit verification code to<br>
              <strong class="email-highlight">{{ email() || 'your email address' }}</strong>
            </p>

            <form [formGroup]="codeForm" (ngSubmit)="submit()" novalidate>
              <div class="code-field">
                <input
                  formControlName="code"
                  type="text"
                  inputmode="numeric"
                  maxlength="6"
                  placeholder="000000"
                  autocomplete="one-time-code"
                  class="code-input"
                  [class.invalid]="codeForm.get('code')?.invalid && codeForm.get('code')?.touched"
                />
                @if (codeForm.get('code')?.invalid && codeForm.get('code')?.touched) {
                  <span class="field-error">Enter the 6-digit code from your email.</span>
                }
              </div>

              <button type="submit" class="btn-primary btn-block" [disabled]="submitting">
                @if (submitting) { <span class="btn-spinner"></span> }
                Verify Email
              </button>
            </form>

            <p class="resend-hint">
              Didn't receive it? Check your spam folder or
              <a routerLink="/auth/register" class="link">register again</a>.
            </p>
          }

          @case ('verifying') {
            <div class="spinner-ring"></div>
            <h2>Verifying…</h2>
            <p class="sub">Just a moment.</p>
          }

          @case ('success') {
            <div class="icon-success">✓</div>
            <h2>Email verified!</h2>
            <p class="sub">
              Your account is now fully activated.<br>
              You can start browsing and shopping.
            </p>
            <a routerLink="/auth/login" class="btn-primary btn-block">Sign in</a>
          }

          @case ('error') {
            <div class="icon-error">✕</div>
            <h2>Verification failed</h2>
            <p class="sub">{{ errorMsg() }}</p>
            <button class="btn-ghost btn-block" (click)="reset()">Try again</button>
            <a routerLink="/auth/register" class="btn-ghost btn-block" style="margin-top:.75rem;">
              Register again
            </a>
          }

        }

      </div>
    </div>
  `,
  styles: [`
    .verify-page {
      min-height: 100vh;
      background: var(--navy-950, #071428);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem;
    }

    .verify-card {
      background: var(--navy-800, #132b52);
      border: 1px solid var(--color-border, #2a2a2e);
      border-radius: 20px;
      padding: 3rem 2.5rem;
      width: 100%;
      max-width: 440px;
      text-align: center;
    }

    .verify-logo {
      width: 56px; height: 56px;
      background: linear-gradient(135deg, var(--orange-700, #c45c00), var(--orange-500, #e87722));
      border-radius: 14px;
      display: flex; align-items: center; justify-content: center;
      font-size: 1.6rem; font-weight: 700; color: #fff;
      margin: 0 auto 1.5rem;
    }

    h2 {
      font-family: var(--font-display, serif);
      font-size: 1.6rem; font-weight: 400;
      color: var(--neutral-50, #f5f5f5);
      margin: 0 0 0.75rem;
    }

    .sub {
      font-size: 0.95rem; color: var(--neutral-500, #808090);
      line-height: 1.7; margin: 0 0 2rem;
    }

    .email-highlight { color: var(--neutral-200, #e0e0f0); font-weight: 600; }

    .code-field { margin-bottom: 1.25rem; text-align: left; }

    .code-input {
      width: 100%; box-sizing: border-box;
      background: rgba(0,0,0,0.3);
      border: 1px solid var(--color-border, #2a2a2e);
      border-radius: 12px;
      padding: 1rem 1.25rem;
      color: var(--neutral-50, #f5f5f5);
      font-size: 2rem; font-weight: 700; letter-spacing: 0.5rem;
      font-family: 'Courier New', monospace;
      text-align: center;
      outline: none; transition: border-color 0.2s;
    }
    .code-input:focus { border-color: var(--orange-500, #e87722); }
    .code-input.invalid { border-color: #e05050; }
    .code-input::placeholder { color: #444; letter-spacing: 0.3rem; }

    .field-error { font-size: 0.8rem; color: #e05050; margin-top: 0.4rem; display: block; }

    .resend-hint {
      margin-top: 1.5rem; font-size: 0.85rem; color: var(--neutral-500, #808090);
    }
    .link { color: var(--orange-400, #ffb870); text-decoration: none; }
    .link:hover { text-decoration: underline; }

    .spinner-ring {
      width: 52px; height: 52px;
      border: 3px solid rgba(232,119,34,0.15);
      border-top-color: var(--orange-500, #e87722);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      margin: 0 auto 1.5rem;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .icon-success, .icon-error {
      width: 60px; height: 60px; border-radius: 50%;
      display: flex; align-items: center; justify-content: center;
      font-size: 1.6rem; font-weight: 700; margin: 0 auto 1.5rem;
    }
    .icon-success { background: rgba(34,197,94,0.12); color: #22c55e; border: 2px solid rgba(34,197,94,0.25); }
    .icon-error   { background: rgba(232,119,34,0.12); color: var(--orange-400,#ffb870); border: 2px solid rgba(232,119,34,0.25); }

    .btn-block { display: block; width: 100%; text-align: center; box-sizing: border-box; }
    .btn-primary {
      display: block; width: 100%;
      padding: 0.875rem 1.5rem;
      background: linear-gradient(135deg, var(--orange-700,#c45c00), var(--orange-500,#e87722));
      color: #fff; font-size: 1rem; font-weight: 600;
      border: none; border-radius: 999px; cursor: pointer;
      text-decoration: none; transition: opacity 0.2s;
    }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
    .btn-ghost {
      display: block; padding: 0.75rem 1.5rem;
      border: 1px solid var(--color-border, #2a2a2e); border-radius: 999px;
      color: var(--neutral-400, #a0a0b0); background: transparent;
      text-decoration: none; font-size: 0.9rem; cursor: pointer; transition: all 0.2s;
    }
    .btn-ghost:hover { border-color: var(--orange-600); color: var(--orange-400); }

    .btn-spinner {
      display: inline-block; width: 14px; height: 14px;
      border: 2px solid rgba(255,255,255,0.3); border-top-color: #fff;
      border-radius: 50%; animation: spin 0.7s linear infinite;
      vertical-align: middle; margin-right: 0.5rem;
    }
  `]
})
export class VerifyEmailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly auth  = inject(AuthService);
  private readonly fb    = inject(FormBuilder);
  private readonly cdr   = inject(ChangeDetectorRef);

  state     = signal<State>('pending');
  errorMsg  = signal('Invalid or expired code.');
  email     = signal('');
  submitting = false;

  codeForm = this.fb.group({
    code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
  });

  ngOnInit(): void {
    const emailParam = this.route.snapshot.queryParamMap.get('email');
    if (emailParam) this.email.set(emailParam);
  }

  submit(): void {
    if (this.codeForm.invalid) {
      this.codeForm.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }
    const code = this.codeForm.value.code!.trim();
    this.submitting = true;
    this.state.set('verifying');

    this.auth.verifyEmail(code).subscribe({
      next: () => {
        this.submitting = false;
        this.state.set('success');
        this.cdr.markForCheck();
      },
      error: (err) => {
        const msg = err.error?.detail ?? err.error?.error ?? err.error?.message ?? 'Invalid or expired code.';
        this.errorMsg.set(msg);
        this.submitting = false;
        this.state.set('error');
        this.cdr.markForCheck();
      }
    });
  }

  reset(): void {
    this.codeForm.reset();
    this.state.set('pending');
    this.cdr.markForCheck();
  }
}
