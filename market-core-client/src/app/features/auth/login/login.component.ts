import { Component, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
  private readonly fb     = inject(FormBuilder);
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast  = inject(ToastService);
  private readonly cdr    = inject(ChangeDetectorRef);

  loading = false;

  form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  get email()    { return this.form.get('email'); }
  get password() { return this.form.get('password'); }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }
    this.loading = true;
    this.cdr.markForCheck();
    this.auth.login(this.form.value as any).subscribe({
      next: () => {
        this.router.navigate(['/']);
      },
      error: (err) => {
        const msg = err.error?.error ?? err.error?.message ?? err.error?.title ?? 'Login failed. Check your credentials.';
        this.toast.error(msg);
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }
}
