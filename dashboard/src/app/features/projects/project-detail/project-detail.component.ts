import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { ProjectService } from '../../../core/services/project.service';

@Component({
  selector: 'app-project-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="space-y-6">
      @if (loading()) {
        <div class="flex items-center justify-center py-12" role="status" aria-live="polite">
          <svg class="animate-spin h-12 w-12 text-blue-600" fill="none" viewBox="0 0 24 24" aria-hidden="true">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span class="sr-only">Loading project...</span>
        </div>
      } @else if (project()) {
        <div class="flex justify-between items-center">
          <div class="flex items-center gap-3">
            <div class="w-4 h-4 rounded-full" [style.background-color]="project()!.color"></div>
            <h1 class="text-3xl font-bold text-gray-900 dark:text-white">{{ project()!.name }}</h1>
          </div>
          <div class="flex gap-3">
            <a
              [routerLink]="['/projects', project()!.id, 'edit']"
              class="px-4 py-2 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
            >
              Edit
            </a>
            <a
              routerLink="/projects"
              class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
            >
              Back to Projects
            </a>
          </div>
        </div>

        <div class="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 dark:text-white mb-2">Description</h2>
          <p class="text-gray-600 dark:text-gray-400">{{ project()!.description }}</p>
        </div>
      } @else {
        <div class="text-center py-12">
          <p class="text-gray-500 dark:text-gray-400">Project not found.</p>
          <a routerLink="/projects" class="mt-4 inline-block text-blue-600 hover:underline">Back to Projects</a>
        </div>
      }
    </div>
  `
})
export class ProjectDetailComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = this.projectService.loading;

  project = () => {
    const id = this.route.snapshot.paramMap.get('id');
    return this.projectService.projects().find(p => p.id === id) ?? null;
  };

  ngOnInit(): void {
    this.projectService.loadProjects().subscribe();
  }
}
