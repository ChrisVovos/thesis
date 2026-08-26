/** Internal type. DO NOT USE DIRECTLY. */
type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
/** Internal type. DO NOT USE DIRECTLY. */
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
/** Places an existing bank item into a section of a draft exam. */
export type AddExamItemCommandInput = {
  /** The exam to change. */
  examId: string;
  /** The bank item to place. */
  itemId: string;
  /** An optional exam specific score. */
  scoreOverride?: number | null | undefined;
  /** The section to append to. */
  sectionId: string;
};

/** Appends a section to a draft exam. */
export type AddExamSectionCommandInput = {
  /** The exam to change. */
  examId: string;
  /** Optional candidate instructions. */
  instructions?: string | null | undefined;
  /** The section title. */
  title: string;
};

/** Creates a draft exam. */
export type CreateExamCommandInput = {
  /** An optional description. */
  description?: string | null | undefined;
  /** The percentage of the total score required to pass. */
  passingScorePercentage: number;
  /** An optional delivery time limit in minutes. */
  timeLimitMinutes?: number | null | undefined;
  /** The exam title. */
  title: string;
};

/** Creates a draft item of any supported answer shape. */
export type CreateItemCommandInput = {
  /** The category the item is filed under. */
  categoryId: string;
  /** The cognitive demand of the item. */
  difficulty: DifficultyLevel;
  /** The score a fully correct response is worth. */
  maximumScore: number;
  /** The answer options, required for every shape except essay. */
  options?: Array<ItemOptionInput> | null | undefined;
  /** The grading guidance, required for essay items. */
  rubric?: EssayRubricInput | null | undefined;
  /** An optional exemplar answer for essay items. */
  sampleAnswer?: string | null | undefined;
  /** The prompt shown to the examinee. */
  stem: string;
  /** The tags to attach to the new item. */
  tagIds?: Array<string> | null | undefined;
  /** The answer shape to create. */
  type: ItemType;
};

/** Creates a role and grants it a set of permissions. */
export type CreateRoleCommandInput = {
  /** The human readable explanation. */
  description: string;
  /** The role name. */
  name: string;
  /** The permissions to grant. */
  permissionNames: Array<string>;
};

/** Creates a user account and assigns their initial roles. */
export type CreateUserCommandInput = {
  /** The human readable name. */
  displayName: string;
  /** The login identifier. */
  email: string;
  /** The initial plaintext password. */
  password: string;
  /** The roles to assign. */
  roleIds: Array<string>;
};

/** The cognitive demand of an item, used when balancing an exam blueprint. */
export type DifficultyLevel =
  /** Straightforward application of a single concept. */
  | 'EASY'
  /** Analysis across several concepts. */
  | 'HARD'
  /** Application of a concept in a familiar context. */
  | 'MEDIUM'
  /** Recall of a single fact. */
  | 'VERY_EASY'
  /** Synthesis or evaluation in an unfamiliar context. */
  | 'VERY_HARD';

/** Grading guidance supplied by a client when authoring an essay item. */
export type EssayRubricInput = {
  /** The guidance a grader applies to a response. */
  guidance: string;
  /** The maximum number of words a response may contain. */
  maximumWords: number;
  /** The minimum number of words a response must contain. */
  minimumWords: number;
};

/** Searches, filters, sorts and pages the exam list. */
export type ExamSearchCriteriaInput = {
  /** Gets the owning instructor to restrict the search to. */
  ownerId?: string | null | undefined;
  /** Gets the one based index of the requested page. */
  page: number;
  /** Gets the requested page size, clamped to MaxPageSize. */
  pageSize: number;
  /** Gets the free-text search term, when one was supplied. */
  search?: string | null | undefined;
  /** Gets the property to sort by, when one was supplied. */
  sortBy?: string | null | undefined;
  /** Gets a value indicating whether the sort is descending. */
  sortDescending: boolean;
  /** Gets the lifecycle statuses to include; all statuses when empty. */
  statuses?: Array<ExamStatus> | null | undefined;
};

/** The lifecycle of an assembled examination. */
export type ExamStatus =
  /** The exam is withdrawn from delivery but retained for audit. */
  | 'ARCHIVED'
  /** The exam is being assembled and its composition may change. */
  | 'DRAFT'
  /** The exam is frozen and may be delivered. */
  | 'PUBLISHED';

/** An answer option supplied by a client when authoring a choice item. */
export type ItemOptionInput = {
  /** An optional rationale shown after answering. */
  feedback?: string | null | undefined;
  /** Whether selecting the option scores. */
  isCorrect: boolean;
  /** The option text. */
  text: string;
};

/** Searches, filters, sorts and pages the item bank. */
export type ItemSearchCriteriaInput = {
  /** Gets the author to restrict the search to. */
  authorId?: string | null | undefined;
  /** Gets the category to restrict the search to. */
  categoryId?: string | null | undefined;
  /** Gets the difficulty levels to include; all levels when empty. */
  difficulties?: Array<DifficultyLevel> | null | undefined;
  /** Gets the one based index of the requested page. */
  page: number;
  /** Gets the requested page size, clamped to MaxPageSize. */
  pageSize: number;
  /** Gets the free-text search term, when one was supplied. */
  search?: string | null | undefined;
  /** Gets the property to sort by, when one was supplied. */
  sortBy?: string | null | undefined;
  /** Gets a value indicating whether the sort is descending. */
  sortDescending: boolean;
  /** Gets the lifecycle statuses to include; all statuses when empty. */
  statuses?: Array<ItemStatus> | null | undefined;
  /** Gets the tags an item must carry to be included. */
  tagIds?: Array<string> | null | undefined;
  /** Gets the answer shapes to include; all shapes when empty. */
  types?: Array<ItemType> | null | undefined;
};

/** The editorial lifecycle of an item. */
export type ItemStatus =
  /** A reviewer accepted the item; it may now be published. */
  | 'APPROVED'
  /** The item is being authored and is freely editable. */
  | 'DRAFT'
  /** The item has been submitted and is awaiting a reviewer decision. */
  | 'IN_REVIEW'
  /** The item is frozen, versioned and usable in exams. */
  | 'PUBLISHED'
  /** The item is withdrawn from further use but retained for audit. */
  | 'RETIRED';

/** The answer and scoring shapes supported by the authoring platform. */
export type ItemType =
  /** A binary decision such as true/false or agree/disagree. */
  | 'EITHER_OR'
  /** A free text response graded against a rubric. */
  | 'ESSAY'
  /** A stem with several options of which one or more are correct. */
  | 'MULTIPLE_CHOICE_MULTIPLE_RESPONSE'
  /** A stem with several options of which exactly one is correct. */
  | 'MULTIPLE_CHOICE_SINGLE_RESPONSE';

/** Exchanges an e-mail address and password for a token pair. */
export type LoginCommandInput = {
  /** The login identifier. */
  email: string;
  /** The plaintext password. */
  password: string;
};

/** Removes a placement from a section. */
export type RemoveExamItemCommandInput = {
  /** The exam to change. */
  examId: string;
  /** The placement to remove. */
  examItemId: string;
  /** The section holding the placement. */
  sectionId: string;
};

/** Removes a section together with all of its placements. */
export type RemoveExamSectionCommandInput = {
  /** The exam to change. */
  examId: string;
  /** The section to remove. */
  sectionId: string;
};

/** Reorders the placements inside a section. */
export type ReorderExamItemsCommandInput = {
  /** The exam to change. */
  examId: string;
  /** Every placement of the section, in the desired order. */
  orderedExamItemIds: Array<string>;
  /** The section to reorder. */
  sectionId: string;
};

/** Reorders the sections of an exam. */
export type ReorderExamSectionsCommandInput = {
  /** The exam to change. */
  examId: string;
  /** Every section of the exam, in the desired order. */
  orderedSectionIds: Array<string>;
};

/** Activates or deactivates a user account. */
export type SetUserActiveCommandInput = {
  /** Whether the user may sign in. */
  isActive: boolean;
  /** The user to change. */
  userId: string;
};

/** Replaces the editorial details of a draft exam. */
export type UpdateExamCommandInput = {
  /** The new description. */
  description?: string | null | undefined;
  /** The exam to update. */
  examId: string;
  /** The new passing score percentage. */
  passingScorePercentage: number;
  /** The new delivery time limit in minutes. */
  timeLimitMinutes?: number | null | undefined;
  /** The new title. */
  title: string;
};

/** Replaces the editorial details of a section. */
export type UpdateExamSectionCommandInput = {
  /** The exam to change. */
  examId: string;
  /** The new instructions. */
  instructions?: string | null | undefined;
  /** The section to update. */
  sectionId: string;
  /** The new title. */
  title: string;
};

/** Replaces the content of a draft item. */
export type UpdateItemCommandInput = {
  /** The new category. */
  categoryId: string;
  /** The new cognitive demand. */
  difficulty: DifficultyLevel;
  /** The item to update. */
  itemId: string;
  /** The new maximum score. */
  maximumScore: number;
  /** The new answer options, for every shape except essay. */
  options?: Array<ItemOptionInput> | null | undefined;
  /** The new grading guidance, for essay items. */
  rubric?: EssayRubricInput | null | undefined;
  /** The new exemplar answer, for essay items. */
  sampleAnswer?: string | null | undefined;
  /** The new prompt. */
  stem: string;
  /** The tags the item should carry afterwards. */
  tagIds?: Array<string> | null | undefined;
};

/** Replaces the description and permission set of a role. */
export type UpdateRoleCommandInput = {
  /** The new description. */
  description: string;
  /** The new role name; ignored for system roles. */
  name: string;
  /** The permissions the role should hold afterwards. */
  permissionNames: Array<string>;
  /** The role to update. */
  roleId: string;
};

/** Replaces the profile and role assignment of a user. */
export type UpdateUserCommandInput = {
  /** The new human readable name. */
  displayName: string;
  /** The new login identifier. */
  email: string;
  /** The roles the user should hold afterwards. */
  roleIds: Array<string>;
  /** The user to update. */
  userId: string;
};

/** Searches, sorts and pages the user directory. */
export type UserSearchCriteriaInput = {
  /** Gets the activation state to filter on, when one was supplied. */
  isActive?: boolean | null | undefined;
  /** Gets the one based index of the requested page. */
  page: number;
  /** Gets the requested page size, clamped to MaxPageSize. */
  pageSize: number;
  /** Gets the role to restrict the search to. */
  roleId?: string | null | undefined;
  /** Gets the free-text search term, when one was supplied. */
  search?: string | null | undefined;
  /** Gets the property to sort by, when one was supplied. */
  sortBy?: string | null | undefined;
  /** Gets a value indicating whether the sort is descending. */
  sortDescending: boolean;
};

export type SearchItemsQueryVariables = Exact<{
  criteria: ItemSearchCriteriaInput;
}>;


export type SearchItemsQuery = { searchItems: { totalCount: number, page: number, pageSize: number, items: Array<{ id: string, type: ItemType, status: ItemStatus, difficulty: DifficultyLevel, stem: string, maximumScore: number, categoryId: string, categoryName: string, authorId: string, authorName: string, versionNumber: number, createdAtUtc: string, lastModifiedAtUtc: string | null, tags: Array<{ id: string, name: string }> }> } };

export type ItemByIdQueryVariables = Exact<{
  id: string;
}>;


export type ItemByIdQuery = { itemById: { rubricGuidance: string | null, rubricMinimumWords: number | null, rubricMaximumWords: number | null, sampleAnswer: string | null, summary: { id: string, type: ItemType, status: ItemStatus, difficulty: DifficultyLevel, stem: string, maximumScore: number, categoryId: string, categoryName: string, authorId: string, authorName: string, versionNumber: number, createdAtUtc: string, lastModifiedAtUtc: string | null, tags: Array<{ id: string, name: string }> }, options: Array<{ id: string, text: string, isCorrect: boolean, position: number, feedback: string | null }>, versions: Array<{ id: string, versionNumber: number, publishedAtUtc: string, stemText: string, difficulty: DifficultyLevel, maximumScore: number, options: Array<{ text: string, isCorrect: boolean, position: number, feedback: string | null }> }> } };

export type ItemVersionsQueryVariables = Exact<{
  itemId: string;
}>;


export type ItemVersionsQuery = { itemVersions: Array<{ id: string, versionNumber: number, publishedAtUtc: string, stemText: string, difficulty: DifficultyLevel, maximumScore: number, options: Array<{ text: string, isCorrect: boolean, position: number, feedback: string | null }> }> };

export type CreateItemMutationVariables = Exact<{
  input: CreateItemCommandInput;
}>;


export type CreateItemMutation = { createItem: string };

export type UpdateItemMutationVariables = Exact<{
  input: UpdateItemCommandInput;
}>;


export type UpdateItemMutation = { updateItem: boolean };

export type DeleteItemMutationVariables = Exact<{
  itemId: string;
}>;


export type DeleteItemMutation = { deleteItem: boolean };

export type SubmitItemMutationVariables = Exact<{
  itemId: string;
}>;


export type SubmitItemMutation = { submitItemForReview: boolean };

export type ApproveItemMutationVariables = Exact<{
  itemId: string;
}>;


export type ApproveItemMutation = { approveItem: boolean };

export type ReturnItemToDraftMutationVariables = Exact<{
  itemId: string;
}>;


export type ReturnItemToDraftMutation = { returnItemToDraft: boolean };

export type PublishItemMutationVariables = Exact<{
  itemId: string;
}>;


export type PublishItemMutation = { publishItem: boolean };

export type RetireItemMutationVariables = Exact<{
  itemId: string;
}>;


export type RetireItemMutation = { retireItem: boolean };

export type CategoriesQueryVariables = Exact<{ [key: string]: never; }>;


export type CategoriesQuery = { categories: Array<{ id: string, name: string, description: string | null, parentCategoryId: string | null, isActive: boolean, itemCount: number }> };

export type TagsQueryVariables = Exact<{ [key: string]: never; }>;


export type TagsQuery = { tags: Array<{ id: string, name: string, itemCount: number }> };

export type CreateTagMutationVariables = Exact<{
  name: string;
}>;


export type CreateTagMutation = { createTag: string };

export type LoginMutationVariables = Exact<{
  input: LoginCommandInput;
}>;


export type LoginMutation = { login: { accessToken: string, accessTokenExpiresAtUtc: string, refreshToken: string, refreshTokenExpiresAtUtc: string, user: { id: string, email: string, displayName: string, roles: Array<string>, permissions: Array<string> } } };

export type RefreshTokenMutationVariables = Exact<{
  refreshToken: string;
}>;


export type RefreshTokenMutation = { refreshToken: { accessToken: string, accessTokenExpiresAtUtc: string, refreshToken: string, refreshTokenExpiresAtUtc: string, user: { id: string, email: string, displayName: string, roles: Array<string>, permissions: Array<string> } } };

export type LogoutMutationVariables = Exact<{
  refreshToken: string;
}>;


export type LogoutMutation = { logout: boolean };

export type MeQueryVariables = Exact<{ [key: string]: never; }>;


export type MeQuery = { me: { id: string, email: string, displayName: string, roles: Array<string>, permissions: Array<string> } };

export type SearchExamsQueryVariables = Exact<{
  criteria: ExamSearchCriteriaInput;
}>;


export type SearchExamsQuery = { searchExams: { totalCount: number, page: number, pageSize: number, items: Array<{ id: string, title: string, description: string | null, status: ExamStatus, timeLimitMinutes: number | null, passingScorePercentage: number, ownerId: string, ownerName: string, sectionCount: number, itemCount: number, totalScore: number, createdAtUtc: string, publishedAtUtc: string | null }> } };

export type ExamByIdQueryVariables = Exact<{
  id: string;
}>;


export type ExamByIdQuery = { examById: { compositionViolations: Array<string>, summary: { id: string, title: string, description: string | null, status: ExamStatus, timeLimitMinutes: number | null, passingScorePercentage: number, ownerId: string, ownerName: string, sectionCount: number, itemCount: number, totalScore: number, createdAtUtc: string, publishedAtUtc: string | null }, sections: Array<{ id: string, title: string, instructions: string | null, position: number, items: Array<{ id: string, itemId: string, position: number, scoreOverride: number | null, effectiveScore: number, item: { id: string, stem: string, type: ItemType, status: ItemStatus, difficulty: DifficultyLevel, maximumScore: number, categoryName: string } | null }> }> } };

export type CreateExamMutationVariables = Exact<{
  input: CreateExamCommandInput;
}>;


export type CreateExamMutation = { createExam: string };

export type UpdateExamMutationVariables = Exact<{
  input: UpdateExamCommandInput;
}>;


export type UpdateExamMutation = { updateExam: boolean };

export type DeleteExamMutationVariables = Exact<{
  examId: string;
}>;


export type DeleteExamMutation = { deleteExam: boolean };

export type PublishExamMutationVariables = Exact<{
  examId: string;
}>;


export type PublishExamMutation = { publishExam: boolean };

export type ArchiveExamMutationVariables = Exact<{
  examId: string;
}>;


export type ArchiveExamMutation = { archiveExam: boolean };

export type ReturnExamToDraftMutationVariables = Exact<{
  examId: string;
}>;


export type ReturnExamToDraftMutation = { returnExamToDraft: boolean };

export type AddExamSectionMutationVariables = Exact<{
  input: AddExamSectionCommandInput;
}>;


export type AddExamSectionMutation = { addExamSection: string };

export type UpdateExamSectionMutationVariables = Exact<{
  input: UpdateExamSectionCommandInput;
}>;


export type UpdateExamSectionMutation = { updateExamSection: boolean };

export type RemoveExamSectionMutationVariables = Exact<{
  input: RemoveExamSectionCommandInput;
}>;


export type RemoveExamSectionMutation = { removeExamSection: boolean };

export type ReorderExamSectionsMutationVariables = Exact<{
  input: ReorderExamSectionsCommandInput;
}>;


export type ReorderExamSectionsMutation = { reorderExamSections: boolean };

export type AddExamItemMutationVariables = Exact<{
  input: AddExamItemCommandInput;
}>;


export type AddExamItemMutation = { addExamItem: string };

export type RemoveExamItemMutationVariables = Exact<{
  input: RemoveExamItemCommandInput;
}>;


export type RemoveExamItemMutation = { removeExamItem: boolean };

export type ReorderExamItemsMutationVariables = Exact<{
  input: ReorderExamItemsCommandInput;
}>;


export type ReorderExamItemsMutation = { reorderExamItems: boolean };

export type SearchUsersQueryVariables = Exact<{
  criteria: UserSearchCriteriaInput;
}>;


export type SearchUsersQuery = { searchUsers: { totalCount: number, page: number, pageSize: number, items: Array<{ id: string, email: string, displayName: string, isActive: boolean, lastSignInAtUtc: string | null, createdAtUtc: string, roles: Array<{ id: string, name: string, description: string, isSystemRole: boolean }> }> } };

export type RolesQueryVariables = Exact<{ [key: string]: never; }>;


export type RolesQuery = { roles: Array<{ id: string, name: string, description: string, isSystemRole: boolean, userCount: number, permissions: Array<{ id: string, name: string, description: string }> }> };

export type PermissionCatalogueQueryVariables = Exact<{ [key: string]: never; }>;


export type PermissionCatalogueQuery = { permissions: Array<{ id: string, name: string, description: string }> };

export type CreateUserMutationVariables = Exact<{
  input: CreateUserCommandInput;
}>;


export type CreateUserMutation = { createUser: string };

export type UpdateUserMutationVariables = Exact<{
  input: UpdateUserCommandInput;
}>;


export type UpdateUserMutation = { updateUser: boolean };

export type SetUserActiveMutationVariables = Exact<{
  input: SetUserActiveCommandInput;
}>;


export type SetUserActiveMutation = { setUserActive: boolean };

export type CreateRoleMutationVariables = Exact<{
  input: CreateRoleCommandInput;
}>;


export type CreateRoleMutation = { createRole: string };

export type UpdateRoleMutationVariables = Exact<{
  input: UpdateRoleCommandInput;
}>;


export type UpdateRoleMutation = { updateRole: boolean };

export type DeleteRoleMutationVariables = Exact<{
  roleId: string;
}>;


export type DeleteRoleMutation = { deleteRole: boolean };


export const SearchItemsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"SearchItems"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"criteria"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"ItemSearchCriteriaInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"searchItems"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"criteria"},"value":{"kind":"Variable","name":{"kind":"Name","value":"criteria"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"totalCount"}},{"kind":"Field","name":{"kind":"Name","value":"page"}},{"kind":"Field","name":{"kind":"Name","value":"pageSize"}},{"kind":"Field","name":{"kind":"Name","value":"items"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"stem"}},{"kind":"Field","name":{"kind":"Name","value":"maximumScore"}},{"kind":"Field","name":{"kind":"Name","value":"categoryId"}},{"kind":"Field","name":{"kind":"Name","value":"categoryName"}},{"kind":"Field","name":{"kind":"Name","value":"authorId"}},{"kind":"Field","name":{"kind":"Name","value":"authorName"}},{"kind":"Field","name":{"kind":"Name","value":"versionNumber"}},{"kind":"Field","name":{"kind":"Name","value":"createdAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"lastModifiedAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"tags"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}}]}}]}}]}}]}}]} as unknown as DocumentNode<SearchItemsQuery, SearchItemsQueryVariables>;
export const ItemByIdDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"ItemById"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"itemById"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"summary"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"stem"}},{"kind":"Field","name":{"kind":"Name","value":"maximumScore"}},{"kind":"Field","name":{"kind":"Name","value":"categoryId"}},{"kind":"Field","name":{"kind":"Name","value":"categoryName"}},{"kind":"Field","name":{"kind":"Name","value":"authorId"}},{"kind":"Field","name":{"kind":"Name","value":"authorName"}},{"kind":"Field","name":{"kind":"Name","value":"versionNumber"}},{"kind":"Field","name":{"kind":"Name","value":"createdAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"lastModifiedAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"tags"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}}]}}]}},{"kind":"Field","name":{"kind":"Name","value":"options"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"text"}},{"kind":"Field","name":{"kind":"Name","value":"isCorrect"}},{"kind":"Field","name":{"kind":"Name","value":"position"}},{"kind":"Field","name":{"kind":"Name","value":"feedback"}}]}},{"kind":"Field","name":{"kind":"Name","value":"rubricGuidance"}},{"kind":"Field","name":{"kind":"Name","value":"rubricMinimumWords"}},{"kind":"Field","name":{"kind":"Name","value":"rubricMaximumWords"}},{"kind":"Field","name":{"kind":"Name","value":"sampleAnswer"}},{"kind":"Field","name":{"kind":"Name","value":"versions"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"versionNumber"}},{"kind":"Field","name":{"kind":"Name","value":"publishedAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"stemText"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"maximumScore"}},{"kind":"Field","name":{"kind":"Name","value":"options"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"text"}},{"kind":"Field","name":{"kind":"Name","value":"isCorrect"}},{"kind":"Field","name":{"kind":"Name","value":"position"}},{"kind":"Field","name":{"kind":"Name","value":"feedback"}}]}}]}}]}}]}}]} as unknown as DocumentNode<ItemByIdQuery, ItemByIdQueryVariables>;
export const ItemVersionsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"ItemVersions"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"itemVersions"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"itemId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"versionNumber"}},{"kind":"Field","name":{"kind":"Name","value":"publishedAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"stemText"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"maximumScore"}},{"kind":"Field","name":{"kind":"Name","value":"options"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"text"}},{"kind":"Field","name":{"kind":"Name","value":"isCorrect"}},{"kind":"Field","name":{"kind":"Name","value":"position"}},{"kind":"Field","name":{"kind":"Name","value":"feedback"}}]}}]}}]}}]} as unknown as DocumentNode<ItemVersionsQuery, ItemVersionsQueryVariables>;
export const CreateItemDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateItem"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateItemCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createItem"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<CreateItemMutation, CreateItemMutationVariables>;
export const UpdateItemDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateItem"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateItemCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateItem"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<UpdateItemMutation, UpdateItemMutationVariables>;
export const DeleteItemDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"DeleteItem"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"deleteItem"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"itemId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}}}]}]}}]} as unknown as DocumentNode<DeleteItemMutation, DeleteItemMutationVariables>;
export const SubmitItemDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"SubmitItem"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"submitItemForReview"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"itemId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}}}]}]}}]} as unknown as DocumentNode<SubmitItemMutation, SubmitItemMutationVariables>;
export const ApproveItemDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"ApproveItem"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"approveItem"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"itemId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}}}]}]}}]} as unknown as DocumentNode<ApproveItemMutation, ApproveItemMutationVariables>;
export const ReturnItemToDraftDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"ReturnItemToDraft"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"returnItemToDraft"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"itemId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}}}]}]}}]} as unknown as DocumentNode<ReturnItemToDraftMutation, ReturnItemToDraftMutationVariables>;
export const PublishItemDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"PublishItem"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"publishItem"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"itemId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}}}]}]}}]} as unknown as DocumentNode<PublishItemMutation, PublishItemMutationVariables>;
export const RetireItemDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"RetireItem"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"retireItem"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"itemId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"itemId"}}}]}]}}]} as unknown as DocumentNode<RetireItemMutation, RetireItemMutationVariables>;
export const CategoriesDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"Categories"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"categories"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"parentCategoryId"}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"itemCount"}}]}}]}}]} as unknown as DocumentNode<CategoriesQuery, CategoriesQueryVariables>;
export const TagsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"Tags"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"tags"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"itemCount"}}]}}]}}]} as unknown as DocumentNode<TagsQuery, TagsQueryVariables>;
export const CreateTagDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateTag"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"name"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"String"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createTag"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"name"},"value":{"kind":"Variable","name":{"kind":"Name","value":"name"}}}]}]}}]} as unknown as DocumentNode<CreateTagMutation, CreateTagMutationVariables>;
export const LoginDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"Login"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"LoginCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"login"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"accessToken"}},{"kind":"Field","name":{"kind":"Name","value":"accessTokenExpiresAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"refreshToken"}},{"kind":"Field","name":{"kind":"Name","value":"refreshTokenExpiresAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"user"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"displayName"}},{"kind":"Field","name":{"kind":"Name","value":"roles"}},{"kind":"Field","name":{"kind":"Name","value":"permissions"}}]}}]}}]}}]} as unknown as DocumentNode<LoginMutation, LoginMutationVariables>;
export const RefreshTokenDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"RefreshToken"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"refreshToken"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"String"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"refreshToken"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"refreshToken"},"value":{"kind":"Variable","name":{"kind":"Name","value":"refreshToken"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"accessToken"}},{"kind":"Field","name":{"kind":"Name","value":"accessTokenExpiresAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"refreshToken"}},{"kind":"Field","name":{"kind":"Name","value":"refreshTokenExpiresAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"user"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"displayName"}},{"kind":"Field","name":{"kind":"Name","value":"roles"}},{"kind":"Field","name":{"kind":"Name","value":"permissions"}}]}}]}}]}}]} as unknown as DocumentNode<RefreshTokenMutation, RefreshTokenMutationVariables>;
export const LogoutDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"Logout"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"refreshToken"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"String"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"logout"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"refreshToken"},"value":{"kind":"Variable","name":{"kind":"Name","value":"refreshToken"}}}]}]}}]} as unknown as DocumentNode<LogoutMutation, LogoutMutationVariables>;
export const MeDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"Me"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"me"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"displayName"}},{"kind":"Field","name":{"kind":"Name","value":"roles"}},{"kind":"Field","name":{"kind":"Name","value":"permissions"}}]}}]}}]} as unknown as DocumentNode<MeQuery, MeQueryVariables>;
export const SearchExamsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"SearchExams"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"criteria"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"ExamSearchCriteriaInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"searchExams"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"criteria"},"value":{"kind":"Variable","name":{"kind":"Name","value":"criteria"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"totalCount"}},{"kind":"Field","name":{"kind":"Name","value":"page"}},{"kind":"Field","name":{"kind":"Name","value":"pageSize"}},{"kind":"Field","name":{"kind":"Name","value":"items"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"timeLimitMinutes"}},{"kind":"Field","name":{"kind":"Name","value":"passingScorePercentage"}},{"kind":"Field","name":{"kind":"Name","value":"ownerId"}},{"kind":"Field","name":{"kind":"Name","value":"ownerName"}},{"kind":"Field","name":{"kind":"Name","value":"sectionCount"}},{"kind":"Field","name":{"kind":"Name","value":"itemCount"}},{"kind":"Field","name":{"kind":"Name","value":"totalScore"}},{"kind":"Field","name":{"kind":"Name","value":"createdAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"publishedAtUtc"}}]}}]}}]}}]} as unknown as DocumentNode<SearchExamsQuery, SearchExamsQueryVariables>;
export const ExamByIdDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"ExamById"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"examById"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"summary"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"timeLimitMinutes"}},{"kind":"Field","name":{"kind":"Name","value":"passingScorePercentage"}},{"kind":"Field","name":{"kind":"Name","value":"ownerId"}},{"kind":"Field","name":{"kind":"Name","value":"ownerName"}},{"kind":"Field","name":{"kind":"Name","value":"sectionCount"}},{"kind":"Field","name":{"kind":"Name","value":"itemCount"}},{"kind":"Field","name":{"kind":"Name","value":"totalScore"}},{"kind":"Field","name":{"kind":"Name","value":"createdAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"publishedAtUtc"}}]}},{"kind":"Field","name":{"kind":"Name","value":"compositionViolations"}},{"kind":"Field","name":{"kind":"Name","value":"sections"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"instructions"}},{"kind":"Field","name":{"kind":"Name","value":"position"}},{"kind":"Field","name":{"kind":"Name","value":"items"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"itemId"}},{"kind":"Field","name":{"kind":"Name","value":"position"}},{"kind":"Field","name":{"kind":"Name","value":"scoreOverride"}},{"kind":"Field","name":{"kind":"Name","value":"effectiveScore"}},{"kind":"Field","name":{"kind":"Name","value":"item"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"stem"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"maximumScore"}},{"kind":"Field","name":{"kind":"Name","value":"categoryName"}}]}}]}}]}}]}}]}}]} as unknown as DocumentNode<ExamByIdQuery, ExamByIdQueryVariables>;
export const CreateExamDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateExam"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateExamCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createExam"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<CreateExamMutation, CreateExamMutationVariables>;
export const UpdateExamDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateExam"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateExamCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateExam"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<UpdateExamMutation, UpdateExamMutationVariables>;
export const DeleteExamDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"DeleteExam"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"examId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"deleteExam"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"examId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"examId"}}}]}]}}]} as unknown as DocumentNode<DeleteExamMutation, DeleteExamMutationVariables>;
export const PublishExamDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"PublishExam"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"examId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"publishExam"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"examId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"examId"}}}]}]}}]} as unknown as DocumentNode<PublishExamMutation, PublishExamMutationVariables>;
export const ArchiveExamDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"ArchiveExam"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"examId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"archiveExam"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"examId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"examId"}}}]}]}}]} as unknown as DocumentNode<ArchiveExamMutation, ArchiveExamMutationVariables>;
export const ReturnExamToDraftDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"ReturnExamToDraft"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"examId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"returnExamToDraft"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"examId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"examId"}}}]}]}}]} as unknown as DocumentNode<ReturnExamToDraftMutation, ReturnExamToDraftMutationVariables>;
export const AddExamSectionDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"AddExamSection"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"AddExamSectionCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"addExamSection"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<AddExamSectionMutation, AddExamSectionMutationVariables>;
export const UpdateExamSectionDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateExamSection"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateExamSectionCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateExamSection"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<UpdateExamSectionMutation, UpdateExamSectionMutationVariables>;
export const RemoveExamSectionDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"RemoveExamSection"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"RemoveExamSectionCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"removeExamSection"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<RemoveExamSectionMutation, RemoveExamSectionMutationVariables>;
export const ReorderExamSectionsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"ReorderExamSections"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"ReorderExamSectionsCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"reorderExamSections"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<ReorderExamSectionsMutation, ReorderExamSectionsMutationVariables>;
export const AddExamItemDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"AddExamItem"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"AddExamItemCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"addExamItem"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<AddExamItemMutation, AddExamItemMutationVariables>;
export const RemoveExamItemDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"RemoveExamItem"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"RemoveExamItemCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"removeExamItem"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<RemoveExamItemMutation, RemoveExamItemMutationVariables>;
export const ReorderExamItemsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"ReorderExamItems"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"ReorderExamItemsCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"reorderExamItems"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<ReorderExamItemsMutation, ReorderExamItemsMutationVariables>;
export const SearchUsersDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"SearchUsers"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"criteria"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UserSearchCriteriaInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"searchUsers"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"criteria"},"value":{"kind":"Variable","name":{"kind":"Name","value":"criteria"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"totalCount"}},{"kind":"Field","name":{"kind":"Name","value":"page"}},{"kind":"Field","name":{"kind":"Name","value":"pageSize"}},{"kind":"Field","name":{"kind":"Name","value":"items"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"displayName"}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"lastSignInAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"createdAtUtc"}},{"kind":"Field","name":{"kind":"Name","value":"roles"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"isSystemRole"}}]}}]}}]}}]}}]} as unknown as DocumentNode<SearchUsersQuery, SearchUsersQueryVariables>;
export const RolesDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"Roles"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"roles"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"isSystemRole"}},{"kind":"Field","name":{"kind":"Name","value":"userCount"}},{"kind":"Field","name":{"kind":"Name","value":"permissions"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"description"}}]}}]}}]}}]} as unknown as DocumentNode<RolesQuery, RolesQueryVariables>;
export const PermissionCatalogueDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"PermissionCatalogue"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"permissions"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"description"}}]}}]}}]} as unknown as DocumentNode<PermissionCatalogueQuery, PermissionCatalogueQueryVariables>;
export const CreateUserDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateUser"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateUserCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createUser"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<CreateUserMutation, CreateUserMutationVariables>;
export const UpdateUserDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateUser"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateUserCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateUser"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<UpdateUserMutation, UpdateUserMutationVariables>;
export const SetUserActiveDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"SetUserActive"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"SetUserActiveCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"setUserActive"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<SetUserActiveMutation, SetUserActiveMutationVariables>;
export const CreateRoleDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateRole"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateRoleCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createRole"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<CreateRoleMutation, CreateRoleMutationVariables>;
export const UpdateRoleDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateRole"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateRoleCommandInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateRole"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}]}]}}]} as unknown as DocumentNode<UpdateRoleMutation, UpdateRoleMutationVariables>;
export const DeleteRoleDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"DeleteRole"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"roleId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"deleteRole"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"roleId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"roleId"}}}]}]}}]} as unknown as DocumentNode<DeleteRoleMutation, DeleteRoleMutationVariables>;