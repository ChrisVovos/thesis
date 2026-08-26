import type { Environment } from './environment';

export const environment: Environment = {
  production: false,
  defaultTransport: 'rest',
  showTransportSelector: true,
  restBaseUrl: '/api/v1',
  graphqlUrl: '/graphql',
  collectMetrics: true,
};
