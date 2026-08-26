import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

/** Shown when the signed-in user lacks the permission a route requires. */
@Component({
  selector: 'app-forbidden-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, RouterLink],
  template: `
    <section class="message" role="alert">
      <mat-icon aria-hidden="true">lock</mat-icon>
      <h1>Not permitted</h1>
      <p>Your account does not hold the permission this area requires.</p>
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
export class ForbiddenPage {}
