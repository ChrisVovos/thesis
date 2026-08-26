import type { Observable } from 'rxjs';
import type { PagedResult } from '../../shared/models/paging.models';
import type {
  Permission,
  Role,
  RoleDraft,
  User,
  UserDraft,
  UserQuery,
  UserUpdate,
} from '../../shared/models/user.models';

/** The administration contract, expressed purely in domain terms. */
export abstract class UsersGateway {
  /**
   * Searches, sorts and pages the user directory.
   *
   * @param query The criteria supplied by the screen.
   */
  abstract search(query: UserQuery): Observable<PagedResult<User>>;

  /**
   * Reads a single user.
   *
   * @param id The identity of the user.
   */
  abstract getById(id: string): Observable<User>;

  /**
   * Creates a user account.
   *
   * @param draft The account to create.
   * @returns The identity of the new user.
   */
  abstract create(draft: UserDraft): Observable<string>;

  /**
   * Replaces the profile and role assignment of a user.
   *
   * @param id The identity of the user.
   * @param update The new profile.
   */
  abstract update(id: string, update: UserUpdate): Observable<void>;

  /**
   * Activates or deactivates a user account.
   *
   * @param id The identity of the user.
   * @param isActive Whether the user may sign in.
   */
  abstract setActive(id: string, isActive: boolean): Observable<void>;

  /** Reads every role together with its permissions. */
  abstract roles(): Observable<readonly Role[]>;

  /** Reads the permission catalogue. */
  abstract permissions(): Observable<readonly Permission[]>;

  /**
   * Creates a role.
   *
   * @param draft The role to create.
   * @returns The identity of the new role.
   */
  abstract createRole(draft: RoleDraft): Observable<string>;

  /**
   * Replaces the description and permission set of a role.
   *
   * @param id The identity of the role.
   * @param draft The new definition.
   */
  abstract updateRole(id: string, draft: RoleDraft): Observable<void>;

  /**
   * Deletes a role that no user holds.
   *
   * @param id The identity of the role.
   */
  abstract removeRole(id: string): Observable<void>;
}
