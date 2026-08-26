import type { DocumentNode } from 'graphql';
import type { Apollo } from 'apollo-angular';
import { map, type Observable } from 'rxjs';
import { normalizeGraphQlErrors } from '../../core/errors/error-normalizer';

/**
 * Runs a query and unwraps its payload, converting any reported error into an {@link AppError}.
 *
 * Queries are executed `network-only`. Apollo's normalized cache would otherwise answer the second
 * and later runs of a benchmark from memory, and a comparison in which one transport serves requests
 * from RAM and the other from the network measures nothing useful. The cache remains available for
 * ordinary use; it is simply bypassed on the path the study exercises.
 *
 * @param apollo The Apollo client.
 * @param document The operation to run.
 * @param select Extracts the payload from the response data.
 * @param variables The operation variables.
 * @returns The unwrapped payload.
 */
export function runQuery<TData, TResult>(
  apollo: Apollo,
  document: DocumentNode,
  select: (data: TData) => TResult,
  variables?: Record<string, unknown>,
): Observable<TResult> {
  return apollo
    .query<TData>({
      query: document,
      variables,
      fetchPolicy: 'network-only',
      errorPolicy: 'all',
    })
    .pipe(
      map((response) => {
        if (response.errors?.length) {
          throw normalizeGraphQlErrors(response.errors);
        }

        return select(response.data);
      }),
    );
}

/**
 * Runs a mutation and unwraps its payload, converting any reported error into an {@link AppError}.
 *
 * @param apollo The Apollo client.
 * @param document The operation to run.
 * @param select Extracts the payload from the response data.
 * @param variables The operation variables.
 * @returns The unwrapped payload.
 */
export function runMutation<TData, TResult>(
  apollo: Apollo,
  document: DocumentNode,
  select: (data: TData) => TResult,
  variables?: Record<string, unknown>,
): Observable<TResult> {
  return apollo
    .mutate<TData>({
      mutation: document,
      variables,
      errorPolicy: 'all',
    })
    .pipe(
      map((response) => {
        if (response.errors?.length) {
          throw normalizeGraphQlErrors(response.errors);
        }

        if (!response.data) {
          throw normalizeGraphQlErrors([{ message: 'The mutation returned no payload.' }]);
        }

        return select(response.data);
      }),
    );
}
