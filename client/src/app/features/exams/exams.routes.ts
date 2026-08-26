import type { Routes } from '@angular/router';
import { requirePermission } from '../../core/auth/auth.guards';
import { Permissions } from '../../shared/models/auth.models';

/** The exam builder routes. */
export const examRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./exam-list.page').then((m) => m.ExamListPage),
    title: 'Exams',
  },
  {
    path: ':id',
    canMatch: [requirePermission(Permissions.ExamsRead)],
    loadComponent: () => import('./exam-builder.page').then((m) => m.ExamBuilderPage),
    title: 'Exam builder',
  },
];
