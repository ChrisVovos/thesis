import type { ItemDetail, ItemOption, ItemType } from '../../shared/models/item.models';
import { gradeExam, gradeQuestion, type LaunchQuestion } from './exam-launch.rules';

function option(position: number, isCorrect: boolean): ItemOption {
  return { text: `Option ${position}`, isCorrect, position };
}

function question(
  examItemId: string,
  type: ItemType,
  points: number,
  options: readonly ItemOption[],
): LaunchQuestion {
  const detail = {
    summary: { type, stem: 'Stem' },
    options,
    versions: [],
  } as unknown as ItemDetail;

  return { examItemId, sectionTitle: 'Section', points, detail };
}

describe('exam launch marking', () => {
  const single = question('q1', 'MultipleChoiceSingleResponse', 2, [
    option(0, true),
    option(1, false),
  ]);

  const multiple = question('q2', 'MultipleChoiceMultipleResponse', 3, [
    option(0, true),
    option(1, true),
    option(2, false),
  ]);

  const essay = question('q3', 'Essay', 5, []);

  it('awards the full points for the correct single response', () => {
    expect(gradeQuestion(single, { selected: [0], text: '' })).toMatchObject({
      awarded: 2,
      correct: true,
      automatic: true,
      answered: true,
    });
  });

  it('awards nothing for the wrong single response', () => {
    expect(gradeQuestion(single, { selected: [1], text: '' })).toMatchObject({
      awarded: 0,
      correct: false,
    });
  });

  it('treats an unanswered question as incorrect rather than unmarked', () => {
    expect(gradeQuestion(single)).toMatchObject({ awarded: 0, correct: false, answered: false });
  });

  it('accepts a multiple response answer whatever order the options were chosen in', () => {
    expect(gradeQuestion(multiple, { selected: [1, 0], text: '' })).toMatchObject({
      awarded: 3,
      correct: true,
    });
  });

  it('refuses partial credit when a correct option is missing', () => {
    expect(gradeQuestion(multiple, { selected: [0], text: '' })).toMatchObject({
      awarded: 0,
      correct: false,
    });
  });

  it('refuses credit when an incorrect option is added to the correct ones', () => {
    expect(gradeQuestion(multiple, { selected: [0, 1, 2], text: '' })).toMatchObject({
      awarded: 0,
      correct: false,
    });
  });

  it('leaves an essay for the examiner and takes the award given', () => {
    expect(gradeQuestion(essay, { selected: [], text: 'An answer.' }, 4)).toMatchObject({
      awarded: 4,
      automatic: false,
      answered: true,
    });
  });

  it('clamps an essay award to the points the question carries', () => {
    expect(gradeQuestion(essay, { selected: [], text: 'x' }, 99).awarded).toBe(5);
    expect(gradeQuestion(essay, { selected: [], text: 'x' }, -3).awarded).toBe(0);
  });

  it('passes an attempt that reaches the passing score', () => {
    const outcome = gradeExam(
      [single, multiple],
      { q1: { selected: [0], text: '' }, q2: { selected: [0, 1], text: '' } },
      {},
      50,
    );

    expect(outcome).toMatchObject({ awarded: 5, possible: 5, percentage: 100, passed: true });
  });

  it('fails an attempt that falls below the passing score', () => {
    const outcome = gradeExam(
      [single, multiple],
      { q1: { selected: [0], text: '' }, q2: { selected: [2], text: '' } },
      {},
      50,
    );

    expect(outcome).toMatchObject({ awarded: 2, possible: 5, percentage: 40, passed: false });
  });

  it('counts the essays waiting on the examiner', () => {
    const outcome = gradeExam([single, essay], {}, {}, 50);

    expect(outcome.manualCount).toBe(1);
    expect(outcome.possible).toBe(7);
  });

  it('reports an exam with no points as failed rather than dividing by zero', () => {
    expect(gradeExam([], {}, {}, 50)).toMatchObject({ percentage: 0, passed: false });
  });
});
