import { Injectable } from '@angular/core';
import type { AuthenticationResult } from '../../shared/models/auth.models';

/** The persisted part of a session. */
export interface StoredSession {
  readonly accessToken: string;
  readonly refreshToken: string;
  readonly accessTokenExpiresAtUtc: string;
}

/**
 * Reads and writes the session to browser storage.
 *
 * Tokens live in `localStorage` rather than in a cookie because the client and the API are separate
 * origins during development and the API is stateless by design. The trade-off — a cross-site
 * scripting flaw could read the token — is mitigated by a fifteen minute access token lifetime,
 * refresh token rotation, and the fact that the client renders no user supplied HTML.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorage {
  private static readonly Key = 'item-authoring-session';

  /**
   * Reads the stored session.
   *
   * @returns The session, or `null` when none is stored or the stored value is unusable.
   */
  read(): StoredSession | null {
    try {
      const raw = localStorage.getItem(TokenStorage.Key);
      if (!raw) {
        return null;
      }

      const parsed = JSON.parse(raw) as Partial<StoredSession>;
      return parsed.accessToken && parsed.refreshToken && parsed.accessTokenExpiresAtUtc
        ? (parsed as StoredSession)
        : null;
    } catch {
      this.clear();
      return null;
    }
  }

  /**
   * Stores the tokens from a sign-in or a refresh.
   *
   * @param result The authentication result to persist.
   */
  write(result: AuthenticationResult): void {
    const session: StoredSession = {
      accessToken: result.accessToken,
      refreshToken: result.refreshToken,
      accessTokenExpiresAtUtc: result.accessTokenExpiresAtUtc,
    };

    try {
      localStorage.setItem(TokenStorage.Key, JSON.stringify(session));
    } catch {
      // A browser with storage disabled still works for the lifetime of the tab.
    }
  }

  /** Discards the stored session. */
  clear(): void {
    try {
      localStorage.removeItem(TokenStorage.Key);
    } catch {
      // Nothing to do; the session simply was not stored.
    }
  }
}
