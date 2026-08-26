import type { Routes } from '@angular/router';
import { requirePermission } from '../../core/auth/auth.guards';
import { Permissions } from '../../shared/models/auth.models';

/** The administration routes. */
export const adminRoutes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'users' },
  {
    path: 'users',
    canMatch: [requirePermission(Permissions.UsersRead)],
    loadComponent: () => import('./users.page').then((m) => m.UsersPage),
    title: 'Users',
  },
  {
    path: 'roles',
    canMatch: [requirePermission(Permissions.UsersRead)],
    loadComponent: () => import('./roles.page').then((m) => m.RolesPage),
    title: 'Roles',
  },
];
