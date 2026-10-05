import { TestBed } from '@angular/core/testing';
import { ServerClock } from './server-clock';

describe('ServerClock', () => {
  afterEach(() => vi.useRealTimers());

  it('follows the server time when the browser clock is behind', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-01-15T10:00:00Z'));
    const clock = TestBed.inject(ServerClock);

    clock.sync('2026-01-15T10:00:05Z');
    vi.advanceTimersByTime(1_000);

    expect(clock.now()).toBe(Date.parse('2026-01-15T10:00:06Z'));
  });
});
