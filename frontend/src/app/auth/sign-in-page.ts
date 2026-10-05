import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { describeApiError } from '../shared/api-error';
import { AuthApi } from './auth.api';
import { Session } from './session';
import { UserRole } from './user-role';

@Component({
  selector: 'app-sign-in-page',
  imports: [ReactiveFormsModule],
  templateUrl: './sign-in-page.html',
})
export class SignInPage {
  private readonly authApi = inject(AuthApi);
  private readonly session = inject(Session);
  private readonly router = inject(Router);

  protected readonly roles = Object.values(UserRole);
  protected readonly error = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    role: UserRole.User,
    userId: '',
  });

  protected async signIn(): Promise<void> {
    const { role, userId } = this.form.getRawValue();
    this.isSubmitting.set(true);
    this.error.set(null);
    try {
      this.session.signIn(await this.authApi.requestDevToken(role, userId.trim()));
      await this.router.navigate(['/auctions']);
    } catch (error) {
      this.error.set(describeApiError(error));
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
