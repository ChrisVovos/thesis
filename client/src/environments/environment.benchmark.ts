import type { Environment } from './environment';

/**
 * The build used for the comparative measurements: production-like, but with the transport selector
 * and the client side metrics collector switched on so a run can be driven from the browser.
 */
export const environment: Environment = {
  production: true,
  defaultTransport: 'rest',
  showTransportSelector: true,
  restBaseUrl: '/api/v1',
  graphqlUrl: '/graphql',
  collectMetrics: true,
};
