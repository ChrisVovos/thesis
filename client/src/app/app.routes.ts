import type { Routes } from '@angular/router';
import { anonymousOnlyGuard, authenticatedGuard, requirePermission } from './core/auth/auth.guards';
import { Permissions } from './shared/models/auth.models';

/**
 * The route table.
 *
 * Feature areas are lazy loaded and gated by the permission they need, so a reviewer never downloads
 * the administration screens and an author never downloads the exam builder. The guards are a user
 * experience measure only; the server authorizes every request regardless of what the router allows.
 */
export const routes: Routes = [
  {
    path: 'sign-in',
    canActivate: [anonymousOnlyGuard],
    loadComponent: () => import('./features/auth/sign-in.page').then((m) => m.SignInPage),
    title: 'Sign in',
  },
  {
    path: '',
    canActivate: [authenticatedGuard],
    loadComponent: () => import('./layout/shell').then((m) => m.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'items' },
      {
        path: 'items',
        canMatch: [requirePermission(Permissions.ItemsRead)],
        loadChildren: () => import('./features/items/items.routes').then((m) => m.itemRoutes),
      },
      {
        path: 'exams',
        canMatch: [requirePermission(Permissions.ExamsRead)],
        loadChildren: () => import('./features/exams/exams.routes').then((m) => m.examRoutes),
      },
      {
        path: 'administration',
        canMatch: [requirePermission(Permissions.UsersRead, Permissions.RolesManage)],
        loadChildren: () => import('./features/admin/admin.routes').then((m) => m.adminRoutes),
      },
      {
        path: 'benchmark',
        loadComponent: () =>
          import('./features/benchmark/benchmark.page').then((m) => m.BenchmarkPage),
        title: 'Benchmark',
      },
      {
        path: 'forbidden',
        loadComponent: () => import('./features/errors/forbidden.page').then((m) => m.ForbiddenPage),
        title: 'Not permitted',
      },
    ],
  },
  {
    path: '**',
    loadComponent: () => import('./features/errors/not-found.page').then((m) => m.NotFoundPage),
    title: 'Not found',
  },
];
