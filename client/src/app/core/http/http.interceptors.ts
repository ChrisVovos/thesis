import {
  HttpErrorResponse,
  type HttpEvent,
  type HttpInterceptorFn,
  type HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, finalize, from, switchMap, throwError, type Observable } from 'rxjs';
import { AuthStore } from '../auth/auth.store';
import { normalizeHttpError } from '../errors/error-normalizer';
import { BusyService } from '../notifications/notification.service';

/** The header carrying the correlation identifier shared with the server. */
export const CORRELATION_HEADER = 'X-Correlation-Id';

/** The header a client sets to name the logical operation it is performing. */
export const OPERATION_HEADER = 'X-Benchmark-Operation';

/**
 * Attaches the bearer token and transparently renews it once when the server says it expired.
 *
 * Retrying exactly once, and only for a 401 on a request that already carried a token, keeps the
 * renewal path from turning a genuinely unauthorized call into an infinite loop.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthStore);

  if (isAuthEndpoint(request.url)) {
    return next(request);
  }

  const token = auth.accessToken;
  const authorized = token ? withBearer(request, token) : request;

  return next(authorized).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || !token) {
        return throwError(() => error);
      }

      return from(auth.refresh()).pipe(
        switchMap((renewed): Observable<HttpEvent<unknown>> => {
          if (!renewed) {
            return throwError(() => error);
          }

          return next(withBearer(request, renewed));
        }),
      );
    }),
  );
};

/**
 * Gives every request a correlation identifier so a client action can be found in the server log.
 */
export const correlationInterceptor: HttpInterceptorFn = (request, next) =>
  next(
    request.clone({
      setHeaders: { [CORRELATION_HEADER]: request.headers.get(CORRELATION_HEADER) ?? newCorrelationId() },
    }),
  );

/**
 * Converts every REST failure into the shared {@link AppError} shape before it leaves the transport.
 */
export const errorNormalizingInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((error: unknown) =>
      throwError(() => (error instanceof HttpErrorResponse ? normalizeHttpError(error) : error)),
    ),
  );

/**
 * Keeps the shell's busy indicator in step with the requests actually in flight.
 */
export const busyInterceptor: HttpInterceptorFn = (request, next) => {
  const busy = inject(BusyService);
  busy.start();
  return next(request).pipe(finalize(() => busy.stop()));
};

function withBearer(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function isAuthEndpoint(url: string): boolean {
  return url.includes('/auth/login') || url.includes('/auth/refresh') || url.includes('/auth/logout');
}

function newCorrelationId(): string {
  return crypto.randomUUID().replaceAll('-', '');
}
