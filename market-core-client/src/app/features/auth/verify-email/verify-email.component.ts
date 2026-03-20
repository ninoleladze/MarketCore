import {
  Component, OnInit, inject,
  ChangeDetectionStrategy, ChangeDetectorRef, signal
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

        <div class="verify-logo">M</div>

        @switch (state()) {

          @case ('verifying') {
            <div class="spinner-ring"></div>
            <h2>Verifying your email…</h2>
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
            <a routerLink="/auth/register" class="btn-ghost btn-block">Register again</a>
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
    .btn-ghost {
      display: block; padding: 0.75rem 1.5rem;
      border: 1px solid var(--color-border, #2a2a2e); border-radius: 999px;
      color: var(--neutral-400, #a0a0b0); background: transparent;
      text-decoration: none; font-size: 0.9rem; cursor: pointer;
    }
    .btn-ghost:hover { border-color: var(--orange-600); color: var(--orange-400); }
  `]
})
export class VerifyEmailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly auth  = inject(AuthService);
  private readonly cdr   = inject(ChangeDetectorRef);

  state    = signal<State>('verifying');
  errorMsg = signal('The link is invalid or has expired. Please register again.');

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!token) {
      this.errorMsg.set('No verification token found. Please use the link from your email.');
      this.state.set('error');
      this.cdr.markForCheck();
      return;
    }

    this.auth.verifyEmail(token).subscribe({
      next: () => {
        this.state.set('success');
        this.cdr.markForCheck();
      },
      error: (err) => {
        const msg = err.error?.detail ?? err.error?.error ?? err.error?.message ?? 'The link is invalid or has expired.';
        this.errorMsg.set(msg);
        this.state.set('error');
        this.cdr.markForCheck();
      }
    });
  }
}
