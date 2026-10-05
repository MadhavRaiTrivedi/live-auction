import { computed, Injectable, signal } from '@angular/core';
import { UserRole } from './user-role';

export interface SignedInUser {
  accessToken: string;
  userId: string;
  role: UserRole;
  expiresAt: string;
}

const STORAGE_KEY = 'live-auction.session';

@Injectable({ providedIn: 'root' })
export class Session {
  private readonly user = signal<SignedInUser | null>(Session.restore());

  readonly current = this.user.asReadonly();
  readonly isSignedIn = computed(() => this.user() !== null);
  readonly isAdmin = computed(() => this.user()?.role === UserRole.Admin);

  signIn(user: SignedInUser): void {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(user));
    this.user.set(user);
  }

  signOut(): void {
    sessionStorage.removeItem(STORAGE_KEY);
    this.user.set(null);
  }

  private static restore(): SignedInUser | null {
    const stored = sessionStorage.getItem(STORAGE_KEY);
    if (!stored) {
      return null;
    }

    const user = JSON.parse(stored) as SignedInUser;
    return new Date(user.expiresAt) > new Date() ? user : null;
  }
}
