import { inject, Injectable } from '@angular/core';
import { Apollo } from 'apollo-angular';
import type { Observable } from 'rxjs';
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
import { runMutation, runQuery } from './graphql-execution';
import {
  CREATE_ROLE,
  CREATE_USER,
  DELETE_ROLE,
  PERMISSIONS,
  ROLES,
  SEARCH_USERS,
  SET_USER_ACTIVE,
  UPDATE_ROLE,
  UPDATE_USER,
} from './operation.documents';

interface RawPage<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

/** The GraphQL implementation of {@link UsersGateway}. */
@Injectable({ providedIn: 'root' })
export class GraphQlUsersGateway extends UsersGateway {
  private readonly apollo = inject(Apollo);
  private readonly metrics = inject(MetricsCollector);

  /** @inheritdoc */
  override search(query: UserQuery): Observable<PagedResult<User>> {
    const criteria = {
      page: query.page,
      pageSize: query.pageSize,
      search: query.search ?? null,
      sortBy: query.sortBy ?? null,
      sortDescending: query.sortDescending ?? false,
      isActive: query.isActive ?? null,
      roleId: query.roleId ?? null,
    };

    return measured(this.metrics, 'graphql', 'users.search', 1, () =>
      runQuery<{ searchUsers: RawPage<User> }, PagedResult<User>>(
        this.apollo,
        SEARCH_USERS,
        (data) =>
          toPagedResult(
            data.searchUsers.items,
            data.searchUsers.totalCount,
            data.searchUsers.page,
            data.searchUsers.pageSize,
          ),
        { criteria },
      ),
    );
  }

  /** @inheritdoc */
  override getById(id: string): Observable<User> {
    return measured(this.metrics, 'graphql', 'users.getById', 1, () =>
      runQuery<{ searchUsers: RawPage<User> }, User>(
        this.apollo,
        SEARCH_USERS,
        (data) => {
          const match = data.searchUsers.items.find((user) => user.id === id);
          if (!match) {
            throw { code: 'user.not_found', message: 'The user does not exist.', kind: 'notFound' };
          }
          return match;
        },
        { criteria: { page: 1, pageSize: 100, search: null, sortDescending: false } },
      ),
    );
  }

  /** @inheritdoc */
  override create(draft: UserDraft): Observable<string> {
    return measured(this.metrics, 'graphql', 'users.create', 1, () =>
      runMutation<{ createUser: string }, string>(
        this.apollo,
        CREATE_USER,
        (data) => data.createUser,
        { input: { ...draft, roleIds: [...draft.roleIds] } },
      ),
    );
  }

  /** @inheritdoc */
  override update(id: string, update: UserUpdate): Observable<void> {
    return measured(this.metrics, 'graphql', 'users.update', 1, () =>
      runMutation<{ updateUser: boolean }, void>(this.apollo, UPDATE_USER, () => undefined, {
        input: { userId: id, ...update, roleIds: [...update.roleIds] },
      }),
    );
  }

  /** @inheritdoc */
  override setActive(id: string, isActive: boolean): Observable<void> {
    return measured(this.metrics, 'graphql', 'users.setActive', 1, () =>
      runMutation<{ setUserActive: boolean }, void>(
        this.apollo,
        SET_USER_ACTIVE,
        () => undefined,
        { input: { userId: id, isActive } },
      ),
    );
  }

  /** @inheritdoc */
  override roles(): Observable<readonly Role[]> {
    return measured(this.metrics, 'graphql', 'roles.list', 1, () =>
      runQuery<{ roles: readonly Role[] }, readonly Role[]>(
        this.apollo,
        ROLES,
        (data) => data.roles,
      ),
    );
  }

  /** @inheritdoc */
  override permissions(): Observable<readonly Permission[]> {
    return measured(this.metrics, 'graphql', 'permissions.list', 1, () =>
      runQuery<{ permissions: readonly Permission[] }, readonly Permission[]>(
        this.apollo,
        PERMISSIONS,
        (data) => data.permissions,
      ),
    );
  }

  /** @inheritdoc */
  override createRole(draft: RoleDraft): Observable<string> {
    return measured(this.metrics, 'graphql', 'roles.create', 1, () =>
      runMutation<{ createRole: string }, string>(
        this.apollo,
        CREATE_ROLE,
        (data) => data.createRole,
        { input: { ...draft, permissionNames: [...draft.permissionNames] } },
      ),
    );
  }

  /** @inheritdoc */
  override updateRole(id: string, draft: RoleDraft): Observable<void> {
    return measured(this.metrics, 'graphql', 'roles.update', 1, () =>
      runMutation<{ updateRole: boolean }, void>(this.apollo, UPDATE_ROLE, () => undefined, {
        input: { roleId: id, ...draft, permissionNames: [...draft.permissionNames] },
      }),
    );
  }

  /** @inheritdoc */
  override removeRole(id: string): Observable<void> {
    return measured(this.metrics, 'graphql', 'roles.delete', 1, () =>
      runMutation<{ deleteRole: boolean }, void>(this.apollo, DELETE_ROLE, () => undefined, {
        roleId: id,
      }),
    );
  }
}
