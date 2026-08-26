import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { environment } from '../../environments/environment';
import { AuthStore } from '../core/auth/auth.store';
import { BusyService } from '../core/notifications/notification.service';
import { Permissions } from '../shared/models/auth.models';
import { TransportSelector } from '../shared/components/transport-selector/transport-selector';

/**
 * The application shell: navigation, the busy indicator, the user menu and the transport selector.
 *
 * The selector is rendered here and nowhere else, exactly once, so no feature screen can offer a
 * second copy that disagrees with this one.
 */
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatDividerModule,
    MatIconModule,
    MatMenuModule,
    MatProgressBarModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    TransportSelector,
  ],
  template: `
    <mat-toolbar class="shell-toolbar">
      <span class="brand">Item Authoring</span>

      <nav class="shell-nav" aria-label="Primary">
        @if (canReadItems()) {
          <a matButton routerLink="/items" routerLinkActive="active-link">Items</a>
        }
        @if (canReadExams()) {
          <a matButton routerLink="/exams" routerLinkActive="active-link">Exams</a>
        }
        @if (canAdminister()) {
          <a matButton routerLink="/administration" routerLinkActive="active-link">Administration</a>
        }
        @if (showBenchmark) {
          <a matButton routerLink="/benchmark" routerLinkActive="active-link">Benchmark</a>
        }
      </nav>

      <span class="spacer"></span>

      <app-transport-selector />

      <button matIconButton [matMenuTriggerFor]="userMenu" [attr.aria-label]="userLabel()">
        <mat-icon>account_circle</mat-icon>
      </button>
      <mat-menu #userMenu="matMenu">
        <div class="user-summary">
          <strong>{{ user()?.displayName }}</strong>
          <small>{{ user()?.email }}</small>
          <small>{{ roleSummary() }}</small>
        </div>
        <mat-divider />
        <button mat-menu-item type="button" data-testid="sign-out" (click)="signOut()">
          <mat-icon>logout</mat-icon>
          <span>Sign out</span>
        </button>
      </mat-menu>
    </mat-toolbar>

    <div class="progress-slot" aria-hidden="true">
      @if (busy.isBusy()) {
        <mat-progress-bar mode="indeterminate" data-testid="global-busy" />
      }
    </div>

    <main class="shell-content">
      <router-outlet />
    </main>
  `,
  styles: `
    .shell-toolbar {
      gap: 0.5rem;
      position: sticky;
      top: 0;
      z-index: 10;
    }

    .brand { font-weight: 600; margin-right: 1.5rem; }
    .shell-nav { display: flex; gap: 0.25rem; }
    .active-link { font-weight: 700; text-decoration: underline; }
    .spacer { flex: 1 1 auto; }
    .progress-slot { height: 4px; }
    .shell-content { padding: 1.5rem; max-width: 1400px; margin: 0 auto; }

    .user-summary {
      display: flex;
      flex-direction: column;
      padding: 0.75rem 1rem;
      gap: 0.15rem;
    }
  `,
})
export class Shell {
  private readonly auth = inject(AuthStore);

  /** Tracks whether any request is in flight. */
  protected readonly busy = inject(BusyService);

  /** The signed-in user. */
  protected readonly user = this.auth.user;

  /** Whether the benchmark screen is offered by this build. */
  protected readonly showBenchmark = environment.showTransportSelector;

  /** Whether the item bank is reachable by the signed-in user. */
  protected readonly canReadItems = computed(() => this.auth.has(Permissions.ItemsRead));

  /** Whether the exam builder is reachable by the signed-in user. */
  protected readonly canReadExams = computed(() => this.auth.has(Permissions.ExamsRead));

  /** Whether the administration area is reachable by the signed-in user. */
  protected readonly canAdminister = computed(() =>
    this.auth.hasAny(Permissions.UsersRead, Permissions.RolesManage),
  );

  /** The accessible label of the user menu. */
  protected readonly userLabel = computed(
    () => `Account menu for ${this.user()?.displayName ?? 'the current user'}`,
  );

  /** The roles held by the signed-in user, as a sentence. */
  protected readonly roleSummary = computed(() => this.user()?.roles.join(', ') ?? '');

  /** Ends the session. */
  protected signOut(): void {
    void this.auth.signOut();
  }
}
