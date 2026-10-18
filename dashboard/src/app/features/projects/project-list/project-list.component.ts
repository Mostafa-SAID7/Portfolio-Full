// src/app/features/projects/project-list/project-list.component.ts
import { Component, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProjectService } from '../../../core/services/project.service';
import { Project } from '../../../core/models/project.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-project-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="space-y-6">
      <div class="flex justify-between items-center">
        <h1 class="text-3xl font-bold text-gray-900 dark:text-white">Projects</h1>
        <a
          routerLink="/projects/new"
          class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
        >
          + New Project
        </a>
      </div>

          @if (loading()) {
        <div class="flex items-center justify-center py-12" role="status" aria-live="polite">
          <svg class="animate-spin h-12 w-12 text-blue-600" fill="none" viewBox="0 0 24 24" aria-hidden="true">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span class="sr-only">Loading projects...</span>
        </div>
          } @else if (visibleProjects().length === 0) {
        <div class="text-center py-12">
          <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <h3 class="mt-2 text-sm font-medium text-gray-900 dark:text-white">No projects</h3>
          <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Get started by creating a new project.</p>
          <div class="mt-6">
            <a
              routerLink="/projects/new"
              class="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              + New Project
            </a>
          </div>
        </div>
      } @else {
        <div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          @for (project of visibleProjects(); track project.id) {
            <div class="bg-white dark:bg-gray-800 overflow-hidden shadow rounded-lg hover:shadow-lg transition-shadow">
              <div class="p-6">
                <div class="flex items-center justify-between mb-4">
                  <div class="w-4 h-4 rounded-full flex-shrink-0" [style.background-color]="project.color"></div>
                  <div class="flex gap-2">
                    <button
                      [routerLink]="['/projects', project.id, 'edit']"
                      class="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 focus:outline-none focus:ring-2 focus:ring-blue-500 rounded p-1"
                      [attr.aria-label]="'Edit ' + project.name"
                      type="button"
                    >
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                      </svg>
                    </button>
                    <button
                      (click)="deleteProject(project.id, project.name)"
                      class="text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300 focus:outline-none focus:ring-2 focus:ring-red-500 rounded p-1"
                      [attr.aria-label]="'Delete ' + project.name"
                      type="button"
                    >
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                </div>
                <a [routerLink]="['/projects', project.id]" class="block focus:outline-none focus:ring-2 focus:ring-blue-500 rounded">
                  <h3 class="text-lg font-medium text-gray-900 dark:text-white mb-2">{{ project.name }}</h3>
                  <p class="text-sm text-gray-600 dark:text-gray-400 line-clamp-3">{{ project.description }}</p>
                </a>
                <div class="mt-4 flex items-center text-sm text-gray-500 dark:text-gray-400">
                  <svg class="mr-1.5 h-5 w-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
                  {{ project.createdAt | date:'MMM d, y' }}
                </div>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class ProjectListComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly authService = inject(AuthService);

  readonly projects = this.projectService.projects;
  readonly loading = this.projectService.loading;
  readonly visibleProjects = computed(() => this.authService.isDemoSession() ? this.demoProjects : this.projects());

  private readonly demoProjects: Project[] = [
    { id: 'brand-refresh', name: 'Brand refresh', description: 'A sharper identity for the next chapter.', color: '#ff6b35', createdAt: new Date(), updatedAt: new Date(), ownerId: 'demo', teamIds: [] },
    { id: 'mobile-flows', name: 'Mobile flows', description: 'Make every tap feel effortless.', color: '#9b8cff', createdAt: new Date(), updatedAt: new Date(), ownerId: 'demo', teamIds: [] },
    { id: 'launch-week', name: 'Launch week', description: 'Everything we need to ship with confidence.', color: '#7ee2c0', createdAt: new Date(), updatedAt: new Date(), ownerId: 'demo', teamIds: [] }
  ];

  ngOnInit(): void {
    this.projectService.loadProjects().subscribe({ error: () => undefined });
  }

  deleteProject(id: string, name: string): void {
    if (confirm(`Are you sure you want to delete "${name}"? This will also delete all associated tasks.`)) {
      this.projectService.deleteProject(id).subscribe();
    }
  }
}