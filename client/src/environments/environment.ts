import type { Environment } from './environment.model';

export const environment: Environment = {
  production: true,
  defaultTransport: 'rest',
  showTransportSelector: false,
  restBaseUrl: '/api/v1',
  graphqlUrl: '/graphql',
  collectMetrics: false,
};
