import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MetricsCollector } from '../../core/metrics/metrics-collector';
import { toPagedResult, type PagedResult } from '../../shared/models/paging.models';
import type {
  Permission,
  Role,
  RoleDraft,
  User,
  UserDraft,
  UserQuery,
  UserUpdate,
} from '../../shared/models/user.models';
import { UsersGateway } from '../gateways/users.gateway';
import { measured } from '../measurement';

interface RestPage<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

/** The REST implementation of {@link UsersGateway}. */
@Injectable({ providedIn: 'root' })
export class RestUsersGateway extends UsersGateway {
  private readonly http = inject(HttpClient);
  private readonly metrics = inject(MetricsCollector);
  private readonly usersUrl = `${environment.restBaseUrl}/users`;
  private readonly rolesUrl = `${environment.restBaseUrl}/roles`;

  /** @inheritdoc */
  override search(query: UserQuery): Observable<PagedResult<User>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.sortDescending) {
      params = params.set('sortDescending', true);
    }
    if (query.isActive !== undefined) {
      params = params.set('isActive', query.isActive);
    }
    if (query.roleId) {
      params = params.set('roleId', query.roleId);
    }

    return measured(this.metrics, 'rest', 'users.search', 1, () =>
      this.http
        .get<RestPage<User>>(this.usersUrl, { params })
        .pipe(map((page) => toPagedResult(page.items, page.totalCount, page.page, page.pageSize))),
    );
  }

  /** @inheritdoc */
  override getById(id: string): Observable<User> {
    return measured(this.metrics, 'rest', 'users.getById', 1, () =>
      this.http.get<User>(`${this.usersUrl}/${id}`),
    );
  }

  /** @inheritdoc */
  override create(draft: UserDraft): Observable<string> {
    return measured(this.metrics, 'rest', 'users.create', 1, () =>
      this.http.post<{ id: string }>(this.usersUrl, draft).pipe(map((created) => created.id)),
    );
  }

  /** @inheritdoc */
  override update(id: string, update: UserUpdate): Observable<void> {
    return measured(this.metrics, 'rest', 'users.update', 1, () =>
      this.http.put<void>(`${this.usersUrl}/${id}`, { userId: id, ...update }),
    );
  }

  /** @inheritdoc */
  override setActive(id: string, isActive: boolean): Observable<void> {
    return measured(this.metrics, 'rest', 'users.setActive', 1, () =>
      this.http.put<void>(`${this.usersUrl}/${id}/active`, null, {
        params: new HttpParams().set('isActive', isActive),
      }),
    );
  }

  /** @inheritdoc */
  override roles(): Observable<readonly Role[]> {
    return measured(this.metrics, 'rest', 'roles.list', 1, () =>
      this.http.get<readonly Role[]>(this.rolesUrl),
    );
  }

  /** @inheritdoc */
  override permissions(): Observable<readonly Permission[]> {
    return measured(this.metrics, 'rest', 'permissions.list', 1, () =>
      this.http.get<readonly Permission[]>(`${environment.restBaseUrl}/permissions`),
    );
  }

  /** @inheritdoc */
  override createRole(draft: RoleDraft): Observable<string> {
    return measured(this.metrics, 'rest', 'roles.create', 1, () =>
      this.http.post<{ id: string }>(this.rolesUrl, draft).pipe(map((created) => created.id)),
    );
  }

  /** @inheritdoc */
  override updateRole(id: string, draft: RoleDraft): Observable<void> {
    return measured(this.metrics, 'rest', 'roles.update', 1, () =>
      this.http.put<void>(`${this.rolesUrl}/${id}`, { roleId: id, ...draft }),
    );
  }

  /** @inheritdoc */
  override removeRole(id: string): Observable<void> {
    return measured(this.metrics, 'rest', 'roles.delete', 1, () =>
      this.http.delete<void>(`${this.rolesUrl}/${id}`),
    );
  }
}
