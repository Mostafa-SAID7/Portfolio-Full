// src/app/features/projects/project-form/project-form.component.ts
import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ProjectService } from '../../../core/services/project.service';

@Component({
  selector: 'app-project-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="max-w-2xl mx-auto">
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-gray-900 dark:text-white">
          {{ isEditMode() ? 'Edit Project' : 'Create New Project' }}
        </h1>
      </div>

      <form [formGroup]="projectForm" (ngSubmit)="onSubmit()" class="bg-white dark:bg-gray-800 shadow rounded-lg p-6 space-y-6">
        @if (errorMessage()) {
          <div 
            role="alert" 
            class="rounded-md bg-red-50 dark:bg-red-900/20 p-4 border border-red-200 dark:border-red-800"
            aria-live="polite"
          >
            <p class="text-sm text-red-800 dark:text-red-300">{{ errorMessage() }}</p>
          </div>
        }

        <div>
          <label for="name" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Project Name <span class="text-red-500">*</span>
          </label>
          <input
            id="name"
            type="text"
            formControlName="name"
            required
            class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-white bg-white dark:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            [class.border-red-500]="projectForm.get('name')?.invalid && projectForm.get('name')?.touched"
            placeholder="Enter project name"
            [attr.aria-invalid]="projectForm.get('name')?.invalid && projectForm.get('name')?.touched"
            [attr.aria-describedby]="projectForm.get('name')?.invalid && projectForm.get('name')?.touched ? 'name-error' : null"
          />
          @if (projectForm.get('name')?.invalid && projectForm.get('name')?.touched) {
            <p id="name-error" class="mt-1 text-sm text-red-600 dark:text-red-400" role="alert">
              Project name is required
            </p>
          }
        </div>

        <div>
          <label for="description" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Description <span class="text-red-500">*</span>
          </label>
          <textarea
            id="description"
            formControlName="description"
            required
            rows="4"
            class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-white bg-white dark:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            [class.border-red-500]="projectForm.get('description')?.invalid && projectForm.get('description')?.touched"
            placeholder="Enter project description"
            [attr.aria-invalid]="projectForm.get('description')?.invalid && projectForm.get('description')?.touched"
            [attr.aria-describedby]="projectForm.get('description')?.invalid && projectForm.get('description')?.touched ? 'description-error' : null"
          ></textarea>
          @if (projectForm.get('description')?.invalid && projectForm.get('description')?.touched) {
            <p id="description-error" class="mt-1 text-sm text-red-600 dark:text-red-400" role="alert">
              Description is required
            </p>
          }
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">
            Project Color <span class="text-red-500">*</span>
          </label>
          <div class="flex flex-wrap gap-3" role="radiogroup" aria-label="Project color">
            @for (colorOption of colorOptions; track colorOption.value) {
              <label
                class="relative cursor-pointer focus-within:ring-2 focus-within:ring-blue-500 rounded-full"
                [attr.aria-label]="colorOption.name"
              >
                <input
                  type="radio"
                  formControlName="color"
                  [value]="colorOption.value"
                  class="sr-only"
                />
                <div
                  class="w-10 h-10 rounded-full border-2 transition-all"
                  [style.background-color]="colorOption.value"
                  [class.border-gray-900]="projectForm.get('color')?.value === colorOption.value"
                  [class.border-gray-300]="projectForm.get('color')?.value !== colorOption.value"
                  [class.ring-2]="projectForm.get('color')?.value === colorOption.value"
                  [class.ring-blue-500]="projectForm.get('color')?.value === colorOption.value"
                ></div>
              </label>
            }
          </div>
        </div>

        <div class="flex justify-end gap-3 pt-4 border-t border-gray-200 dark:border-gray-700">
          <button
            type="button"
            (click)="cancel()"
            class="px-4 py-2 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors focus:outline-none focus:ring-2 focus:ring-gray-500 focus:ring-offset-2"
          >
            Cancel
          </button>
          <button
            type="submit"
            [disabled]="projectForm.invalid || isLoading()"
            class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            [attr.aria-busy]="isLoading()"
          >
            @if (isLoading()) {
              <span class="flex items-center">
                <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Saving...
              </span>
            } @else {
              {{ isEditMode() ? 'Update Project' : 'Create Project' }}
            }
          </button>
        </div>
      </form>
    </div>
  `
})
export class ProjectFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly projectService = inject(ProjectService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEditMode = signal(false);

  private projectId: string | null = null;

  readonly colorOptions = [
    { name: 'Red', value: '#ef4444' },
    { name: 'Orange', value: '#f97316' },
    { name: 'Yellow', value: '#eab308' },
    { name: 'Green', value: '#22c55e' },
    { name: 'Blue', value: '#3b82f6' },
    { name: 'Indigo', value: '#6366f1' },
    { name: 'Purple', value: '#a855f7' },
    { name: 'Pink', value: '#ec4899' }
  ];

  readonly projectForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    description: ['', [Validators.required]],
    color: ['#3b82f6', [Validators.required]],
    teamIds: [[] as string[]]
  });

  ngOnInit(): void {
    this.projectId = this.route.snapshot.paramMap.get('id');
    if (this.projectId) {
      this.isEditMode.set(true);
      this.loadProject(this.projectId);
    }
  }

  private loadProject(id: string): void {
    const projects = this.projectService.projects();
    const project = projects.find(p => p.id === id);

    if (project) {
      this.populateForm(project);
      return;
    }

    this.isLoading.set(true);
    this.projectService.loadProjects().subscribe({
      next: loadedProjects => {
        this.isLoading.set(false);
        const loadedProject = loadedProjects.find(item => item.id === id);
        if (loadedProject) {
          this.populateForm(loadedProject);
        } else {
          this.errorMessage.set('This project could not be found.');
        }
      },
      error: err => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to load project. Please try again.');
      }
    });
  }

  private populateForm(project: { name: string; description: string; color: string; teamIds: string[] }): void {
    this.projectForm.patchValue({
      name: project.name,
      description: project.description,
      color: project.color,
      teamIds: project.teamIds
    });
  }

  onSubmit(): void {
    if (this.projectForm.invalid) {
      this.projectForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const projectData = this.projectForm.getRawValue();

    const request = this.isEditMode() && this.projectId
      ? this.projectService.updateProject(this.projectId, projectData)
      : this.projectService.createProject(projectData);

    request.subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/projects']);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to save project. Please try again.');
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/projects']);
  }
}