import { Routes } from '@angular/router';

export const TASK_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./task-board/task-board.component').then(m => m.TaskBoardComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./task-form/task-form.component').then(m => m.TaskFormComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./task-form/task-form.component').then(m => m.TaskFormComponent)
  }
];