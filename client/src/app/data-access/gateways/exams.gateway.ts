import type { Observable } from 'rxjs';
import type {
  ExamDetail,
  ExamDraft,
  ExamQuery,
  ExamSectionDraft,
  ExamSummary,
  ExamTransition,
} from '../../shared/models/exam.models';
import type { PagedResult } from '../../shared/models/paging.models';

/** The exam builder contract, expressed purely in domain terms. */
export abstract class ExamsGateway {
  /**
   * Searches, sorts and pages the exam list.
   *
   * @param query The criteria supplied by the screen.
   */
  abstract search(query: ExamQuery): Observable<PagedResult<ExamSummary>>;

  /**
   * Reads a single exam together with its full composition.
   *
   * @param id The identity of the exam.
   */
  abstract getById(id: string): Observable<ExamDetail>;

  /**
   * Creates a draft exam.
   *
   * @param draft The exam to create.
   * @returns The identity of the new exam.
   */
  abstract create(draft: ExamDraft): Observable<string>;

  /**
   * Replaces the editorial details of a draft exam.
   *
   * @param id The identity of the exam.
   * @param draft The new details.
   */
  abstract update(id: string, draft: ExamDraft): Observable<void>;

  /**
   * Logically removes an exam.
   *
   * @param id The identity of the exam.
   */
  abstract remove(id: string): Observable<void>;

  /**
   * Requests a lifecycle transition.
   *
   * @param id The identity of the exam.
   * @param transition The transition to request.
   */
  abstract transition(id: string, transition: ExamTransition): Observable<void>;

  /**
   * Appends a section to a draft exam.
   *
   * @param examId The identity of the exam.
   * @param draft The section to add.
   * @returns The identity of the new section.
   */
  abstract addSection(examId: string, draft: ExamSectionDraft): Observable<string>;

  /**
   * Replaces the editorial details of a section.
   *
   * @param examId The identity of the exam.
   * @param sectionId The identity of the section.
   * @param draft The new details.
   */
  abstract updateSection(
    examId: string,
    sectionId: string,
    draft: ExamSectionDraft,
  ): Observable<void>;

  /**
   * Removes a section together with all of its placements.
   *
   * @param examId The identity of the exam.
   * @param sectionId The identity of the section.
   */
  abstract removeSection(examId: string, sectionId: string): Observable<void>;

  /**
   * Reorders the sections of an exam.
   *
   * @param examId The identity of the exam.
   * @param orderedSectionIds Every section of the exam, in the desired order.
   */
  abstract reorderSections(examId: string, orderedSectionIds: readonly string[]): Observable<void>;

  /**
   * Places an existing bank item into a section.
   *
   * @param examId The identity of the exam.
   * @param sectionId The identity of the section.
   * @param itemId The bank item to place.
   * @param scoreOverride An optional exam specific score.
   * @returns The identity of the new placement.
   */
  abstract addItem(
    examId: string,
    sectionId: string,
    itemId: string,
    scoreOverride?: number | null,
  ): Observable<string>;

  /**
   * Removes a placement from a section.
   *
   * @param examId The identity of the exam.
   * @param sectionId The identity of the section.
   * @param examItemId The identity of the placement.
   */
  abstract removeItem(examId: string, sectionId: string, examItemId: string): Observable<void>;

  /**
   * Reorders the placements inside a section.
   *
   * @param examId The identity of the exam.
   * @param sectionId The identity of the section.
   * @param orderedExamItemIds Every placement of the section, in the desired order.
   */
  abstract reorderItems(
    examId: string,
    sectionId: string,
    orderedExamItemIds: readonly string[],
  ): Observable<void>;
}
