import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MetricsCollector } from '../../core/metrics/metrics-collector';
import type {
  ExamDetail,
  ExamDraft,
  ExamQuery,
  ExamSectionDraft,
  ExamSummary,
  ExamTransition,
} from '../../shared/models/exam.models';
import { toPagedResult, type PagedResult } from '../../shared/models/paging.models';
import { ExamsGateway } from '../gateways/exams.gateway';
import { measured } from '../measurement';

interface RestPage<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

const TRANSITION_ROUTES: Readonly<Record<ExamTransition, string>> = {
  publish: 'publish',
  archive: 'archive',
  returnToDraft: 'return-to-draft',
};

/** The REST implementation of {@link ExamsGateway}. */
@Injectable({ providedIn: 'root' })
export class RestExamsGateway extends ExamsGateway {
  private readonly http = inject(HttpClient);
  private readonly metrics = inject(MetricsCollector);
  private readonly baseUrl = `${environment.restBaseUrl}/exams`;

  /** @inheritdoc */
  override search(query: ExamQuery): Observable<PagedResult<ExamSummary>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.sortBy) {
      params = params.set('sortBy', query.sortBy);
    }
    if (query.sortDescending) {
      params = params.set('sortDescending', true);
    }
    if (query.ownerId) {
      params = params.set('ownerId', query.ownerId);
    }
    for (const status of query.statuses ?? []) {
      params = params.append('status', status);
    }

    return measured(this.metrics, 'rest', 'exams.search', 1, () =>
      this.http
        .get<RestPage<ExamSummary>>(this.baseUrl, { params })
        .pipe(map((page) => toPagedResult(page.items, page.totalCount, page.page, page.pageSize))),
    );
  }

  /** @inheritdoc */
  override getById(id: string): Observable<ExamDetail> {
    return measured(this.metrics, 'rest', 'exams.getById', 1, () =>
      this.http.get<ExamDetail>(`${this.baseUrl}/${id}`),
    );
  }

  /** @inheritdoc */
  override create(draft: ExamDraft): Observable<string> {
    return measured(this.metrics, 'rest', 'exams.create', 1, () =>
      this.http.post<{ id: string }>(this.baseUrl, draft).pipe(map((created) => created.id)),
    );
  }

  /** @inheritdoc */
  override update(id: string, draft: ExamDraft): Observable<void> {
    return measured(this.metrics, 'rest', 'exams.update', 1, () =>
      this.http.put<void>(`${this.baseUrl}/${id}`, { examId: id, ...draft }),
    );
  }

  /** @inheritdoc */
  override remove(id: string): Observable<void> {
    return measured(this.metrics, 'rest', 'exams.delete', 1, () =>
      this.http.delete<void>(`${this.baseUrl}/${id}`),
    );
  }

  /** @inheritdoc */
  override transition(id: string, transition: ExamTransition): Observable<void> {
    return measured(this.metrics, 'rest', `exams.${transition}`, 1, () =>
      this.http.post<void>(`${this.baseUrl}/${id}/${TRANSITION_ROUTES[transition]}`, null),
    );
  }

  /** @inheritdoc */
  override addSection(examId: string, draft: ExamSectionDraft): Observable<string> {
    return measured(this.metrics, 'rest', 'exams.addSection', 1, () =>
      this.http
        .post<{ id: string }>(`${this.baseUrl}/${examId}/sections`, {
          examId,
          title: draft.title,
          instructions: draft.instructions ?? null,
        })
        .pipe(map((created) => created.id)),
    );
  }

  /** @inheritdoc */
  override updateSection(
    examId: string,
    sectionId: string,
    draft: ExamSectionDraft,
  ): Observable<void> {
    return measured(this.metrics, 'rest', 'exams.updateSection', 1, () =>
      this.http.put<void>(`${this.baseUrl}/${examId}/sections/${sectionId}`, {
        examId,
        sectionId,
        title: draft.title,
        instructions: draft.instructions ?? null,
      }),
    );
  }

  /** @inheritdoc */
  override removeSection(examId: string, sectionId: string): Observable<void> {
    return measured(this.metrics, 'rest', 'exams.removeSection', 1, () =>
      this.http.delete<void>(`${this.baseUrl}/${examId}/sections/${sectionId}`),
    );
  }

  /** @inheritdoc */
  override reorderSections(examId: string, orderedSectionIds: readonly string[]): Observable<void> {
    return measured(this.metrics, 'rest', 'exams.reorderSections', 1, () =>
      this.http.put<void>(`${this.baseUrl}/${examId}/sections/order`, orderedSectionIds),
    );
  }

  /** @inheritdoc */
  override addItem(
    examId: string,
    sectionId: string,
    itemId: string,
    scoreOverride?: number | null,
  ): Observable<string> {
    return measured(this.metrics, 'rest', 'exams.addItem', 1, () =>
      this.http
        .post<{ id: string }>(`${this.baseUrl}/${examId}/sections/${sectionId}/items`, {
          examId,
          sectionId,
          itemId,
          scoreOverride: scoreOverride ?? null,
        })
        .pipe(map((created) => created.id)),
    );
  }

  /** @inheritdoc */
  override removeItem(examId: string, sectionId: string, examItemId: string): Observable<void> {
    return measured(this.metrics, 'rest', 'exams.removeItem', 1, () =>
      this.http.delete<void>(
        `${this.baseUrl}/${examId}/sections/${sectionId}/items/${examItemId}`,
      ),
    );
  }

  /** @inheritdoc */
  override reorderItems(
    examId: string,
    sectionId: string,
    orderedExamItemIds: readonly string[],
  ): Observable<void> {
    return measured(this.metrics, 'rest', 'exams.reorderItems', 1, () =>
      this.http.put<void>(
        `${this.baseUrl}/${examId}/sections/${sectionId}/items/order`,
        orderedExamItemIds,
      ),
    );
  }
}
