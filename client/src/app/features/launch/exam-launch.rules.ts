import type { ItemDetail } from '../../shared/models/item.models';

/** A question as the runner presents it: where it sits, what it is worth and how it was authored. */
export interface LaunchQuestion {
  readonly examItemId: string;
  readonly sectionTitle: string;
  readonly points: number;
  readonly detail: ItemDetail;
}

/** What the candidate entered for one question. */
export interface LaunchResponse {
  /** The positions of the options the candidate chose. */
  readonly selected: readonly number[];

  /** The prose the candidate wrote, for an essay. */
  readonly text: string;
}

/** How one question was marked. */
export interface QuestionOutcome {
  readonly examItemId: string;
  readonly points: number;
  readonly awarded: number;

  /** Whether the answer could be marked without a human. */
  readonly automatic: boolean;

  /** Whether an automatically marked answer matched the authored key. */
  readonly correct: boolean;

  /** Whether the candidate entered anything at all. */
  readonly answered: boolean;
}

/** The result of a whole attempt. */
export interface ExamOutcome {
  readonly awarded: number;
  readonly possible: number;
  readonly percentage: number;
  readonly passed: boolean;

  /** How many essays still carry a mark the examiner entered by hand. */
  readonly manualCount: number;
  readonly questions: readonly QuestionOutcome[];
}

/** The response a question starts with. */
export const EMPTY_RESPONSE: LaunchResponse = { selected: [], text: '' };

/** The positions of the options the author marked correct. */
export function correctPositions(detail: ItemDetail): readonly number[] {
  return detail.options
    .filter((option) => option.isCorrect)
    .map((option) => option.position)
    .sort((left, right) => left - right);
}

function sameSelection(left: readonly number[], right: readonly number[]): boolean {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

/**
 * Marks one question.
 *
 * Choice questions are all or nothing: a multiple response answer scores only when it names every
 * correct option and no incorrect one, because partial credit is a marking policy the exam does not
 * carry. Essays cannot be marked from the key at all, so they take the examiner's award instead.
 */
export function gradeQuestion(
  question: LaunchQuestion,
  response: LaunchResponse = EMPTY_RESPONSE,
  award = 0,
): QuestionOutcome {
  const { examItemId, points } = question;

  if (question.detail.summary.type === 'Essay') {
    const answered = response.text.trim().length > 0;
    return {
      examItemId,
      points,
      awarded: Math.min(Math.max(award, 0), points),
      automatic: false,
      correct: false,
      answered,
    };
  }

  const chosen = [...response.selected].sort((left, right) => left - right);
  const correct = sameSelection(chosen, correctPositions(question.detail));

  return {
    examItemId,
    points,
    awarded: correct ? points : 0,
    automatic: true,
    correct,
    answered: chosen.length > 0,
  };
}

/**
 * Marks a whole attempt against the exam's passing score.
 *
 * @param questions The questions presented, in the order they were shown.
 * @param responses What the candidate entered, keyed by placement.
 * @param awards The marks the examiner gave the essays, keyed by placement.
 * @param passingScorePercentage The percentage the exam requires to pass.
 */
export function gradeExam(
  questions: readonly LaunchQuestion[],
  responses: Readonly<Record<string, LaunchResponse>>,
  awards: Readonly<Record<string, number>>,
  passingScorePercentage: number,
): ExamOutcome {
  const outcomes = questions.map((question) =>
    gradeQuestion(question, responses[question.examItemId], awards[question.examItemId] ?? 0),
  );

  const awarded = outcomes.reduce((total, outcome) => total + outcome.awarded, 0);
  const possible = outcomes.reduce((total, outcome) => total + outcome.points, 0);
  const percentage = possible === 0 ? 0 : Math.round((awarded / possible) * 1000) / 10;

  return {
    awarded,
    possible,
    percentage,
    passed: possible > 0 && percentage >= passingScorePercentage,
    manualCount: outcomes.filter((outcome) => !outcome.automatic).length,
    questions: outcomes,
  };
}
