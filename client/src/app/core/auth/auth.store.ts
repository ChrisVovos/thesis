import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthGateway } from '../../data-access/gateways/auth.gateway';
import type {
  AuthenticationResult,
  Credentials,
  CurrentUser,
  PermissionName,
} from '../../shared/models/auth.models';
import { TokenStorage } from './token-storage';

/**
 * The signed-in session.
 *
 * The store is transport agnostic: it depends on the abstract {@link AuthGateway} and therefore signs
 * in over whichever surface is currently selected, without knowing which that is.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly gateway = inject(AuthGateway);
  private readonly storage = inject(TokenStorage);
  private readonly router = inject(Router);

  private readonly currentUser = signal<CurrentUser | null>(null);
  private readonly signingIn = signal(false);

  /** The signed-in user, or `null` when the session is anonymous. */
  readonly user = this.currentUser.asReadonly();

  /** Whether a sign-in is currently in flight. */
  readonly busy = this.signingIn.asReadonly();

  /** Whether a token is held. */
  readonly isAuthenticated = computed(() => this.currentUser() !== null);

  /** The permissions held by the signed-in user. */
  readonly permissions = computed(() => new Set(this.currentUser()?.permissions ?? []));

  /** The roles held by the signed-in user. */
  readonly roles = computed(() => new Set(this.currentUser()?.roles ?? []));

  /** The access token to attach to outgoing requests, when one is held. */
  get accessToken(): string | null {
    return this.storage.read()?.accessToken ?? null;
  }

  /** The refresh token held for this session, when one is held. */
  get refreshToken(): string | null {
    return this.storage.read()?.refreshToken ?? null;
  }

  /**
   * Determines whether the signed-in user holds a permission.
   *
   * @param permission The permission to test for.
   * @returns `true` when the permission is held.
   */
  has(permission: PermissionName): boolean {
    return this.permissions().has(permission);
  }

  /**
   * Determines whether the signed-in user holds any of the supplied permissions.
   *
   * @param permissions The permissions to test for.
   * @returns `true` when at least one is held.
   */
  hasAny(...permissions: readonly PermissionName[]): boolean {
    return permissions.some((permission) => this.has(permission));
  }

  /**
   * Signs in and stores the resulting session.
   *
   * @param credentials The credentials to present.
   * @returns The authenticated profile.
   */
  async signIn(credentials: Credentials): Promise<CurrentUser> {
    this.signingIn.set(true);
    try {
      const result = await firstValueFrom(this.gateway.signIn(credentials));
      this.accept(result);
      return result.user;
    } finally {
      this.signingIn.set(false);
    }
  }

  /**
   * Restores a session from storage, so a reload does not sign the user out.
   *
   * @returns `true` when a session was restored.
   */
  async restore(): Promise<boolean> {
    if (!this.storage.read()) {
      return false;
    }

    try {
      this.currentUser.set(await firstValueFrom(this.gateway.currentUser()));
      return true;
    } catch {
      this.storage.clear();
      this.currentUser.set(null);
      return false;
    }
  }

  /**
   * Exchanges the stored refresh token for a new session.
   *
   * @returns The new access token, or `null` when the session could not be renewed.
   */
  async refresh(): Promise<string | null> {
    const token = this.refreshToken;
    if (!token) {
      return null;
    }

    try {
      const result = await firstValueFrom(this.gateway.refresh(token));
      this.accept(result);
      return result.accessToken;
    } catch {
      await this.signOut();
      return null;
    }
  }

  /** Revokes the session and returns to the sign-in screen. */
  async signOut(): Promise<void> {
    const token = this.refreshToken;
    this.storage.clear();
    this.currentUser.set(null);

    if (token) {
      try {
        await firstValueFrom(this.gateway.signOut(token));
      } catch {
        // Sign-out is best effort: the client side session is already gone.
      }
    }

    await this.router.navigate(['/sign-in']);
  }

  private accept(result: AuthenticationResult): void {
    this.storage.write(result);
    this.currentUser.set(result.user);
  }
}
