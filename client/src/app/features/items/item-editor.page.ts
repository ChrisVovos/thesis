import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  FormArray,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
  type FormGroup,
  type ValidationErrors,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router } from '@angular/router';
import { firstValueFrom, of } from 'rxjs';
import { isAppError } from '../../core/errors/app-error';
import { NotificationService } from '../../core/notifications/notification.service';
import { TransportService } from '../../core/transport/transport.service';
import { ItemsGateway, TaxonomyGateway } from '../../data-access/gateways/items.gateway';
import { LoadState } from '../../shared/components/load-state/load-state';
import {
  DIFFICULTY_LEVELS,
  ITEM_TYPES,
  type DifficultyLevel,
  type ItemDetail,
  type ItemDraft,
  type ItemType,
} from '../../shared/models/item.models';
import { correctAnswerRule, describeFormErrors, optionCountRule } from './item-form.rules';

/**
 * The item editor.
 *
 * One form serves all four answer shapes. The shape specific rules are expressed as reactive form
 * validators that mirror, name for name, the invariants the domain enforces on the server — so a
 * mistake is reported before a round trip, and the server still refuses it if the client is bypassed.
 */
@Component({
  selector: 'app-item-editor-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LoadState,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './item-editor.page.html',
  styleUrl: './item-editor.page.scss',
})
export class ItemEditorPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly items = inject(ItemsGateway);
  private readonly taxonomy = inject(TaxonomyGateway);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly transport = inject(TransportService);

  /** The item being edited, or `undefined` when authoring a new one. */
  readonly id = input<string>();

  /** The options offered by the form. */
  protected readonly itemTypes = ITEM_TYPES;
  protected readonly difficulties = DIFFICULTY_LEVELS;

  /** Whether a save is in flight. */
  protected readonly saving = signal(false);

  /** The categories an item can be filed under. */
  protected readonly categories = rxResource({
    params: () => ({ transport: this.transport.active() }),
    stream: () => this.taxonomy.categories(),
  });

  /** The item being edited, when the route names one. */
  protected readonly existing = rxResource({
    params: () => ({ id: this.id(), transport: this.transport.active() }),
    stream: ({ params }) =>
      params.id ? this.items.getById(params.id) : of<ItemDetail | undefined>(undefined),
  });

  /** The editor form. */
  protected readonly form = this.formBuilder.nonNullable.group(
    {
      type: this.formBuilder.nonNullable.control<ItemType>('MultipleChoiceSingleResponse'),
      stem: ['', [Validators.required, Validators.maxLength(4000)]],
      difficulty: this.formBuilder.nonNullable.control<DifficultyLevel>('Medium'),
      categoryId: ['', [Validators.required]],
      maximumScore: [1, [Validators.required, Validators.min(0.01), Validators.max(1000)]],
      options: this.formBuilder.array<FormGroup>([]),
      rubricGuidance: [''],
      rubricMinimumWords: [50],
      rubricMaximumWords: [400],
      sampleAnswer: [''],
    },
    { validators: [optionCountRule, correctAnswerRule] },
  );

  /** Whether the selected shape uses answer options. */
  protected readonly usesOptions = computed(() => this.selectedType() !== 'Essay');

  /** Whether the selected shape uses a grading rubric. */
  protected readonly usesRubric = computed(() => this.selectedType() === 'Essay');

  /** Whether the selected shape allows more than one correct option. */
  protected readonly allowsMultipleCorrect = computed(
    () => this.selectedType() === 'MultipleChoiceMultipleResponse',
  );

  /** Whether the selected shape has a fixed number of options. */
  protected readonly hasFixedOptions = computed(() => this.selectedType() === 'EitherOr');

  private readonly selectedType = signal<ItemType>('MultipleChoiceSingleResponse');

  constructor() {
    this.form.controls.type.valueChanges.subscribe((type) => {
      this.selectedType.set(type);
      this.applyShapeDefaults(type);
    });

    effect(() => {
      const detail = this.existing.value();
      if (detail) {
        this.load(detail);
      }
    });

    this.applyShapeDefaults('MultipleChoiceSingleResponse');
  }

  /** The option rows of the form. */
  protected get optionRows(): FormArray<FormGroup> {
    return this.form.controls.options;
  }

  /** The validation failures reported for the form as a whole. */
  protected get formErrors(): ValidationErrors | null {
    return this.form.errors;
  }

  /**
   * Renders the form-level validation failure as a sentence.
   *
   * @returns The message to show, or `null` when the form is valid.
   */
  protected formErrorMessage(): string | null {
    return describeFormErrors(this.form.errors);
  }

  /** Appends an empty option row. */
  protected addOption(): void {
    this.optionRows.push(this.newOptionRow());
  }

  /**
   * Removes an option row.
   *
   * @param index The zero based row index.
   */
  protected removeOption(index: number): void {
    this.optionRows.removeAt(index);
  }

  /**
   * Marks one option as correct, clearing the others when the shape allows only one.
   *
   * @param index The zero based row index.
   * @param isCorrect Whether the option is correct.
   */
  protected setCorrect(index: number, isCorrect: boolean): void {
    if (isCorrect && !this.allowsMultipleCorrect()) {
      this.optionRows.controls.forEach((row, rowIndex) =>
        row.controls['isCorrect'].setValue(rowIndex === index, { emitEvent: false }),
      );
      return;
    }

    this.optionRows.at(index).controls['isCorrect'].setValue(isCorrect, { emitEvent: false });
  }

  /** Saves the item and returns to the bank. */
  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    try {
      const draft = this.toDraft();
      const existingId = this.id();

      if (existingId) {
        await firstValueFrom(this.items.update(existingId, draft));
        this.notifications.success('The item was saved.');
      } else {
        const created = await firstValueFrom(this.items.create(draft));
        this.notifications.success('The item was created.');
        await this.router.navigate(['/items', created]);
        return;
      }

      await this.router.navigate(['/items']);
    } catch (error: unknown) {
      this.notifications.failure(
        isAppError(error)
          ? error
          : { code: 'client.unexpected', message: 'The item could not be saved.', kind: 'failure' },
      );
    } finally {
      this.saving.set(false);
    }
  }

  /** Abandons the edit. */
  protected cancel(): void {
    void this.router.navigate(['/items']);
  }

  private toDraft(): ItemDraft {
    const value = this.form.getRawValue();

    return {
      type: value.type,
      stem: value.stem,
      difficulty: value.difficulty,
      categoryId: value.categoryId,
      maximumScore: Number(value.maximumScore),
      options: this.usesOptions()
        ? this.optionRows.controls.map((row) => ({
            text: row.controls['text'].value as string,
            isCorrect: row.controls['isCorrect'].value as boolean,
            feedback: (row.controls['feedback'].value as string) || null,
          }))
        : undefined,
      rubric: this.usesRubric()
        ? {
            guidance: value.rubricGuidance,
            minimumWords: Number(value.rubricMinimumWords),
            maximumWords: Number(value.rubricMaximumWords),
          }
        : undefined,
      sampleAnswer: this.usesRubric() ? value.sampleAnswer || null : null,
      tagIds: [],
    };
  }

  private load(detail: ItemDetail): void {
    const summary = detail.summary;

    this.selectedType.set(summary.type);
    this.form.patchValue(
      {
        type: summary.type,
        stem: summary.stem,
        difficulty: summary.difficulty,
        categoryId: summary.categoryId,
        maximumScore: summary.maximumScore,
        rubricGuidance: detail.rubricGuidance ?? '',
        rubricMinimumWords: detail.rubricMinimumWords ?? 50,
        rubricMaximumWords: detail.rubricMaximumWords ?? 400,
        sampleAnswer: detail.sampleAnswer ?? '',
      },
      { emitEvent: false },
    );

    this.optionRows.clear({ emitEvent: false });
    for (const option of detail.options) {
      this.optionRows.push(this.newOptionRow(option.text, option.isCorrect, option.feedback ?? ''), {
        emitEvent: false,
      });
    }

    this.form.updateValueAndValidity();
  }

  private applyShapeDefaults(type: ItemType): void {
    if (type === 'Essay') {
      this.optionRows.clear();
      this.form.controls.rubricGuidance.setValidators([
        Validators.required,
        Validators.maxLength(4000),
      ]);
    } else {
      this.form.controls.rubricGuidance.clearValidators();
      if (type === 'EitherOr') {
        this.optionRows.clear();
        this.optionRows.push(this.newOptionRow('True', true));
        this.optionRows.push(this.newOptionRow('False', false));
      } else if (this.optionRows.length === 0) {
        this.addOption();
        this.addOption();
        this.optionRows.at(0).controls['isCorrect'].setValue(true, { emitEvent: false });
      }
    }

    this.form.controls.rubricGuidance.updateValueAndValidity({ emitEvent: false });
    this.form.updateValueAndValidity({ emitEvent: false });
  }

  private newOptionRow(text = '', isCorrect = false, feedback = ''): FormGroup {
    return this.formBuilder.nonNullable.group({
      text: [text, [Validators.required, Validators.maxLength(1000)]],
      isCorrect: [isCorrect],
      feedback: [feedback, [Validators.maxLength(1000)]],
    });
  }
}
