import type { ItemSummary } from './item.models';
import type { PagedQuery } from './paging.models';

/** The lifecycle of an assembled examination. */
export type ExamStatus = 'Draft' | 'Published' | 'Archived';

/** Every exam status, in workflow order. */
export const EXAM_STATUSES: readonly ExamStatus[] = ['Draft', 'Published', 'Archived'];

/** The placement of a bank item inside an exam section. */
export interface ExamItem {
  readonly id: string;
  readonly itemId: string;
  readonly position: number;
  readonly scoreOverride?: number | null;
  readonly effectiveScore: number;
  readonly item?: ItemSummary | null;
}

/** A titled group of items inside an exam. */
export interface ExamSection {
  readonly id: string;
  readonly title: string;
  readonly instructions?: string | null;
  readonly position: number;
  readonly items: readonly ExamItem[];
}

/** The list projection of an exam. */
export interface ExamSummary {
  readonly id: string;
  readonly title: string;
  readonly description?: string | null;
  readonly status: ExamStatus;
  readonly timeLimitMinutes?: number | null;
  readonly passingScorePercentage: number;
  readonly ownerId: string;
  readonly ownerName: string;
  readonly sectionCount: number;
  readonly itemCount: number;
  readonly totalScore: number;
  readonly createdAtUtc: string;
  readonly publishedAtUtc?: string | null;
}

/** The full projection of an exam, as the builder and the preview need it. */
export interface ExamDetail {
  readonly summary: ExamSummary;
  readonly sections: readonly ExamSection[];
  readonly compositionViolations: readonly string[];
}

/** The search, filter, sort and paging criteria of the exam list. */
export interface ExamQuery extends PagedQuery {
  readonly statuses?: readonly ExamStatus[];
  readonly ownerId?: string;
}

/** Everything needed to create or replace the editorial details of an exam. */
export interface ExamDraft {
  readonly title: string;
  readonly description?: string | null;
  readonly timeLimitMinutes?: number | null;
  readonly passingScorePercentage: number;
}

/** The editorial details of a section. */
export interface ExamSectionDraft {
  readonly title: string;
  readonly instructions?: string | null;
}

/** The lifecycle transitions a client can request on an exam. */
export type ExamTransition = 'publish' | 'archive' | 'returnToDraft';
