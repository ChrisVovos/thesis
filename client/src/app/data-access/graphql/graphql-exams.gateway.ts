import { inject, Injectable } from '@angular/core';
import { Apollo } from 'apollo-angular';
import type { Observable } from 'rxjs';
import { MetricsCollector } from '../../core/metrics/metrics-collector';
import type {
  ExamDetail,
  ExamDraft,
  ExamQuery,
  ExamSection,
  ExamSectionDraft,
  ExamStatus,
  ExamSummary,
  ExamTransition,
} from '../../shared/models/exam.models';
import type { DifficultyLevel, ItemStatus, ItemType } from '../../shared/models/item.models';
import { toPagedResult, type PagedResult } from '../../shared/models/paging.models';
import { ExamsGateway } from '../gateways/exams.gateway';
import { measured } from '../measurement';
import { fromGraphQlEnum, toGraphQlEnums } from './enum-mapping';
import { runMutation, runQuery } from './graphql-execution';
import {
  ADD_EXAM_ITEM,
  ADD_EXAM_SECTION,
  ARCHIVE_EXAM,
  CREATE_EXAM,
  DELETE_EXAM,
  EXAM_BY_ID,
  PUBLISH_EXAM,
  REMOVE_EXAM_ITEM,
  REMOVE_EXAM_SECTION,
  REORDER_EXAM_ITEMS,
  REORDER_EXAM_SECTIONS,
  RETURN_EXAM_TO_DRAFT,
  SEARCH_EXAMS,
  UPDATE_EXAM,
  UPDATE_EXAM_SECTION,
} from './operation.documents';

interface RawExamSummary extends Omit<ExamSummary, 'status'> {
  readonly status: string;
}

interface RawExamDetail {
  readonly summary: RawExamSummary;
  readonly compositionViolations: readonly string[];
  readonly sections: readonly RawExamSection[];
}

interface RawExamSection extends Omit<ExamSection, 'items'> {
  readonly items: readonly RawExamItem[];
}

interface RawExamItem {
  readonly id: string;
  readonly itemId: string;
  readonly position: number;
  readonly scoreOverride: number | null;
  readonly effectiveScore: number;
  readonly item: {
    readonly id: string;
    readonly stem: string;
    readonly type: string;
    readonly status: string;
    readonly difficulty: string;
    readonly maximumScore: number;
    readonly categoryName: string;
  } | null;
}

interface RawPage<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

const TRANSITION_DOCUMENTS = {
  publish: PUBLISH_EXAM,
  archive: ARCHIVE_EXAM,
  returnToDraft: RETURN_EXAM_TO_DRAFT,
} as const;

/** The GraphQL implementation of {@link ExamsGateway}. */
@Injectable({ providedIn: 'root' })
export class GraphQlExamsGateway extends ExamsGateway {
  private readonly apollo = inject(Apollo);
  private readonly metrics = inject(MetricsCollector);

  /** @inheritdoc */
  override search(query: ExamQuery): Observable<PagedResult<ExamSummary>> {
    const criteria = {
      page: query.page,
      pageSize: query.pageSize,
      search: query.search ?? null,
      sortBy: query.sortBy ?? null,
      sortDescending: query.sortDescending ?? false,
      statuses: toGraphQlEnums(query.statuses) ?? null,
      ownerId: query.ownerId ?? null,
    };

    return measured(this.metrics, 'graphql', 'exams.search', 1, () =>
      runQuery<{ searchExams: RawPage<RawExamSummary> }, PagedResult<ExamSummary>>(
        this.apollo,
        SEARCH_EXAMS,
        (data) =>
          toPagedResult(
            data.searchExams.items.map(GraphQlExamsGateway.toSummary),
            data.searchExams.totalCount,
            data.searchExams.page,
            data.searchExams.pageSize,
          ),
        { criteria },
      ),
    );
  }

  /** @inheritdoc */
  override getById(id: string): Observable<ExamDetail> {
    return measured(this.metrics, 'graphql', 'exams.getById', 1, () =>
      runQuery<{ examById: RawExamDetail }, ExamDetail>(
        this.apollo,
        EXAM_BY_ID,
        (data) => ({
          summary: GraphQlExamsGateway.toSummary(data.examById.summary),
          compositionViolations: data.examById.compositionViolations,
          sections: data.examById.sections.map((section) => ({
            ...section,
            items: section.items.map((placement) => ({
              id: placement.id,
              itemId: placement.itemId,
              position: placement.position,
              scoreOverride: placement.scoreOverride,
              effectiveScore: placement.effectiveScore,
              item: placement.item
                ? {
                    id: placement.item.id,
                    stem: placement.item.stem,
                    type: fromGraphQlEnum<ItemType>(placement.item.type),
                    status: fromGraphQlEnum<ItemStatus>(placement.item.status),
                    difficulty: fromGraphQlEnum<DifficultyLevel>(placement.item.difficulty),
                    maximumScore: placement.item.maximumScore,
                    categoryId: '',
                    categoryName: placement.item.categoryName,
                    authorId: '',
                    authorName: '',
                    versionNumber: 0,
                    createdAtUtc: '',
                    tags: [],
                  }
                : null,
            })),
          })),
        }),
        { id },
      ),
    );
  }

  /** @inheritdoc */
  override create(draft: ExamDraft): Observable<string> {
    return measured(this.metrics, 'graphql', 'exams.create', 1, () =>
      runMutation<{ createExam: string }, string>(
        this.apollo,
        CREATE_EXAM,
        (data) => data.createExam,
        { input: GraphQlExamsGateway.toExamInput(draft) },
      ),
    );
  }

  /** @inheritdoc */
  override update(id: string, draft: ExamDraft): Observable<void> {
    return measured(this.metrics, 'graphql', 'exams.update', 1, () =>
      runMutation<{ updateExam: boolean }, void>(this.apollo, UPDATE_EXAM, () => undefined, {
        input: { examId: id, ...GraphQlExamsGateway.toExamInput(draft) },
      }),
    );
  }

  /** @inheritdoc */
  override remove(id: string): Observable<void> {
    return measured(this.metrics, 'graphql', 'exams.delete', 1, () =>
      runMutation<{ deleteExam: boolean }, void>(this.apollo, DELETE_EXAM, () => undefined, {
        examId: id,
      }),
    );
  }

  /** @inheritdoc */
  override transition(id: string, transition: ExamTransition): Observable<void> {
    return measured(this.metrics, 'graphql', `exams.${transition}`, 1, () =>
      runMutation<Record<string, boolean>, void>(
        this.apollo,
        TRANSITION_DOCUMENTS[transition],
        () => undefined,
        { examId: id },
      ),
    );
  }

  /** @inheritdoc */
  override addSection(examId: string, draft: ExamSectionDraft): Observable<string> {
    return measured(this.metrics, 'graphql', 'exams.addSection', 1, () =>
      runMutation<{ addExamSection: string }, string>(
        this.apollo,
        ADD_EXAM_SECTION,
        (data) => data.addExamSection,
        { input: { examId, title: draft.title, instructions: draft.instructions ?? null } },
      ),
    );
  }

  /** @inheritdoc */
  override updateSection(
    examId: string,
    sectionId: string,
    draft: ExamSectionDraft,
  ): Observable<void> {
    return measured(this.metrics, 'graphql', 'exams.updateSection', 1, () =>
      runMutation<{ updateExamSection: boolean }, void>(
        this.apollo,
        UPDATE_EXAM_SECTION,
        () => undefined,
        {
          input: {
            examId,
            sectionId,
            title: draft.title,
            instructions: draft.instructions ?? null,
          },
        },
      ),
    );
  }

  /** @inheritdoc */
  override removeSection(examId: string, sectionId: string): Observable<void> {
    return measured(this.metrics, 'graphql', 'exams.removeSection', 1, () =>
      runMutation<{ removeExamSection: boolean }, void>(
        this.apollo,
        REMOVE_EXAM_SECTION,
        () => undefined,
        { input: { examId, sectionId } },
      ),
    );
  }

  /** @inheritdoc */
  override reorderSections(examId: string, orderedSectionIds: readonly string[]): Observable<void> {
    return measured(this.metrics, 'graphql', 'exams.reorderSections', 1, () =>
      runMutation<{ reorderExamSections: boolean }, void>(
        this.apollo,
        REORDER_EXAM_SECTIONS,
        () => undefined,
        { input: { examId, orderedSectionIds: [...orderedSectionIds] } },
      ),
    );
  }

  /** @inheritdoc */
  override addItem(
    examId: string,
    sectionId: string,
    itemId: string,
    scoreOverride?: number | null,
  ): Observable<string> {
    return measured(this.metrics, 'graphql', 'exams.addItem', 1, () =>
      runMutation<{ addExamItem: string }, string>(
        this.apollo,
        ADD_EXAM_ITEM,
        (data) => data.addExamItem,
        { input: { examId, sectionId, itemId, scoreOverride: scoreOverride ?? null } },
      ),
    );
  }

  /** @inheritdoc */
  override removeItem(examId: string, sectionId: string, examItemId: string): Observable<void> {
    return measured(this.metrics, 'graphql', 'exams.removeItem', 1, () =>
      runMutation<{ removeExamItem: boolean }, void>(
        this.apollo,
        REMOVE_EXAM_ITEM,
        () => undefined,
        { input: { examId, sectionId, examItemId } },
      ),
    );
  }

  /** @inheritdoc */
  override reorderItems(
    examId: string,
    sectionId: string,
    orderedExamItemIds: readonly string[],
  ): Observable<void> {
    return measured(this.metrics, 'graphql', 'exams.reorderItems', 1, () =>
      runMutation<{ reorderExamItems: boolean }, void>(
        this.apollo,
        REORDER_EXAM_ITEMS,
        () => undefined,
        { input: { examId, sectionId, orderedExamItemIds: [...orderedExamItemIds] } },
      ),
    );
  }

  private static toSummary(raw: RawExamSummary): ExamSummary {
    return { ...raw, status: fromGraphQlEnum<ExamStatus>(raw.status) };
  }

  private static toExamInput(draft: ExamDraft): Record<string, unknown> {
    return {
      title: draft.title,
      description: draft.description ?? null,
      timeLimitMinutes: draft.timeLimitMinutes ?? null,
      passingScorePercentage: draft.passingScorePercentage,
    };
  }
}
