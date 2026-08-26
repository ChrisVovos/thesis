import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
  type ApplicationConfig,
} from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { routes } from './app.routes';
import { ApolloCacheReset, graphqlProviders } from './core/graphql/apollo.providers';
import {
  authInterceptor,
  busyInterceptor,
  correlationInterceptor,
  errorNormalizingInterceptor,
} from './core/http/http.interceptors';
import { gatewayProviders } from './data-access/gateway.providers';

/**
 * The composition root of the client.
 *
 * The interceptor order matters and reads outside-in: give the request an identity, attach the token,
 * show the busy indicator, and normalize whatever comes back before anything else sees it.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'top' }),
    ),
    provideAnimationsAsync(),
    provideHttpClient(
      withInterceptors([
        correlationInterceptor,
        authInterceptor,
        busyInterceptor,
        errorNormalizingInterceptor,
      ]),
    ),
    ...graphqlProviders,
    ...gatewayProviders,
    // Instantiated eagerly so the Apollo cache is already listening for transport changes before the
    // first screen loads.
    provideAppInitializer(() => {
      inject(ApolloCacheReset);
    }),
  ],
};
