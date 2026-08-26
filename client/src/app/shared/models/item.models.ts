import type { PagedQuery } from './paging.models';

/** The answer and scoring shapes the platform supports. */
export type ItemType =
  | 'MultipleChoiceSingleResponse'
  | 'MultipleChoiceMultipleResponse'
  | 'Essay'
  | 'EitherOr';

/** The editorial lifecycle of an item. */
export type ItemStatus = 'Draft' | 'InReview' | 'Approved' | 'Published' | 'Retired';

/** The cognitive demand of an item. */
export type DifficultyLevel = 'VeryEasy' | 'Easy' | 'Medium' | 'Hard' | 'VeryHard';

/** Every answer shape, in the order the editor offers them. */
export const ITEM_TYPES: readonly ItemType[] = [
  'MultipleChoiceSingleResponse',
  'MultipleChoiceMultipleResponse',
  'EitherOr',
  'Essay',
];

/** Every lifecycle status, in workflow order. */
export const ITEM_STATUSES: readonly ItemStatus[] = [
  'Draft',
  'InReview',
  'Approved',
  'Published',
  'Retired',
];

/** Every difficulty level, from least to most demanding. */
export const DIFFICULTY_LEVELS: readonly DifficultyLevel[] = [
  'VeryEasy',
  'Easy',
  'Medium',
  'Hard',
  'VeryHard',
];

/** A tag attached to an item. */
export interface ItemTag {
  readonly id: string;
  readonly name: string;
}

/** A selectable answer option. */
export interface ItemOption {
  readonly id?: string;
  readonly text: string;
  readonly isCorrect: boolean;
  readonly position: number;
  readonly feedback?: string | null;
}

/** The list projection of an item. */
export interface ItemSummary {
  readonly id: string;
  readonly type: ItemType;
  readonly status: ItemStatus;
  readonly difficulty: DifficultyLevel;
  readonly stem: string;
  readonly maximumScore: number;
  readonly categoryId: string;
  readonly categoryName: string;
  readonly authorId: string;
  readonly authorName: string;
  readonly versionNumber: number;
  readonly createdAtUtc: string;
  readonly lastModifiedAtUtc?: string | null;
  readonly tags: readonly ItemTag[];
}

/** An immutable published version of an item. */
export interface ItemVersion {
  readonly id: string;
  readonly versionNumber: number;
  readonly publishedAtUtc: string;
  readonly stemText: string;
  readonly difficulty: DifficultyLevel;
  readonly maximumScore: number;
  readonly options: readonly ItemOption[];
}

/** The full projection of an item, as the editor and the preview need it. */
export interface ItemDetail {
  readonly summary: ItemSummary;
  readonly options: readonly ItemOption[];
  readonly rubricGuidance?: string | null;
  readonly rubricMinimumWords?: number | null;
  readonly rubricMaximumWords?: number | null;
  readonly sampleAnswer?: string | null;
  readonly versions: readonly ItemVersion[];
}

/** The search, filter, sort and paging criteria of the item bank. */
export interface ItemQuery extends PagedQuery {
  readonly types?: readonly ItemType[];
  readonly statuses?: readonly ItemStatus[];
  readonly difficulties?: readonly DifficultyLevel[];
  readonly categoryId?: string;
  readonly tagIds?: readonly string[];
  readonly authorId?: string;
}

/** The grading guidance of an essay item. */
export interface EssayRubricInput {
  readonly guidance: string;
  readonly minimumWords: number;
  readonly maximumWords: number;
}

/** Everything needed to create or replace an item, whatever its answer shape. */
export interface ItemDraft {
  readonly type: ItemType;
  readonly stem: string;
  readonly difficulty: DifficultyLevel;
  readonly categoryId: string;
  readonly maximumScore: number;
  readonly options?: readonly Omit<ItemOption, 'id' | 'position'>[];
  readonly rubric?: EssayRubricInput;
  readonly sampleAnswer?: string | null;
  readonly tagIds?: readonly string[];
}

/** A node in the item bank taxonomy. */
export interface Category {
  readonly id: string;
  readonly name: string;
  readonly description?: string | null;
  readonly parentCategoryId?: string | null;
  readonly isActive: boolean;
  readonly itemCount: number;
}

/** A free-form label attached to items. */
export interface Tag {
  readonly id: string;
  readonly name: string;
  readonly itemCount: number;
}

/** The lifecycle transitions a client can request on an item. */
export type ItemTransition = 'submit' | 'approve' | 'returnToDraft' | 'publish' | 'retire';
