const MS_PER_MINUTE = 60_000;
const DATETIME_LOCAL_LENGTH = 16;

// <input type="datetime-local"> works in local time without a zone, e.g. "2026-01-15T15:30".
export function toDateTimeLocal(date: Date): string {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * MS_PER_MINUTE);
  return local.toISOString().slice(0, DATETIME_LOCAL_LENGTH);
}

export function fromDateTimeLocal(value: string): string {
  return new Date(value).toISOString();
}
