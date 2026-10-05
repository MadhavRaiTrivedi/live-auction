import { formatCountdown } from './countdown';

describe('formatCountdown', () => {
  it('shows hours, minutes and seconds', () => {
    expect(formatCountdown(3_723_000)).toBe('01:02:03');
  });

  it('rounds a partial second up so it never shows zero before the end', () => {
    expect(formatCountdown(200)).toBe('00:00:01');
  });

  it('adds days for long auctions', () => {
    expect(formatCountdown(2 * 86_400_000 + 5_000)).toBe('2d 00:00:05');
  });

  it('says ended once time is up', () => {
    expect(formatCountdown(0)).toBe('Ended');
  });
});
