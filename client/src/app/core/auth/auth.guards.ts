import { inject } from '@angular/core';
import { Router, type CanActivateFn, type CanMatchFn } from '@angular/router';
import type { PermissionName } from '../../shared/models/auth.models';
import { AuthStore } from './auth.store';

/**
 * Blocks a route until a session exists, restoring one from storage if possible.
 *
 * The guard is the client's own convenience, not a security boundary. Every request it lets through
 * is still authorized by the server's application layer, so a user who bypasses the router gains
 * nothing but an empty screen.
 */
export const authenticatedGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  if (auth.isAuthenticated() || (await auth.restore())) {
    return true;
  }

  return router.createUrlTree(['/sign-in'], { queryParams: { returnUrl: state.url } });
};

/**
 * Builds a guard that additionally requires one of the supplied permissions.
 *
 * @param permissions The permissions that grant access; holding any one of them is enough.
 * @returns A guard that can be attached to a lazy route.
 */
export function requirePermission(...permissions: readonly PermissionName[]): CanMatchFn {
  return async () => {
    const auth = inject(AuthStore);
    const router = inject(Router);

    if (!auth.isAuthenticated() && !(await auth.restore())) {
      return router.createUrlTree(['/sign-in']);
    }

    return auth.hasAny(...permissions) ? true : router.createUrlTree(['/forbidden']);
  };
}

/** Sends an already signed-in user away from the sign-in screen. */
export const anonymousOnlyGuard: CanActivateFn = async () => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  if (auth.isAuthenticated() || (await auth.restore())) {
    return router.createUrlTree(['/items']);
  }

  return true;
};
