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

/** The palette used to make a status recognisable at a glance. */
const STATUS_CLASS: Readonly<Record<string, string>> = {
  Draft: 'status-draft',
  InReview: 'status-review',
  Approved: 'status-approved',
  Published: 'status-published',
  Retired: 'status-retired',
  Archived: 'status-retired',
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
      min-height: 1.5rem;
    }

    .status-draft { --mdc-chip-elevated-container-color: #e8eaf6; }
    .status-review { --mdc-chip-elevated-container-color: #fff8e1; }
    .status-approved { --mdc-chip-elevated-container-color: #e0f2f1; }
    .status-published { --mdc-chip-elevated-container-color: #e8f5e9; }
    .status-retired { --mdc-chip-elevated-container-color: #eceff1; }
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
    return STATUS_CLASS[this.value()] ?? '';
  }
}
