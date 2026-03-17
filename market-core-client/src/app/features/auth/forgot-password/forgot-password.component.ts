import { Component, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ForgotPasswordComponent {
  private readonly fb   = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly cdr  = inject(ChangeDetectorRef);

  loading  = false;
  sent     = false;
  errorMsg = '';

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  get email() { return this.form.get('email'); }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }
    this.loading  = true;
    this.errorMsg = '';
    this.cdr.markForCheck();
    this.auth.forgotPassword({ email: this.form.value.email as string }).subscribe({
      next: () => {
        this.sent    = true;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.errorMsg = err.error?.error ?? err.error?.message ?? err.error?.title ?? 'Something went wrong. Please try again.';
        this.loading  = false;
        this.cdr.markForCheck();
      }
    });
  }
}
