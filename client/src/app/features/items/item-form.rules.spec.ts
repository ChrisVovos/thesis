import { FormBuilder, type FormGroup } from '@angular/forms';
import { correctAnswerRule, describeFormErrors, minimumOptions, optionCountRule } from './item-form.rules';
import type { ItemType } from '../../shared/models/item.models';

describe('item form rules', () => {
  const formBuilder = new FormBuilder();

  function buildForm(type: ItemType, options: readonly boolean[]): FormGroup {
    return formBuilder.group({
      type: [type],
      options: formBuilder.array(
        options.map((isCorrect) => formBuilder.group({ text: ['x'], isCorrect: [isCorrect] })),
      ),
    });
  }

  it('requires two options for a single response item', () => {
    expect(optionCountRule(buildForm('MultipleChoiceSingleResponse', [true]))).toEqual({
      itemTooFewOptions: { required: 2 },
    });
  });

  it('accepts a single response item with exactly one correct option', () => {
    const form = buildForm('MultipleChoiceSingleResponse', [true, false, false]);

    expect(optionCountRule(form)).toBeNull();
    expect(correctAnswerRule(form)).toBeNull();
  });

  it('rejects a single response item with two correct options', () => {
    expect(correctAnswerRule(buildForm('MultipleChoiceSingleResponse', [true, true]))).toEqual({
      itemRequiresExactlyOneCorrect: true,
    });
  });

  it('requires two correct options for a multiple response item', () => {
    expect(
      correctAnswerRule(buildForm('MultipleChoiceMultipleResponse', [true, false, false])),
    ).toEqual({ itemRequiresTwoCorrect: true });
  });

  it('requires a distractor in a multiple response item', () => {
    expect(correctAnswerRule(buildForm('MultipleChoiceMultipleResponse', [true, true, true]))).toEqual(
      { itemRequiresDistractor: true },
    );
  });

  it('requires exactly two options for an either/or item', () => {
    expect(optionCountRule(buildForm('EitherOr', [true, false, false]))).toEqual({
      itemEitherOrRequiresTwoOptions: true,
    });
  });

  it('applies no option rules to an essay item', () => {
    const form = buildForm('Essay', []);

    expect(optionCountRule(form)).toBeNull();
    expect(correctAnswerRule(form)).toBeNull();
  });

  it('states the minimum number of options for each answer shape', () => {
    expect(minimumOptions('MultipleChoiceSingleResponse')).toBe(2);
    expect(minimumOptions('MultipleChoiceMultipleResponse')).toBe(3);
    expect(minimumOptions('EitherOr')).toBe(2);
    expect(minimumOptions('Essay')).toBe(0);
  });

  it('renders every failure as a sentence a user can act on', () => {
    expect(describeFormErrors(null)).toBeNull();
    expect(describeFormErrors({ itemRequiresExactlyOneCorrect: true })).toContain('Exactly one');
    expect(describeFormErrors({ itemTooFewOptions: { required: 3 } })).toContain('at least 3');
    expect(describeFormErrors({ itemEitherOrRequiresTwoOptions: true })).toContain('exactly two');
  });
});
