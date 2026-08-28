import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import type { ExamStatus } from '../../models/exam.models';
import type { DifficultyLevel, ItemStatus, ItemType } from '../../models/item.models';

/** Human readable labels for the enumerations the interface renders. */
const LABELS: Readonly<Record<string, string>> = {
  MultipleChoiceSingleResponse: 'Single response',
  MultipleChoiceMultipleResponse: 'Multiple response',
  EitherOr: 'Either/or',
  Essay: 'Essay',
  Draft: 'Draft',
  InReview: 'In review',
  Approved: 'Approved',
  Published: 'Published',
  Retired: 'Retired',
  Archived: 'Archived',
  VeryEasy: 'Very easy',
  Easy: 'Easy',
  Medium: 'Medium',
  Hard: 'Hard',
  VeryHard: 'Very hard',
};

/**
 * The palette that makes a value recognisable at a glance.
 *
 * Lifecycle statuses run neutral to green as an item matures, difficulties run light to dark blue as
 * they get harder, and answer shapes use a separate violet family so the two are never confused.
 */
const CHIP_CLASS: Readonly<Record<string, string>> = {
  Draft: 'chip-neutral',
  InReview: 'chip-warn',
  Approved: 'chip-info',
  Published: 'chip-success',
  Retired: 'chip-muted',
  Archived: 'chip-muted',

  VeryEasy: 'chip-level-1',
  Easy: 'chip-level-2',
  Medium: 'chip-level-3',
  Hard: 'chip-level-4',
  VeryHard: 'chip-level-5',

  MultipleChoiceSingleResponse: 'chip-accent',
  MultipleChoiceMultipleResponse: 'chip-accent',
  EitherOr: 'chip-accent',
  Essay: 'chip-accent',
};

/** Renders a lifecycle status, an answer shape or a difficulty as a labelled chip. */
@Component({
  selector: 'app-status-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatChipsModule],
  template: `
    <mat-chip [class]="chipClass()" [attr.aria-label]="label()">{{ label() }}</mat-chip>
  `,
  styles: `
    mat-chip {
      font-size: 0.75rem;
      font-weight: 500;
      min-height: 1.5rem;
    }
  `,
})
export class StatusChip {
  /** The value to render. */
  readonly value = input.required<ItemStatus | ItemType | DifficultyLevel | ExamStatus | string>();

  /** The human readable label of the value. */
  protected label(): string {
    const value = this.value();
    return LABELS[value] ?? value;
  }

  /** The style class that colours the chip. */
  protected chipClass(): string {
    return CHIP_CLASS[this.value()] ?? 'chip-neutral';
  }
}
