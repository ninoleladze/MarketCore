import {
  Component, OnInit, inject,
  ChangeDetectionStrategy, signal
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

type State = 'verifying' | 'success' | 'error';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="verify-page">
      <div class="verify-card">

        <!-- Logo -->
        <div class="verify-logo">M</div>

        @switch (state()) {
          @case ('verifying') {
            <div class="spinner-ring"></div>
            <h2>Verifying your email…</h2>
            <p class="sub">Just a moment, please hold on.</p>
          }

          @case ('success') {
            <div class="icon-success">✓</div>
            <h2>Email verified!</h2>
            <p class="sub">
              Your account is now fully activated.<br>
              You can start browsing and shopping.
            </p>
            <a routerLink="/products" class="btn-primary btn-block">Start Shopping</a>
            <a routerLink="/auth/login" class="btn-ghost btn-block" style="margin-top:.75rem;">
              Sign in
            </a>
          }

          @case ('error') {
            <div class="icon-error">✕</div>
            <h2>Verification failed</h2>
            <p class="sub">{{ errorMsg() }}</p>
            <a routerLink="/auth/register" class="btn-primary btn-block">Register again</a>
          }
        }

      </div>
    </div>
  `,
  styles: [`
    .verify-page {
      min-height: 100vh;
      background: var(--dark-950, #0d0d0f);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem;
    }

    .verify-card {
      background: var(--dark-800, #1a1a1e);
      border: 1px solid var(--color-border, #2a2a2e);
      border-radius: 20px;
      padding: 3rem 2.5rem;
      width: 100%;
      max-width: 420px;
      text-align: center;
    }

    .verify-logo {
      width: 56px;
      height: 56px;
      background: linear-gradient(135deg, var(--red-700, #b00032), var(--red-500, #e00047));
      border-radius: 14px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.6rem;
      font-weight: 700;
      color: #fff;
      margin: 0 auto 1.5rem;
    }

    h2 {
      font-family: var(--font-display, serif);
      font-size: 1.6rem;
      font-weight: 400;
      color: var(--neutral-50, #f5f5f5);
      margin: 0 0 0.75rem;
    }

    .sub {
      font-size: 0.95rem;
      color: var(--neutral-500, #808090);
      line-height: 1.7;
      margin: 0 0 2rem;
    }

    /* Spinner */
    .spinner-ring {
      width: 52px;
      height: 52px;
      border: 3px solid rgba(224,0,71,0.15);
      border-top-color: var(--red-500, #e00047);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      margin: 0 auto 1.5rem;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    /* Icons */
    .icon-success, .icon-error {
      width: 60px;
      height: 60px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.6rem;
      font-weight: 700;
      margin: 0 auto 1.5rem;
    }
    .icon-success {
      background: rgba(34,197,94,0.12);
      color: #22c55e;
      border: 2px solid rgba(34,197,94,0.25);
    }
    .icon-error {
      background: rgba(224,0,71,0.12);
      color: var(--red-400, #ff4d76);
      border: 2px solid rgba(224,0,71,0.25);
    }

    .btn-block { display: block; width: 100%; text-align: center; }
    .btn-ghost {
      display: block;
      padding: 0.75rem 1.5rem;
      border: 1px solid var(--color-border, #2a2a2e);
      border-radius: 999px;
      color: var(--neutral-400, #a0a0b0);
      text-decoration: none;
      font-size: 0.9rem;
      transition: all 0.2s;
    }
    .btn-ghost:hover { border-color: var(--red-600); color: var(--red-400); }
  `]
})
export class VerifyEmailComponent implements OnInit {
  private readonly route   = inject(ActivatedRoute);
  private readonly auth    = inject(AuthService);

  state    = signal<State>('verifying');
  errorMsg = signal('Invalid or expired verification link.');

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!token) {
      this.errorMsg.set('No verification token found in the URL.');
      this.state.set('error');
      return;
    }

    this.auth.verifyEmail(token).subscribe({
      next: () => this.state.set('success'),
      error: (err) => {
        const msg = err.error?.error ?? err.error?.message ?? 'Invalid or expired verification link.';
        this.errorMsg.set(msg);
        this.state.set('error');
      }
    });
  }
}
