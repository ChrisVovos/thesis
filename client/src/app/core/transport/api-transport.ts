/**
 * The API surface a request is sent over.
 *
 * The whole comparison rests on this being the only thing that varies: every feature component,
 * route, form and view model in the application is written once and executes identically for both
 * values.
 */
export type ApiTransport = 'rest' | 'graphql';

/** The transports the application knows how to speak. */
export const API_TRANSPORTS: readonly ApiTransport[] = ['rest', 'graphql'] as const;

/**
 * Narrows an untrusted value to an {@link ApiTransport}.
 *
 * @param value The candidate value, typically read from local storage.
 * @returns `true` when the value names a supported transport.
 */
export function isApiTransport(value: unknown): value is ApiTransport {
  return typeof value === 'string' && (API_TRANSPORTS as readonly string[]).includes(value);
}
