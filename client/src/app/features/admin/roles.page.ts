import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { isAppError } from '../../core/errors/app-error';
import { TransportService } from '../../core/transport/transport.service';
import { UsersGateway } from '../../data-access/gateways/users.gateway';
import { LoadState } from '../../shared/components/load-state/load-state';

/** The roles and the permissions each of them grants. */
@Component({
  selector: 'app-roles-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LoadState,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatTabsModule,
    MatTooltipModule,
    RouterLink,
  ],
  template: `
    <nav mat-tab-nav-bar [tabPanel]="panel" aria-label="Administration areas">
      <a mat-tab-link routerLink="/administration/users" [active]="false">Users</a>
      <a mat-tab-link routerLink="/administration/roles" [active]="true">Roles</a>
    </nav>
    <mat-tab-nav-panel #panel>
      <header class="page-header">
        <div>
          <h1>Roles</h1>
          <p class="subtitle">
            A role is a bundle of permissions. Use cases name the permission they need, never the
            role, so re-bundling here takes effect without a redeployment.
          </p>
        </div>
      </header>

      <app-load-state
        [loading]="roles.isLoading()"
        [error]="failure()"
        [empty]="!roles.isLoading() && !failure() && (roles.value() ?? []).length === 0"
        emptyMessage="No roles are defined."
        (retry)="roles.reload()"
      />

      <div class="role-grid">
        @for (role of roles.value() ?? []; track role.id) {
          <mat-card [attr.data-testid]="'role-' + role.name">
            <mat-card-header>
              <mat-card-title>
                {{ role.name }}
                @if (role.isSystemRole) {
                  <mat-icon
                    class="system-badge"
                    aria-label="Ships with the platform"
                    matTooltip="Ships with the platform"
                    >verified</mat-icon
                  >
                }
              </mat-card-title>
              <mat-card-subtitle>
                {{ role.description }} · held by {{ role.userCount }} users
              </mat-card-subtitle>
            </mat-card-header>

            <mat-card-content>
              <mat-chip-set aria-label="Granted permissions">
                @for (permission of role.permissions; track permission.id) {
                  <mat-chip [matTooltip]="permission.description">{{ permission.name }}</mat-chip>
                }
              </mat-chip-set>
            </mat-card-content>
          </mat-card>
        }
      </div>
    </mat-tab-nav-panel>
  `,
  styles: `
    .role-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(20rem, 1fr));
      gap: 1rem;
    }

    .system-badge { font-size: 1rem; height: 1rem; width: 1rem; vertical-align: middle; }
  `,
})
export class RolesPage {
  private readonly users = inject(UsersGateway);
  private readonly transport = inject(TransportService);

  /** The roles and their permissions; re-runs when the transport changes. */
  protected readonly roles = rxResource({
    params: () => ({ transport: this.transport.active() }),
    stream: () => this.users.roles(),
  });

  /** The normalized failure of the last load, when it failed. */
  protected readonly failure = computed(() => {
    const error = this.roles.error();
    return isAppError(error) ? error : null;
  });
}
