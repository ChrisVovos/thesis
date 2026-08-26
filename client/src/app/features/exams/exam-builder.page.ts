import { CdkDrag, CdkDragDrop, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthStore } from '../../core/auth/auth.store';
import { isAppError } from '../../core/errors/app-error';
import { NotificationService } from '../../core/notifications/notification.service';
import { TransportService } from '../../core/transport/transport.service';
import { ExamsGateway } from '../../data-access/gateways/exams.gateway';
import { ItemsGateway } from '../../data-access/gateways/items.gateway';
import { LoadState } from '../../shared/components/load-state/load-state';
import { StatusChip } from '../../shared/components/status-chip/status-chip';
import { Permissions } from '../../shared/models/auth.models';
import type { ExamItem, ExamTransition } from '../../shared/models/exam.models';
import { describeCompositionViolation } from './composition-messages';

/**
 * The exam builder: compose sections, place published items, reorder by dragging, and publish.
 */
@Component({
  selector: 'app-exam-builder-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CdkDrag,
    CdkDropList,
    FormsModule,
    LoadState,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatListModule,
    RouterLink,
    StatusChip,
  ],
  templateUrl: './exam-builder.page.html',
  styleUrl: './exam-builder.page.scss',
})
export class ExamBuilderPage {
  private readonly exams = inject(ExamsGateway);
  private readonly items = inject(ItemsGateway);
  private readonly notifications = inject(NotificationService);
  private readonly auth = inject(AuthStore);
  private readonly transport = inject(TransportService);

  /** The exam being assembled. */
  readonly id = input.required<string>();

  /** The free-text term used to find items to place. */
  protected readonly itemSearch = signal('');

  /** The title of the section about to be added. */
  protected readonly newSectionTitle = signal('');

  /** Whether the signed-in user may change this exam. */
  protected readonly canEdit = computed(() => this.auth.has(Permissions.ExamsUpdate));
  protected readonly canPublish = computed(() => this.auth.has(Permissions.ExamsPublish));

  /** The exam and its composition; re-runs when the transport changes. */
  protected readonly exam = rxResource({
    params: () => ({ id: this.id(), transport: this.transport.active() }),
    stream: ({ params }) => this.exams.getById(params.id),
  });

  /** The published items available for placement. */
  protected readonly available = rxResource({
    params: () => ({ search: this.itemSearch(), transport: this.transport.active() }),
    stream: ({ params }) =>
      this.items.search({
        page: 1,
        pageSize: 20,
        search: params.search || undefined,
        statuses: ['Published'],
      }),
  });

  /** The normalized failure of the last load, when it failed. */
  protected readonly failure = computed(() => {
    const error = this.exam.error();
    return isAppError(error) ? error : null;
  });

  /** The composition rules the exam currently violates, as sentences. */
  protected readonly violations = computed(() =>
    (this.exam.value()?.compositionViolations ?? []).map(describeCompositionViolation),
  );

  /** Whether the exam is a draft and therefore still editable. */
  protected readonly isDraft = computed(() => this.exam.value()?.summary.status === 'Draft');

  /** Appends a section. */
  protected async addSection(): Promise<void> {
    const title = this.newSectionTitle().trim();
    if (!title) {
      return;
    }

    await this.mutate(() => this.exams.addSection(this.id(), { title }), 'The section was added.');
    this.newSectionTitle.set('');
  }

  /**
   * Removes a section together with all of its placements.
   *
   * @param sectionId The section to remove.
   */
  protected removeSection(sectionId: string): Promise<void> {
    return this.mutate(
      () => this.exams.removeSection(this.id(), sectionId),
      'The section was removed.',
    );
  }

  /**
   * Places an item at the end of a section.
   *
   * @param sectionId The section to append to.
   * @param itemId The item to place.
   */
  protected addItem(sectionId: string, itemId: string): Promise<void> {
    return this.mutate(
      () => this.exams.addItem(this.id(), sectionId, itemId),
      'The item was added to the exam.',
    );
  }

  /**
   * Removes a placement.
   *
   * @param sectionId The section holding the placement.
   * @param examItemId The placement to remove.
   */
  protected removeItem(sectionId: string, examItemId: string): Promise<void> {
    return this.mutate(
      () => this.exams.removeItem(this.id(), sectionId, examItemId),
      'The item was removed from the exam.',
    );
  }

  /**
   * Applies a drag-and-drop reorder to a section.
   *
   * @param sectionId The section being reordered.
   * @param event The drop event describing the move.
   */
  protected async reorder(sectionId: string, event: CdkDragDrop<readonly ExamItem[]>): Promise<void> {
    if (event.previousIndex === event.currentIndex) {
      return;
    }

    const ordered = [...event.container.data];
    moveItemInArray(ordered, event.previousIndex, event.currentIndex);

    await this.mutate(
      () => this.exams.reorderItems(this.id(), sectionId, ordered.map((item) => item.id)),
      'The exam was reordered.',
    );
  }

  /**
   * Requests a lifecycle transition on the exam.
   *
   * @param transition The transition to request.
   */
  protected transition(transition: ExamTransition): Promise<void> {
    return this.mutate(
      () => this.exams.transition(this.id(), transition),
      'The exam was updated.',
    );
  }

  private async mutate(operation: () => Promise<unknown> | object, message: string): Promise<void> {
    try {
      await firstValueFrom(operation() as never);
      this.notifications.success(message);
      this.exam.reload();
    } catch (error: unknown) {
      this.notifications.failure(
        isAppError(error)
          ? error
          : { code: 'client.unexpected', message: 'The operation failed.', kind: 'failure' },
      );
    }
  }
}
