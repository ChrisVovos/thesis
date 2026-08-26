import { inject, Injectable } from '@angular/core';
import { Apollo } from 'apollo-angular';
import { map, type Observable } from 'rxjs';
import { MetricsCollector } from '../../core/metrics/metrics-collector';
import type {
  AuthenticationResult,
  Credentials,
  CurrentUser,
} from '../../shared/models/auth.models';
import { AuthGateway } from '../gateways/auth.gateway';
import { measured } from '../measurement';
import { runMutation, runQuery } from './graphql-execution';
import { LOGIN, LOGOUT, ME, REFRESH_TOKEN } from './operation.documents';

interface AuthenticationPayload {
  readonly accessToken: string;
  readonly accessTokenExpiresAtUtc: string;
  readonly refreshToken: string;
  readonly refreshTokenExpiresAtUtc: string;
  readonly user: CurrentUser;
}

/** The GraphQL implementation of {@link AuthGateway}. */
@Injectable({ providedIn: 'root' })
export class GraphQlAuthGateway extends AuthGateway {
  private readonly apollo = inject(Apollo);
  private readonly metrics = inject(MetricsCollector);

  /** @inheritdoc */
  override signIn(credentials: Credentials): Observable<AuthenticationResult> {
    return measured(this.metrics, 'graphql', 'auth.signIn', 1, () =>
      runMutation<{ login: AuthenticationPayload }, AuthenticationResult>(
        this.apollo,
        LOGIN,
        (data) => data.login,
        { input: credentials },
      ),
    );
  }

  /** @inheritdoc */
  override refresh(refreshToken: string): Observable<AuthenticationResult> {
    return measured(this.metrics, 'graphql', 'auth.refresh', 1, () =>
      runMutation<{ refreshToken: AuthenticationPayload }, AuthenticationResult>(
        this.apollo,
        REFRESH_TOKEN,
        (data) => data.refreshToken,
        { refreshToken },
      ),
    );
  }

  /** @inheritdoc */
  override signOut(refreshToken: string): Observable<void> {
    return measured(this.metrics, 'graphql', 'auth.signOut', 1, () =>
      runMutation<{ logout: boolean }, void>(this.apollo, LOGOUT, () => undefined, {
        refreshToken,
      }),
    );
  }

  /** @inheritdoc */
  override currentUser(): Observable<CurrentUser> {
    return measured(this.metrics, 'graphql', 'auth.currentUser', 1, () =>
      runQuery<{ me: CurrentUser }, CurrentUser>(this.apollo, ME, (data) => data.me).pipe(
        map((user) => user),
      ),
    );
  }
}
