/** A user as the client knows them after signing in. */
export interface CurrentUser {
  readonly id: string;
  readonly email: string;
  readonly displayName: string;
  readonly roles: readonly string[];
  readonly permissions: readonly string[];
}

/** The tokens issued by a sign-in or a refresh, together with the profile they describe. */
export interface AuthenticationResult {
  readonly accessToken: string;
  readonly accessTokenExpiresAtUtc: string;
  readonly refreshToken: string;
  readonly refreshTokenExpiresAtUtc: string;
  readonly user: CurrentUser;
}

/** The credentials presented at sign-in. */
export interface Credentials {
  readonly email: string;
  readonly password: string;
}

/**
 * The permission names the client checks before offering an action.
 *
 * Hiding a control the caller cannot use is a courtesy, not a security boundary: the server enforces
 * the same names in its application layer and rejects the request regardless of what the client shows.
 */
export const Permissions = {
  ItemsRead: 'items.read',
  ItemsCreate: 'items.create',
  ItemsUpdate: 'items.update',
  ItemsDelete: 'items.delete',
  ItemsSubmit: 'items.submit',
  ItemsReview: 'items.review',
  ItemsPublish: 'items.publish',
  ExamsRead: 'exams.read',
  ExamsCreate: 'exams.create',
  ExamsUpdate: 'exams.update',
  ExamsDelete: 'exams.delete',
  ExamsPublish: 'exams.publish',
  TaxonomyManage: 'taxonomy.manage',
  UsersRead: 'users.read',
  UsersManage: 'users.manage',
  RolesManage: 'roles.manage',
} as const;

/** The union of every permission name the client references. */
export type PermissionName = (typeof Permissions)[keyof typeof Permissions];
