import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function describeApiError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Something went wrong. Please try again.';
  }

  if (error.status === 0) {
    return 'The auction API is unreachable. Check that the backend is running.';
  }

  if (error.status === HttpStatusCode.TooManyRequests) {
    return 'You are bidding too fast. Wait a moment and try again.';
  }

  const problem = error.error as ProblemDetails | null;
  const validationMessages = Object.values(problem?.errors ?? {}).flat();
  if (validationMessages.length > 0) {
    return validationMessages.join(' ');
  }

  return problem?.detail ?? problem?.title ?? `Request failed with status ${error.status}.`;
}
