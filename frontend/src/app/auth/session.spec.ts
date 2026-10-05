import { TestBed } from '@angular/core/testing';
import { Session } from './session';
import { UserRole } from './user-role';

describe('Session', () => {
  beforeEach(() => sessionStorage.clear());

  it('restores a signed-in user that has not expired', () => {
    TestBed.inject(Session).signIn({
      accessToken: 'token',
      userId: 'user-1',
      role: UserRole.Admin,
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    });

    TestBed.resetTestingModule();
    const restored = TestBed.inject(Session);

    expect(restored.isSignedIn()).toBe(true);
    expect(restored.isAdmin()).toBe(true);
  });

  it('ignores an expired session', () => {
    TestBed.inject(Session).signIn({
      accessToken: 'token',
      userId: 'user-1',
      role: UserRole.User,
      expiresAt: new Date(Date.now() - 1_000).toISOString(),
    });

    TestBed.resetTestingModule();

    expect(TestBed.inject(Session).isSignedIn()).toBe(false);
  });
});
