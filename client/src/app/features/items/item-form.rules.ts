import type { AbstractControl, FormArray, FormGroup, ValidationErrors } from '@angular/forms';
import type { ItemType } from '../../shared/models/item.models';

/**
 * The client side mirror of the domain's option-count invariants.
 *
 * The rules are deliberately duplicated rather than fetched, because a form must be able to say "this
 * is wrong" without a round trip. They are duplicated *only here*, in one file, next to the rule they
 * mirror, and the server remains the authority: if the two ever disagree, the request is refused and
 * the same code the domain raised is what the user sees.
 */
export function optionCountRule(control: AbstractControl): ValidationErrors | null {
  const group = control as FormGroup;
  const type = group.controls['type']?.value as ItemType | undefined;
  const options = group.controls['options'] as FormArray | undefined;

  if (!type || !options || type === 'Essay') {
    return null;
  }

  const required = minimumOptions(type);
  if (type === 'EitherOr' && options.length !== 2) {
    return { itemEitherOrRequiresTwoOptions: true };
  }

  return options.length < required ? { itemTooFewOptions: { required } } : null;
}

/**
 * The client side mirror of the domain's "which options are correct" invariants.
 */
export function correctAnswerRule(control: AbstractControl): ValidationErrors | null {
  const group = control as FormGroup;
  const type = group.controls['type']?.value as ItemType | undefined;
  const options = group.controls['options'] as FormArray | undefined;

  if (!type || !options || type === 'Essay' || options.length === 0) {
    return null;
  }

  const correct = options.controls.filter(
    (row) => (row as FormGroup).controls['isCorrect']?.value === true,
  ).length;

  switch (type) {
    case 'MultipleChoiceSingleResponse':
    case 'EitherOr':
      return correct === 1 ? null : { itemRequiresExactlyOneCorrect: true };

    case 'MultipleChoiceMultipleResponse':
      if (correct < 2) {
        return { itemRequiresTwoCorrect: true };
      }
      return correct < options.length ? null : { itemRequiresDistractor: true };

    default:
      return null;
  }
}

/**
 * The minimum number of options an answer shape requires.
 *
 * @param type The answer shape.
 * @returns The inclusive minimum number of options.
 */
export function minimumOptions(type: ItemType): number {
  switch (type) {
    case 'MultipleChoiceMultipleResponse':
      return 3;
    case 'EitherOr':
      return 2;
    case 'MultipleChoiceSingleResponse':
      return 2;
    default:
      return 0;
  }
}

/**
 * Renders a form-level validation failure as a sentence.
 *
 * @param errors The failures reported by the form.
 * @returns The message to show, or `null` when the form is valid.
 */
export function describeFormErrors(errors: ValidationErrors | null): string | null {
  if (!errors) {
    return null;
  }

  if (errors['itemEitherOrRequiresTwoOptions']) {
    return 'An either/or item must have exactly two options.';
  }
  if (errors['itemTooFewOptions']) {
    const required = (errors['itemTooFewOptions'] as { required: number }).required;
    return `This item type requires at least ${required} options.`;
  }
  if (errors['itemRequiresExactlyOneCorrect']) {
    return 'Exactly one option must be marked as correct.';
  }
  if (errors['itemRequiresTwoCorrect']) {
    return 'A multiple response item must have at least two correct options.';
  }
  if (errors['itemRequiresDistractor']) {
    return 'A multiple response item must have at least one incorrect option.';
  }

  return null;
}
