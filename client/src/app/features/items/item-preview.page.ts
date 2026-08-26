import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { RouterLink } from '@angular/router';
import { isAppError } from '../../core/errors/app-error';
import { TransportService } from '../../core/transport/transport.service';
import { ItemsGateway } from '../../data-access/gateways/items.gateway';
import { LoadState } from '../../shared/components/load-state/load-state';
import { StatusChip } from '../../shared/components/status-chip/status-chip';

/** Shows an item as a candidate would see it, together with its version history. */
@Component({
  selector: 'app-item-preview-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    LoadState,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatIconModule,
    MatListModule,
    RouterLink,
    StatusChip,
  ],
  template: `
    <app-load-state [loading]="detail.isLoading()" [error]="failure()" (retry)="detail.reload()" />

    @if (detail.value(); as item) {
      <header class="page-header">
        <div>
          <h1>Item preview</h1>
          <div class="chips">
            <app-status-chip [value]="item.summary.type" />
            <app-status-chip [value]="item.summary.status" />
            <app-status-chip [value]="item.summary.difficulty" />
          </div>
        </div>
        <a matButton="outlined" routerLink="/items">
          <mat-icon>arrow_back</mat-icon>
          Back to the bank
        </a>
      </header>

      <mat-card>
        <mat-card-header>
          <mat-card-title>{{ item.summary.stem }}</mat-card-title>
          <mat-card-subtitle>
            {{ item.summary.categoryName }} · worth {{ item.summary.maximumScore }} ·
            authored by {{ item.summary.authorName }}
          </mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          @if (item.options.length > 0) {
            <ol class="options">
              @for (option of item.options; track option.position) {
                <li [class.correct]="option.isCorrect">
                  <span class="marker" aria-hidden="true">
                    {{ option.isCorrect ? '●' : '○' }}
                  </span>
                  <span>
                    {{ option.text }}
                    @if (option.isCorrect) {
                      <span class="visually-hidden">(correct answer)</span>
                    }
                  </span>
                  @if (option.feedback) {
                    <small class="feedback">{{ option.feedback }}</small>
                  }
                </li>
              }
            </ol>
          }

          @if (item.rubricGuidance) {
            <section class="rubric">
              <h2>Grading rubric</h2>
              <p>{{ item.rubricGuidance }}</p>
              <p class="word-range">
                Expected length {{ item.rubricMinimumWords }}–{{ item.rubricMaximumWords }} words.
              </p>
              @if (item.sampleAnswer) {
                <h3>Sample answer</h3>
                <p>{{ item.sampleAnswer }}</p>
              }
            </section>
          }
        </mat-card-content>
      </mat-card>

      <mat-card class="versions">
        <mat-card-header>
          <mat-card-title>Version history</mat-card-title>
          <mat-card-subtitle>
            Published versions are frozen and can never change.
          </mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          @if (item.versions.length === 0) {
            <p class="muted">This item has never been published.</p>
          } @else {
            <mat-list>
              @for (version of item.versions; track version.id) {
                <mat-list-item>
                  <span matListItemTitle>Version {{ version.versionNumber }}</span>
                  <span matListItemLine>
                    Published {{ version.publishedAtUtc | date: 'medium' }} ·
                    worth {{ version.maximumScore }}
                  </span>
                </mat-list-item>
                <mat-divider />
              }
            </mat-list>
          }
        </mat-card-content>
      </mat-card>
    }
  `,
  styles: `
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      margin-bottom: 1rem;

      h1 { margin: 0 0 0.5rem; font-size: 1.5rem; }
    }

    .chips { display: flex; gap: 0.35rem; flex-wrap: wrap; }
    .options { list-style: none; padding: 0; margin: 1rem 0 0; display: flex; flex-direction: column; gap: 0.5rem; }
    .options li { display: grid; grid-template-columns: 1.5rem 1fr; gap: 0.25rem 0.5rem; }
    .options li.correct { font-weight: 600; }
    .feedback { grid-column: 2; opacity: 0.7; }
    .rubric { margin-top: 1.5rem; }
    .rubric h2 { font-size: 1rem; }
    .word-range { opacity: 0.7; }
    .versions { margin-top: 1rem; }
    .muted { opacity: 0.7; }

    .visually-hidden {
      position: absolute;
      width: 1px;
      height: 1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
    }
  `,
})
export class ItemPreviewPage {
  private readonly items = inject(ItemsGateway);
  private readonly transport = inject(TransportService);

  /** The item to preview. */
  readonly id = input.required<string>();

  /** The loaded item. Re-runs whenever the transport changes. */
  protected readonly detail = rxResource({
    params: () => ({ id: this.id(), transport: this.transport.active() }),
    stream: ({ params }) => this.items.getById(params.id),
  });

  /** The normalized failure of the last load, when it failed. */
  protected readonly failure = computed(() => {
    const error = this.detail.error();
    return isAppError(error) ? error : null;
  });
}
