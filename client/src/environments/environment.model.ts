import type { ApiTransport } from '../app/core/transport/api-transport';

/** The settings a build injects into the application. */
export interface Environment {
  /** Whether this build is a production build. */
  readonly production: boolean;
  /** The transport used when the user has never chosen one. */
  readonly defaultTransport: ApiTransport;
  /** Whether the toolbar transport selector is rendered. */
  readonly showTransportSelector: boolean;
  /** The base path of the REST surface. */
  readonly restBaseUrl: string;
  /** The path of the GraphQL surface. */
  readonly graphqlUrl: string;
  /** Whether the client records its own latency and payload measurements. */
  readonly collectMetrics: boolean;
}
