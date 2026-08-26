/** The classification of a failure, mirrored from the server's transport-neutral error type. */
export type AppErrorKind =
  | 'validation'
  | 'notFound'
  | 'conflict'
  | 'unauthorized'
  | 'forbidden'
  | 'failure';

/**
 * A failure in the shape the user interface consumes.
 *
 * REST reports failures as RFC 9457 problem documents and GraphQL reports them as error extensions.
 * Both are normalized into this one type before they reach a component, so no feature code contains a
 * branch on which transport produced the error.
 */
export interface AppError {
  /** The stable, machine readable identifier of the failure. */
  readonly code: string;
  /** The human readable explanation, safe to show to a user. */
  readonly message: string;
  /** The classification used to choose how the failure is presented. */
  readonly kind: AppErrorKind;
  /** Per-field messages, populated for validation failures. */
  readonly fieldErrors?: Readonly<Record<string, readonly string[]>>;
  /** The correlation identifier of the request, when the server supplied one. */
  readonly correlationId?: string;
}

/**
 * Creates an error for a condition the client detected itself.
 *
 * @param code The stable failure identifier.
 * @param message The human readable explanation.
 * @param kind The classification.
 * @returns The error.
 */
export function appError(code: string, message: string, kind: AppErrorKind = 'failure'): AppError {
  return { code, message, kind };
}

/**
 * Determines whether a value is an {@link AppError}.
 *
 * @param value The candidate value.
 * @returns `true` when the value is a normalized application error.
 */
export function isAppError(value: unknown): value is AppError {
  return (
    typeof value === 'object' &&
    value !== null &&
    'code' in value &&
    'message' in value &&
    'kind' in value
  );
}
