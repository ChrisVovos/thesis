import type { PagedQuery } from './paging.models';

/** A capability that can be granted to a role. */
export interface Permission {
  readonly id: string;
  readonly name: string;
  readonly description: string;
}

/** A named bundle of permissions. */
export interface Role {
  readonly id: string;
  readonly name: string;
  readonly description: string;
  readonly isSystemRole: boolean;
  readonly permissions: readonly Permission[];
  readonly userCount: number;
}

/** A person who signs in to the platform. */
export interface User {
  readonly id: string;
  readonly email: string;
  readonly displayName: string;
  readonly isActive: boolean;
  readonly lastSignInAtUtc?: string | null;
  readonly createdAtUtc: string;
  readonly roles: readonly Role[];
}

/** The search, sort and paging criteria of the user directory. */
export interface UserQuery extends PagedQuery {
  readonly isActive?: boolean;
  readonly roleId?: string;
}

/** Everything needed to create a user account. */
export interface UserDraft {
  readonly email: string;
  readonly displayName: string;
  readonly password: string;
  readonly roleIds: readonly string[];
}

/** Everything needed to replace the profile and role assignment of a user. */
export interface UserUpdate {
  readonly email: string;
  readonly displayName: string;
  readonly roleIds: readonly string[];
}

/** Everything needed to create or replace a role. */
export interface RoleDraft {
  readonly name: string;
  readonly description: string;
  readonly permissionNames: readonly string[];
}
