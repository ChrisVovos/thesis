import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { forkJoin, map, of, switchMap, type Observable } from 'rxjs';
import { isAppError } from '../../core/errors/app-error';
import { TransportService } from '../../core/transport/transport.service';
import { ExamsGateway } from '../../data-access/gateways/exams.gateway';
import { ItemsGateway } from '../../data-access/gateways/items.gateway';
import { LoadState } from '../../shared/components/load-state/load-state';
import { StatusChip } from '../../shared/components/status-chip/status-chip';
import type { ExamDetail, ExamSummary } from '../../shared/models/exam.models';
import {
  EMPTY_RESPONSE,
  correctPositions,
  gradeExam,
  type LaunchQuestion,
  type LaunchResponse,
} from './exam-launch.rules';

/** An exam loaded ready to sit: the exam itself and every question in presentation order. */
interface LaunchedExam {
  readonly exam: ExamDetail;
  readonly questions: readonly LaunchQuestion[];
}

/**
 * Sits a published exam end to end.
 *
 * The page exists so an exam can be demonstrated rather than only assembled: it walks the questions
 * in order, collects answers, and marks the attempt against the exam's own passing score. Nothing is
 * persisted — an attempt lives in this component and is gone when the page is left — because the
 * server owns no notion of a sitting, and inventing one client side would misrepresent the API.
 */
@Component({
  selector: 'app-exam-launch-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    LoadState,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatRadioModule,
    MatSelectModule,
    StatusChip,
  ],
  templateUrl: './exam-launch.page.html',
  styleUrl: './exam-launch.page.scss',
})
export class ExamLaunchPage {
  private readonly exams = inject(ExamsGateway);
  private readonly items = inject(ItemsGateway);

  /** The active transport; every request re-runs when it changes. */
  protected readonly transport = inject(TransportService);

  /** The exam chosen in the picker, before it is launched. */
  protected readonly chosenExamId = signal<string | null>(null);

  /** The exam actually launched, which is what loads the questions. */
  protected readonly launchedExamId = signal<string | null>(null);

  /** The question on screen. */
  protected readonly index = signal(0);

  /** Whether the attempt has been submitted for marking. */
  protected readonly submitted = signal(false);

  /** What the candidate entered, keyed by placement. */
  private readonly responses = signal<Record<string, LaunchResponse>>({});

  /** The marks the examiner gave the essays, keyed by placement. */
  private readonly awards = signal<Record<string, number>>({});

  /** The exams available to sit. */
  protected readonly published = rxResource({
    params: () => ({ transport: this.transport.active() }),
    stream: () => this.exams.search({ page: 1, pageSize: 100, statuses: ['Published'] }),
  });

  /**
   * The launched exam and its questions.
   *
   * A placement carries only the item summary, so the authored options have to be fetched per item;
   * that is also what makes the page a fair demonstration of either transport under a real workload.
   */
  protected readonly attempt = rxResource({
    params: () => ({ id: this.launchedExamId(), transport: this.transport.active() }),
    stream: ({ params }) => (params.id ? this.load(params.id) : of<LaunchedExam | null>(null)),
  });

  /** The exams the picker offers. */
  protected readonly publishedExams = computed<readonly ExamSummary[]>(
    () => this.published.value()?.items ?? [],
  );

  /** The questions of the launched exam. */
  protected readonly questions = computed<readonly LaunchQuestion[]>(
    () => this.attempt.value()?.questions ?? [],
  );

  /** How many questions the attempt has. */
  protected readonly total = computed(() => this.questions().length);

  /** The question on screen, when there is one. */
  protected readonly current = computed<LaunchQuestion | null>(
    () => this.questions()[this.index()] ?? null,
  );

  /** How far through the attempt the candidate is, as a percentage. */
  protected readonly progress = computed(() => {
    const total = this.total();
    return total === 0 ? 0 : ((this.index() + 1) / total) * 100;
  });

  /** How many questions carry an answer. */
  protected readonly answeredCount = computed(() => {
    const responses = this.responses();
    return this.questions().filter((question) => {
      const response = responses[question.examItemId];
      return response ? response.selected.length > 0 || response.text.trim().length > 0 : false;
    }).length;
  });

  /** The marked attempt. */
  protected readonly outcome = computed(() =>
    gradeExam(
      this.questions(),
      this.responses(),
      this.awards(),
      this.attempt.value()?.exam.summary.passingScorePercentage ?? 0,
    ),
  );

  /** The normalized failure of the last load, when it failed. */
  protected readonly failure = computed(() => {
    const error = this.attempt.error() ?? this.published.error();
    return isAppError(error) ? error : null;
  });

  /** Starts an attempt at the chosen exam. */
  protected launch(): void {
    const id = this.chosenExamId();
    if (!id) {
      return;
    }

    this.reset();
    this.launchedExamId.set(id);
  }

  /** Abandons the attempt and returns to the picker. */
  protected exit(): void {
    this.reset();
    this.launchedExamId.set(null);
  }

  /** Starts the same exam again from the first question. */
  protected restart(): void {
    const id = this.launchedExamId();
    this.reset();
    this.launchedExamId.set(id);
  }

  /** Moves to the next question, or submits when the last one is answered. */
  protected next(): void {
    if (this.index() + 1 < this.total()) {
      this.index.update((index) => index + 1);
      return;
    }

    this.submitted.set(true);
  }

  /** Moves back to the previous question. */
  protected previous(): void {
    this.index.update((index) => Math.max(0, index - 1));
  }

  /** Reopens the paper so the candidate can change an answer. */
  protected reopen(): void {
    this.submitted.set(false);
  }

  /** The answer recorded for a question. */
  protected responseFor(examItemId: string): LaunchResponse {
    return this.responses()[examItemId] ?? EMPTY_RESPONSE;
  }

  /** Whether an option is currently chosen. */
  protected isChosen(examItemId: string, position: number): boolean {
    return this.responseFor(examItemId).selected.includes(position);
  }

  /** The single option chosen for a question, when one was. */
  protected chosenOne(examItemId: string): number | null {
    const [first] = this.responseFor(examItemId).selected;
    return first ?? null;
  }

  /** Records the single option chosen for a question. */
  protected chooseOne(examItemId: string, position: number): void {
    this.setResponse(examItemId, { selected: [position], text: '' });
  }

  /** Adds or removes one option of a multiple response question. */
  protected toggleOne(examItemId: string, position: number, chosen: boolean): void {
    const selected = this.responseFor(examItemId).selected;
    this.setResponse(examItemId, {
      selected: chosen ? [...selected, position] : selected.filter((value) => value !== position),
      text: '',
    });
  }

  /** Records the prose written for an essay. */
  protected writeEssay(examItemId: string, text: string): void {
    this.setResponse(examItemId, { selected: [], text });
  }

  /** The mark the examiner gave an essay. */
  protected awardFor(examItemId: string): number {
    return this.awards()[examItemId] ?? 0;
  }

  /** Records the mark the examiner gave an essay. */
  protected award(examItemId: string, points: number, value: unknown): void {
    const parsed = Number(value);
    const clamped = Number.isFinite(parsed) ? Math.min(Math.max(parsed, 0), points) : 0;
    this.awards.update((awards) => ({ ...awards, [examItemId]: clamped }));
  }

  /** The answer key of a question, as a sentence. */
  protected answerKey(question: LaunchQuestion): string {
    const correct = correctPositions(question.detail);
    return question.detail.options
      .filter((option) => correct.includes(option.position))
      .map((option) => option.text)
      .join(', ');
  }

  /** What the candidate chose, as a sentence. */
  protected givenAnswer(question: LaunchQuestion): string {
    const response = this.responseFor(question.examItemId);
    if (question.detail.summary.type === 'Essay') {
      return response.text.trim();
    }

    return question.detail.options
      .filter((option) => response.selected.includes(option.position))
      .map((option) => option.text)
      .join(', ');
  }

  private setResponse(examItemId: string, response: LaunchResponse): void {
    this.responses.update((responses) => ({ ...responses, [examItemId]: response }));
  }

  private reset(): void {
    this.index.set(0);
    this.submitted.set(false);
    this.responses.set({});
    this.awards.set({});
  }

  private load(id: string): Observable<LaunchedExam> {
    return this.exams.getById(id).pipe(
      switchMap((exam) => {
        const placements = exam.sections.flatMap((section) =>
          section.items.map((placement) => ({ section, placement })),
        );

        if (placements.length === 0) {
          return of<LaunchedExam>({ exam, questions: [] });
        }

        return forkJoin(
          placements.map(({ section, placement }) =>
            this.items.getById(placement.itemId).pipe(
              map(
                (detail): LaunchQuestion => ({
                  examItemId: placement.id,
                  sectionTitle: section.title,
                  points: placement.effectiveScore,
                  detail,
                }),
              ),
            ),
          ),
        ).pipe(map((questions): LaunchedExam => ({ exam, questions })));
      }),
    );
  }
}
