// Pattern: Observer — ProfileService exposes Observables; component subscribes
// and propagates state into Angular signals for fine-grained reactivity.
// Pattern: Command — saveProfile() and changePassword() encapsulate
// discrete user-initiated mutations with full loading/error lifecycle.

import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  signal,
  computed,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ProfileService } from '../../core/services/profile.service';
import {
  ProfileDto,
  UpdateProfileCommand,
  ChangePasswordCommand,
} from '../../core/models/profile.models';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, DatePipe],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileComponent implements OnInit {
  // ── State signals ─────────────────────────────────────────────────────────
  profile = signal<ProfileDto | null>(null);
  loading = signal(true);
  saving = signal(false);
  pwSaving = signal(false);
  successMsg = signal('');
  errorMsg = signal('');
  pwSuccessMsg = signal('');
  pwErrorMsg = signal('');

  // ── Address panel toggle ───────────────────────────────────────────────────
  addressExpanded = signal(false);

  // ── Password visibility toggles ───────────────────────────────────────────
  showCurrentPw = signal(false);
  showNewPw = signal(false);

  // ── Password strength (0–4) ────────────────────────────────────────────────
  pwStrength = signal(0);

  // ── Edit-form model (two-way bound) ───────────────────────────────────────
  firstName = '';
  lastName = '';
  gitHubUrl = '';
  street = '';
  city = '';
  state = '';
  zipCode = '';
  country = '';

  // ── Password-form model ───────────────────────────────────────────────────
  currentPassword = '';
  newPassword = '';

  // ── Computed ──────────────────────────────────────────────────────────────
  initials = computed(() => {
    const p = this.profile();
    if (!p) return '??';
    return (
      (p.firstName?.[0] ?? '').toUpperCase() +
      (p.lastName?.[0] ?? '').toUpperCase()
    );
  });

  pwStrengthLabel = computed(() => {
    const s = this.pwStrength();
    if (s === 0) return '';
    if (s === 1) return 'Weak';
    if (s === 2) return 'Fair';
    if (s === 3) return 'Good';
    return 'Strong';
  });

  pwStrengthClass = computed(() => {
    const s = this.pwStrength();
    if (s <= 1) return 'weak';
    if (s === 2) return 'fair';
    if (s === 3) return 'good';
    return 'strong';
  });

  constructor(private profileService: ProfileService) {}

  ngOnInit(): void {
    this.profileService.getProfile().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.loading.set(false);
        this.populateForm(p);
      },
      error: () => {
        this.loading.set(false);
        this.errorMsg.set('Failed to load profile.');
      },
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private populateForm(p: ProfileDto): void {
    this.firstName = p.firstName;
    this.lastName = p.lastName;
    this.gitHubUrl = p.gitHubUrl ?? '';
    this.street = p.address?.street ?? '';
    this.city = p.address?.city ?? '';
    this.state = p.address?.state ?? '';
    this.zipCode = p.address?.zipCode ?? '';
    this.country = p.address?.country ?? '';
  }

  getInitials(): string {
    return this.initials();
  }

  memberSince(): string {
    const p = this.profile();
    if (!p) return '';
    return new Date(p.createdAt).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  toggleAddress(): void {
    this.addressExpanded.update((v) => !v);
  }

  // ── Password strength calculation ─────────────────────────────────────────
  onNewPasswordChange(value: string): void {
    this.newPassword = value;
    let score = 0;
    if (value.length >= 8) score++;
    if (value.length >= 12) score++;
    if (/[A-Z]/.test(value) && /[a-z]/.test(value)) score++;
    if (/[0-9]/.test(value) && /[^a-zA-Z0-9]/.test(value)) score++;
    this.pwStrength.set(score);
  }

  // ── Save profile ──────────────────────────────────────────────────────────
  // Pattern: Command — encapsulates the update mutation with rollback-ready state
  saveProfile(): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.successMsg.set('');
    this.errorMsg.set('');

    const hasAddress =
      this.street.trim() ||
      this.city.trim() ||
      this.state.trim() ||
      this.zipCode.trim() ||
      this.country.trim();

    const command: UpdateProfileCommand = {
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim(),
      gitHubUrl: this.gitHubUrl.trim() || undefined,
      ...(hasAddress && {
        street: this.street.trim() || undefined,
        city: this.city.trim() || undefined,
        state: this.state.trim() || undefined,
        zipCode: this.zipCode.trim() || undefined,
        country: this.country.trim() || undefined,
      }),
    };

    this.profileService.updateProfile(command).subscribe({
      next: (updated) => {
        this.profile.set(updated);
        this.saving.set(false);
        this.successMsg.set('Profile updated successfully!');
        setTimeout(() => this.successMsg.set(''), 4000);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMsg.set(
          err?.error?.error ?? 'Failed to update profile. Please try again.'
        );
        setTimeout(() => this.errorMsg.set(''), 5000);
      },
    });
  }

  // ── Change password ───────────────────────────────────────────────────────
  changePassword(): void {
    if (this.pwSaving()) return;
    this.pwSaving.set(true);
    this.pwSuccessMsg.set('');
    this.pwErrorMsg.set('');

    const command: ChangePasswordCommand = {
      currentPassword: this.currentPassword,
      newPassword: this.newPassword,
    };

    this.profileService.changePassword(command).subscribe({
      next: () => {
        this.pwSaving.set(false);
        this.pwSuccessMsg.set('Password changed successfully!');
        this.currentPassword = '';
        this.newPassword = '';
        this.pwStrength.set(0);
        setTimeout(() => this.pwSuccessMsg.set(''), 4000);
      },
      error: (err) => {
        this.pwSaving.set(false);
        this.pwErrorMsg.set(
          err?.error?.error ?? 'Failed to change password. Please try again.'
        );
        setTimeout(() => this.pwErrorMsg.set(''), 5000);
      },
    });
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(value);
  }

  truncateId(id: string): string {
    return id ? `${id.slice(0, 8)}...` : '';
  }
}
