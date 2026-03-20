import { Component, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;

  form = this.fb.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])/),
    ]]
  });

  get firstName() { return this.form.get('firstName'); }
  get lastName() { return this.form.get('lastName'); }
  get email() { return this.form.get('email'); }
  get password() { return this.form.get('password'); }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }
    this.loading = true;
    this.cdr.markForCheck();
    const email = this.form.value.email as string;
    this.auth.register(this.form.value as any).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/auth/verify-email'], { queryParams: { email } });
      },
      error: (err) => {
        const msg = err.error?.error ?? err.error?.message ?? err.error?.title ?? 'Registration failed. Please try again.';
        this.toast.error(msg);
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }
}
