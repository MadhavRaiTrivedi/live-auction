import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { SignedInUser } from './session';
import { UserRole } from './user-role';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  requestDevToken(role: UserRole, userId: string | null): Promise<SignedInUser> {
    return firstValueFrom(
      this.http.post<SignedInUser>('/api/auth/dev-token', { role, userId: userId || null }),
    );
  }
}
