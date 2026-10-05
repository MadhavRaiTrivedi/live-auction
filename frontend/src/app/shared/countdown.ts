const MS_PER_SECOND = 1_000;
const SECONDS_PER_MINUTE = 60;
const SECONDS_PER_HOUR = 3_600;
const SECONDS_PER_DAY = 86_400;

export function formatCountdown(remainingMs: number): string {
  if (remainingMs <= 0) {
    return 'Ended';
  }

  const totalSeconds = Math.ceil(remainingMs / MS_PER_SECOND);
  const days = Math.floor(totalSeconds / SECONDS_PER_DAY);
  const hours = Math.floor((totalSeconds % SECONDS_PER_DAY) / SECONDS_PER_HOUR);
  const minutes = Math.floor((totalSeconds % SECONDS_PER_HOUR) / SECONDS_PER_MINUTE);
  const seconds = totalSeconds % SECONDS_PER_MINUTE;
  const clock = [hours, minutes, seconds].map((part) => String(part).padStart(2, '0')).join(':');

  return days > 0 ? `${days}d ${clock}` : clock;
}
