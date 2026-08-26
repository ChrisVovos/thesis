import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

/** Shown when no route matches the requested address. */
@Component({
  selector: 'app-not-found-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, RouterLink],
  template: `
    <section class="message">
      <mat-icon aria-hidden="true">explore_off</mat-icon>
      <h1>Page not found</h1>
      <p>The address you followed does not lead anywhere in this application.</p>
      <a matButton="filled" routerLink="/items">Back to the item bank</a>
    </section>
  `,
  styles: `
    .message {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.75rem;
      padding: 4rem 1rem;
      text-align: center;
    }
  `,
})
export class NotFoundPage {}
