import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MetricsCollector } from '../../core/metrics/metrics-collector';
import type {
  AuthenticationResult,
  Credentials,
  CurrentUser,
} from '../../shared/models/auth.models';
import { AuthGateway } from '../gateways/auth.gateway';
import { measured } from '../measurement';

/**
 * The REST implementation of {@link AuthGateway}.
 *
 * The REST wire format for these operations already matches the shared view models, so mapping is a
 * no-op. That is itself a finding for the comparison: a resource shaped exactly for one screen costs
 * the client nothing to consume, and costs the server an endpoint per screen.
 */
@Injectable({ providedIn: 'root' })
export class RestAuthGateway extends AuthGateway {
  private readonly http = inject(HttpClient);
  private readonly metrics = inject(MetricsCollector);
  private readonly baseUrl = `${environment.restBaseUrl}/auth`;

  /** @inheritdoc */
  override signIn(credentials: Credentials): Observable<AuthenticationResult> {
    return measured(this.metrics, 'rest', 'auth.signIn', 1, () =>
      this.http.post<AuthenticationResult>(`${this.baseUrl}/login`, credentials),
    );
  }

  /** @inheritdoc */
  override refresh(refreshToken: string): Observable<AuthenticationResult> {
    return measured(this.metrics, 'rest', 'auth.refresh', 1, () =>
      this.http.post<AuthenticationResult>(`${this.baseUrl}/refresh`, { refreshToken }),
    );
  }

  /** @inheritdoc */
  override signOut(refreshToken: string): Observable<void> {
    return measured(this.metrics, 'rest', 'auth.signOut', 1, () =>
      this.http.post<void>(`${this.baseUrl}/logout`, { refreshToken }),
    );
  }

  /** @inheritdoc */
  override currentUser(): Observable<CurrentUser> {
    return measured(this.metrics, 'rest', 'auth.currentUser', 1, () =>
      this.http.get<CurrentUser>(`${this.baseUrl}/me`).pipe(map((user) => user)),
    );
  }
}
