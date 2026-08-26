import { HttpHeaders } from '@angular/common/http';
import { effect, inject, Injectable, type Provider } from '@angular/core';
import { ApolloClientOptions, ApolloLink, InMemoryCache } from '@apollo/client/core';
import { setContext } from '@apollo/client/link/context';
import { Apollo, APOLLO_OPTIONS } from 'apollo-angular';
import { HttpLink } from 'apollo-angular/http';
import { environment } from '../../../environments/environment';
import { AuthStore } from '../auth/auth.store';
import { CORRELATION_HEADER } from '../http/http.interceptors';
import { TransportService } from '../transport/transport.service';

/**
 * Builds the Apollo client.
 *
 * The auth link reads the same token store the REST interceptor reads, so the two surfaces cannot
 * present different identities for the same session.
 *
 * @returns The Apollo client options.
 */
function createApolloOptions(): ApolloClientOptions<unknown> {
  const httpLink = inject(HttpLink);
  const auth = inject(AuthStore);

  const contextLink = setContext(() => {
    const token = auth.accessToken;
    const headers: Record<string, string> = {
      [CORRELATION_HEADER]: crypto.randomUUID().replaceAll('-', ''),
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    return { headers: new HttpHeaders(headers) };
  });

  return {
    link: ApolloLink.from([contextLink, httpLink.create({ uri: environment.graphqlUrl })]),
    cache: new InMemoryCache({ addTypename: false }),
    defaultOptions: {
      query: { errorPolicy: 'all' },
      mutate: { errorPolicy: 'all' },
    },
  };
}

/**
 * Clears the Apollo cache whenever the transport changes.
 *
 * A GraphQL run must never be served data that a REST run put in memory, and the reverse must be
 * equally impossible. Without this, the first few operations after a switch would report the latency
 * of a cache lookup and contaminate the comparison.
 */
@Injectable({ providedIn: 'root' })
export class ApolloCacheReset {
  private readonly apollo = inject(Apollo);
  private readonly transport = inject(TransportService);

  constructor() {
    let previous = this.transport.active();
    effect(() => {
      const current = this.transport.active();
      if (current === previous) {
        return;
      }

      previous = current;
      void this.apollo.client.clearStore();
    });
  }
}

/** The providers that make the GraphQL transport available. */
export const graphqlProviders: readonly Provider[] = [
  {
    provide: APOLLO_OPTIONS,
    useFactory: createApolloOptions,
  },
  Apollo,
];
