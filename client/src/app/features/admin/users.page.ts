import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthStore } from '../../core/auth/auth.store';
import { isAppError } from '../../core/errors/app-error';
import { NotificationService } from '../../core/notifications/notification.service';
import { TransportService } from '../../core/transport/transport.service';
import { UsersGateway } from '../../data-access/gateways/users.gateway';
import { LoadState } from '../../shared/components/load-state/load-state';
import { Permissions } from '../../shared/models/auth.models';
import type { User, UserQuery } from '../../shared/models/user.models';

/** The user directory. */
@Component({
  selector: 'app-users-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    FormsModule,
    LoadState,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatTableModule,
    MatTabsModule,
    RouterLink,
  ],
  template: `
    <nav mat-tab-nav-bar [tabPanel]="panel" aria-label="Administration areas">
      <a mat-tab-link routerLink="/administration/users" [active]="true">Users</a>
      <a mat-tab-link routerLink="/administration/roles" [active]="false">Roles</a>
    </nav>
    <mat-tab-nav-panel #panel>
      <header class="page-header">
        <div>
          <h1>Users</h1>
          <p class="subtitle">{{ totalCount() }} accounts.</p>
        </div>

        @if (canManage()) {
          <button
            matButton="filled"
            type="button"
            data-testid="new-user"
            [attr.aria-expanded]="showCreate()"
            (click)="toggleCreate()"
          >
            <mat-icon>{{ showCreate() ? 'close' : 'add' }}</mat-icon>
            {{ showCreate() ? 'Cancel' : 'New user' }}
          </button>
        }
      </header>

      @if (canManage() && showCreate()) {
        <mat-card class="create">
          <mat-card-content>
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Display name</mat-label>
              <input
                matInput
                data-testid="new-user-name"
                [ngModel]="draftName()"
                (ngModelChange)="draftName.set($event)"
              />
            </mat-form-field>

            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>E-mail address</mat-label>
              <input
                matInput
                type="email"
                autocomplete="off"
                data-testid="new-user-email"
                [ngModel]="draftEmail()"
                (ngModelChange)="draftEmail.set($event)"
              />
            </mat-form-field>

            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Password</mat-label>
              <input
                matInput
                type="password"
                autocomplete="new-password"
                data-testid="new-user-password"
                [ngModel]="draftPassword()"
                (ngModelChange)="draftPassword.set($event)"
              />
            </mat-form-field>

            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Roles</mat-label>
              <mat-select
                multiple
                required
                data-testid="new-user-roles"
                [ngModel]="draftRoleIds()"
                (ngModelChange)="draftRoleIds.set($event)"
              >
                @for (role of roles.value() ?? []; track role.id) {
                  <mat-option [value]="role.id">{{ role.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>

            <button
              matButton="filled"
              type="button"
              data-testid="create-user"
              [disabled]="!canSubmit()"
              (click)="createUser()"
            >
              Create account
            </button>
          </mat-card-content>
        </mat-card>
      }

      <mat-card class="filters">
        <mat-card-content>
          <mat-form-field appearance="outline" subscriptSizing="dynamic">
            <mat-label>Search</mat-label>
            <input
              matInput
              type="search"
              data-testid="user-search"
              [ngModel]="criteria().search ?? ''"
              (ngModelChange)="search($event)"
            />
            <mat-icon matSuffix>search</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" subscriptSizing="dynamic">
            <mat-label>Role</mat-label>
            <mat-select
              data-testid="user-filter-role"
              [ngModel]="criteria().roleId ?? null"
              (ngModelChange)="filterRole($event)"
            >
              <mat-option [value]="null">All roles</mat-option>
              @for (role of roles.value() ?? []; track role.id) {
                <mat-option [value]="role.id">{{ role.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        </mat-card-content>
      </mat-card>

      <app-load-state
        [loading]="page.isLoading()"
        [error]="failure()"
        [empty]="!page.isLoading() && !failure() && rows().length === 0"
        emptyMessage="No accounts match the current filters."
        (retry)="page.reload()"
      />

      @if (rows().length > 0) {
        <mat-card class="results">
          <table mat-table [dataSource]="rows()" data-testid="user-table">
            <ng-container matColumnDef="displayName">
              <th mat-header-cell *matHeaderCellDef>Name</th>
              <td mat-cell *matCellDef="let user">{{ user.displayName }}</td>
            </ng-container>

            <ng-container matColumnDef="email">
              <th mat-header-cell *matHeaderCellDef>E-mail</th>
              <td mat-cell *matCellDef="let user">{{ user.email }}</td>
            </ng-container>

            <ng-container matColumnDef="roles">
              <th mat-header-cell *matHeaderCellDef>Roles</th>
              <td mat-cell *matCellDef="let user">{{ roleNames(user) }}</td>
            </ng-container>

            <ng-container matColumnDef="lastSignIn">
              <th mat-header-cell *matHeaderCellDef>Last sign-in</th>
              <td mat-cell *matCellDef="let user">
                {{ user.lastSignInAtUtc ? (user.lastSignInAtUtc | date: 'short') : 'Never' }}
              </td>
            </ng-container>

            <ng-container matColumnDef="active">
              <th mat-header-cell *matHeaderCellDef>Active</th>
              <td mat-cell *matCellDef="let user">
                <mat-slide-toggle
                  [checked]="user.isActive"
                  [disabled]="!canManage()"
                  [attr.data-testid]="'user-active-' + user.id"
                  [attr.aria-label]="'Account active for ' + user.displayName"
                  (change)="setActive(user, $event.checked)"
                />
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="columns"></tr>
            <tr mat-row *matRowDef="let row; columns: columns"></tr>
          </table>

          <mat-paginator
            [length]="totalCount()"
            [pageIndex]="criteria().page - 1"
            [pageSize]="criteria().pageSize"
            [pageSizeOptions]="[10, 20, 50]"
            (page)="changePage($event)"
            aria-label="Select a page of users"
          />
        </mat-card>
      }
    </mat-tab-nav-panel>
  `,
  styles: `
    .create {
      margin-bottom: 1rem;

      mat-card-content {
        display: flex;
        gap: 0.75rem;
        flex-wrap: wrap;
        align-items: center;
      }

      mat-form-field { min-width: 14rem; }
    }
  `,
})
export class UsersPage {
  private readonly users = inject(UsersGateway);
  private readonly notifications = inject(NotificationService);
  private readonly auth = inject(AuthStore);
  private readonly transport = inject(TransportService);

  protected readonly columns = ['displayName', 'email', 'roles', 'lastSignIn', 'active'];

  /** The criteria currently applied. */
  protected readonly criteria = signal<UserQuery>({ page: 1, pageSize: 20 });

  /** Whether the signed-in user may change accounts. */
  protected readonly canManage = computed(() => this.auth.has(Permissions.UsersManage));

  /** Whether the new account form is open. */
  protected readonly showCreate = signal(false);

  protected readonly draftName = signal('');
  protected readonly draftEmail = signal('');
  protected readonly draftPassword = signal('');
  protected readonly draftRoleIds = signal<readonly string[]>([]);
  private readonly creating = signal(false);

  /** Whether the draft account is complete enough to submit. */
  protected readonly canSubmit = computed(
    () =>
      !this.creating() &&
      this.draftName().trim().length > 0 &&
      this.draftEmail().trim().length > 0 &&
      this.draftPassword().length > 0 &&
      this.draftRoleIds().length > 0,
  );

  /** The current page of the directory; re-runs when the transport changes. */
  protected readonly page = rxResource({
    params: () => ({ criteria: this.criteria(), transport: this.transport.active() }),
    stream: ({ params }) => this.users.search(params.criteria),
  });

  /** The roles available as a filter. */
  protected readonly roles = rxResource({
    params: () => ({ transport: this.transport.active() }),
    stream: () => this.users.roles(),
  });

  protected readonly rows = computed(() => this.page.value()?.items ?? []);
  protected readonly totalCount = computed(() => this.page.value()?.totalCount ?? 0);
  protected readonly failure = computed(() => {
    const error = this.page.error();
    return isAppError(error) ? error : null;
  });

  /** Renders the roles of a user as a sentence. */
  protected roleNames(user: User): string {
    return user.roles.map((role) => role.name).join(', ');
  }

  /** Opens or abandons the new account form. */
  protected toggleCreate(): void {
    const open = !this.showCreate();
    this.showCreate.set(open);
    if (!open) {
      this.resetDraft();
    }
  }

  /** Creates the drafted account over whichever transport is active. */
  protected async createUser(): Promise<void> {
    if (!this.canSubmit()) {
      return;
    }

    this.creating.set(true);
    try {
      await firstValueFrom(
        this.users.create({
          email: this.draftEmail().trim(),
          displayName: this.draftName().trim(),
          password: this.draftPassword(),
          roleIds: this.draftRoleIds(),
        }),
      );

      this.notifications.success('The account was created.');
      this.resetDraft();
      this.showCreate.set(false);
      this.page.reload();
    } catch (error: unknown) {
      this.notifications.failure(
        isAppError(error)
          ? error
          : {
              code: 'client.unexpected',
              message: 'The account could not be created.',
              kind: 'failure',
            },
      );
    } finally {
      this.creating.set(false);
    }
  }

  /** Applies a new free-text search term. */
  protected search(term: string): void {
    this.criteria.update((current) => ({ ...current, search: term || undefined, page: 1 }));
  }

  /** Restricts the result to one role. */
  protected filterRole(roleId: string | null): void {
    this.criteria.update((current) => ({ ...current, roleId: roleId ?? undefined, page: 1 }));
  }

  /** Moves to another page. */
  protected changePage(event: PageEvent): void {
    this.criteria.update((current) => ({
      ...current,
      page: event.pageIndex + 1,
      pageSize: event.pageSize,
    }));
  }

  /**
   * Activates or deactivates an account.
   *
   * @param user The account to change.
   * @param isActive Whether the user may sign in.
   */
  protected async setActive(user: User, isActive: boolean): Promise<void> {
    try {
      await firstValueFrom(this.users.setActive(user.id, isActive));
      this.notifications.success('The account was updated.');
      this.page.reload();
    } catch (error: unknown) {
      this.notifications.failure(
        isAppError(error)
          ? error
          : { code: 'client.unexpected', message: 'The account could not be updated.', kind: 'failure' },
      );
      this.page.reload();
    }
  }

  private resetDraft(): void {
    this.draftName.set('');
    this.draftEmail.set('');
    this.draftPassword.set('');
    this.draftRoleIds.set([]);
  }
}
