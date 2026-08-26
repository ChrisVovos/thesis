import type { Routes } from '@angular/router';
import { requirePermission } from '../../core/auth/auth.guards';
import { Permissions } from '../../shared/models/auth.models';

/** The item authoring routes. */
export const itemRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./item-list.page').then((m) => m.ItemListPage),
    title: 'Item bank',
  },
  {
    path: 'new',
    canMatch: [requirePermission(Permissions.ItemsCreate)],
    loadComponent: () => import('./item-editor.page').then((m) => m.ItemEditorPage),
    title: 'New item',
  },
  {
    path: ':id',
    loadComponent: () => import('./item-preview.page').then((m) => m.ItemPreviewPage),
    title: 'Item preview',
  },
  {
    path: ':id/edit',
    canMatch: [requirePermission(Permissions.ItemsUpdate)],
    loadComponent: () => import('./item-editor.page').then((m) => m.ItemEditorPage),
    title: 'Edit item',
  },
];
