import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import type { ExamDraft } from '../../shared/models/exam.models';

/** Collects the editorial details of a new exam. */
@Component({
  selector: 'app-new-exam-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  template: `
    <h2 mat-dialog-title>New examination</h2>

    <mat-dialog-content>
      <form [formGroup]="form" class="grid" novalidate>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Title</mat-label>
          <input matInput formControlName="title" data-testid="exam-title" />
          @if (form.controls.title.touched && form.controls.title.invalid) {
            <mat-error>A title is required.</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Description (optional)</mat-label>
          <textarea matInput rows="3" formControlName="description"></textarea>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Time limit (minutes)</mat-label>
          <input matInput type="number" min="1" max="1440" formControlName="timeLimitMinutes" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Passing score (%)</mat-label>
          <input
            matInput
            type="number"
            min="0"
            max="100"
            formControlName="passingScorePercentage"
            data-testid="exam-passing-score"
          />
        </mat-form-field>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button matButton type="button" mat-dialog-close>Cancel</button>
      <button matButton="filled" type="button" (click)="confirm()" data-testid="confirm-new-exam">
        Create
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(10rem, 1fr));
      gap: 0.75rem;
      padding-top: 0.5rem;
      min-width: min(32rem, 80vw);
    }

    .full-width { grid-column: 1 / -1; }
  `,
})
export class NewExamDialog {
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<NewExamDialog, ExamDraft | undefined>);

  /** The details form. */
  protected readonly form = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(256)]],
    description: [''],
    timeLimitMinutes: this.formBuilder.control<number | null>(60, [
      Validators.min(1),
      Validators.max(1440),
    ]),
    passingScorePercentage: [50, [Validators.required, Validators.min(0), Validators.max(100)]],
  });

  /** Closes the dialog with the entered details. */
  protected confirm(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.dialogRef.close({
      title: value.title,
      description: value.description || null,
      timeLimitMinutes: value.timeLimitMinutes,
      passingScorePercentage: Number(value.passingScorePercentage),
    });
  }
}
