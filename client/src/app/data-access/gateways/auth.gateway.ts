import type { Observable } from 'rxjs';
import type {
  AuthenticationResult,
  Credentials,
  CurrentUser,
} from '../../shared/models/auth.models';

/**
 * The authentication contract, expressed purely in domain terms.
 *
 * No method signature mentions HTTP, GraphQL, a URL or a document. That is the rule the whole
 * comparison rests on: a component that injects this class cannot tell which transport serves it, so
 * the same screen and the same user actions can be measured over both.
 */
export abstract class AuthGateway {
  /**
   * Exchanges credentials for a session.
   *
   * @param credentials The credentials to present.
   */
  abstract signIn(credentials: Credentials): Observable<AuthenticationResult>;

  /**
   * Exchanges a refresh token for a new session.
   *
   * @param refreshToken The refresh token held by the client.
   */
  abstract refresh(refreshToken: string): Observable<AuthenticationResult>;

  /**
   * Revokes a session.
   *
   * @param refreshToken The refresh token held by the client.
   */
  abstract signOut(refreshToken: string): Observable<void>;

  /** Reads the profile and permissions of the caller. */
  abstract currentUser(): Observable<CurrentUser>;
}
