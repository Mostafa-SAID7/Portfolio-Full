import { Routes } from '@angular/router';

/**
 * Pure CRUD surface: card grid (list) + create/edit forms. There is no
 * project detail page — the public portfolio is the detail view.
 */
export const PROJECT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./project-list/project-list.component').then(m => m.ProjectListComponent)
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./project-form/project-form.component').then(m => m.ProjectFormComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () =>
      import('./project-form/project-form.component').then(m => m.ProjectFormComponent)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
