import { HttpErrorResponse } from '@angular/common/http';
import type { GraphQLFormattedError } from 'graphql';
import { appError, type AppError, type AppErrorKind } from './app-error';

/** The server's classification values, as published in the error extensions. */
const CLASSIFICATION_TO_KIND: Readonly<Record<string, AppErrorKind>> = {
  BAD_USER_INPUT: 'validation',
  NOT_FOUND: 'notFound',
  CONFLICT: 'conflict',
  UNAUTHENTICATED: 'unauthorized',
  FORBIDDEN: 'forbidden',
  INTERNAL_SERVER_ERROR: 'failure',
};

/** The HTTP status codes the REST surface uses for each classification. */
const STATUS_TO_KIND: Readonly<Record<number, AppErrorKind>> = {
  400: 'validation',
  401: 'unauthorized',
  403: 'forbidden',
  404: 'notFound',
  409: 'conflict',
  422: 'validation',
  429: 'failure',
};

/** The shape of an RFC 9457 problem document as this API emits it. */
interface ProblemDocument {
  readonly title?: string;
  readonly detail?: string;
  readonly status?: number;
  readonly code?: string;
  readonly correlationId?: string;
  readonly errors?: Record<string, string[]>;
}

/**
 * Converts a REST failure into the shared {@link AppError} shape.
 *
 * @param response The failed HTTP response.
 * @returns The normalized error.
 */
export function normalizeHttpError(response: HttpErrorResponse): AppError {
  if (response.status === 0) {
    return appError('network.unreachable', 'The server could not be reached.', 'failure');
  }

  const problem = (response.error ?? {}) as ProblemDocument;
  const kind = STATUS_TO_KIND[response.status] ?? 'failure';

  return {
    code: problem.code ?? `http.${response.status}`,
    message: problem.detail ?? problem.title ?? response.message,
    kind,
    fieldErrors: problem.errors,
    correlationId: problem.correlationId ?? response.headers?.get('X-Correlation-Id') ?? undefined,
  };
}

/**
 * Converts a GraphQL failure into the shared {@link AppError} shape.
 *
 * @param errors The errors reported by the server.
 * @param correlationId The correlation identifier of the request, when known.
 * @returns The normalized error.
 */
export function normalizeGraphQlErrors(
  errors: readonly GraphQLFormattedError[],
  correlationId?: string,
): AppError {
  const first = errors[0];
  if (!first) {
    return appError('graphql.unknown', 'The operation failed for an unknown reason.');
  }

  const extensions = (first.extensions ?? {}) as {
    code?: string;
    classification?: string;
    errors?: Record<string, string[]>;
  };

  return {
    code: extensions.code ?? 'graphql.unknown',
    message: first.message,
    kind: CLASSIFICATION_TO_KIND[extensions.classification ?? ''] ?? 'failure',
    fieldErrors: extensions.errors,
    correlationId,
  };
}
