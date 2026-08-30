import { HttpErrorResponse } from '@angular/common/http';

interface ApiProblem {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  const problem = error.error as ApiProblem | null;
  const validationMessage = problem?.errors ? Object.values(problem.errors).flat()[0] : undefined;

  return validationMessage ?? problem?.detail ?? problem?.title ?? fallback;
}
