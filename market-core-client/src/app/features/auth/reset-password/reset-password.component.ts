import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb     = inject(FormBuilder);
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route  = inject(ActivatedRoute);
  private readonly cdr    = inject(ChangeDetectorRef);

  loading      = false;
  success      = false;
  errorMsg     = '';
  token        = '';

  form = this.fb.group({
    newPassword: ['', [Validators.required, Validators.minLength(6)]]
  });

  get newPassword() { return this.form.get('newPassword'); }

  ngOnInit(): void {
    const t = this.route.snapshot.queryParamMap.get('token');
    if (!t) {
      this.errorMsg = 'Invalid or missing reset token.';
      this.cdr.markForCheck();
      return;
    }
    this.token = t;
  }

  submit(): void {
    if (this.token === '') return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }
    this.loading  = true;
    this.errorMsg = '';
    this.cdr.markForCheck();
    this.auth.resetPassword({ token: this.token, newPassword: this.form.value.newPassword as string }).subscribe({
      next: () => {
        this.success = true;
        this.loading = false;
        this.cdr.markForCheck();
        setTimeout(() => this.router.navigate(['/auth/login']), 2000);
      },
      error: (err) => {
        this.errorMsg = err.error?.error ?? err.error?.message ?? err.error?.title ?? 'Password reset failed. The link may have expired.';
        this.loading  = false;
        this.cdr.markForCheck();
      }
    });
  }
}
