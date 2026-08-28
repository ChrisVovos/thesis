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
          <a matButton routerLink="/launch" routerLinkActive="active-link">Launch</a>
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
      height: 64px;
      padding-inline: 1.5rem;
      background: var(--app-shell-bg);
      color: #fff;
      box-shadow: 0 1px 12px rgba(15, 23, 42, 0.25);
    }

    .brand {
      font-size: 1.0625rem;
      font-weight: 700;
      letter-spacing: -0.01em;
      margin-right: 2rem;
      color: #fff;
    }

    .shell-nav {
      display: flex;
      gap: 0.25rem;
    }

    .shell-nav a {
      border-radius: 999px;
      font-weight: 500;
      color: rgba(255, 255, 255, 0.78);
    }

    .shell-nav a:hover {
      color: #fff;
      background: rgba(255, 255, 255, 0.1);
    }

    .shell-nav a.active-link {
      color: #fff;
      font-weight: 600;
      background: rgba(255, 255, 255, 0.18);
    }

    .shell-toolbar button {
      color: rgba(255, 255, 255, 0.85);
    }

    /* The selector sits on the dark bar, so its Material colours are re-pointed at light ones. */
    .shell-toolbar app-transport-selector {
      --mdc-outlined-text-field-outline-color: rgba(255, 255, 255, 0.35);
      --mdc-outlined-text-field-hover-outline-color: rgba(255, 255, 255, 0.6);
      --mdc-outlined-text-field-focus-outline-color: #fff;
      --mdc-outlined-text-field-input-text-color: #fff;
      --mat-form-field-outlined-label-text-color: rgba(255, 255, 255, 0.75);
      --mat-form-field-outlined-hover-label-text-color: #fff;
      --mat-form-field-outlined-focus-label-text-color: #fff;
      --mat-select-enabled-trigger-text-color: #fff;
      --mat-select-enabled-arrow-color: rgba(255, 255, 255, 0.75);
      --mat-select-focused-arrow-color: #fff;
    }

    .spacer { flex: 1 1 auto; }

    /* Reserved for the busy bar; tinted so the toolbar and the page band stay one dark region. */
    .progress-slot { height: 4px; background: #1e293b; }

    .shell-content { padding: 0 1.5rem 2rem; max-width: 1400px; margin: 0 auto; }

    .user-summary {
      display: flex;
      flex-direction: column;
      padding: 0.75rem 1rem;
      gap: 0.15rem;

      small { color: var(--app-text-muted); }
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
