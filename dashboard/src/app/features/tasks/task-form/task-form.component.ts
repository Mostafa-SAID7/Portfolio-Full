// src/app/features/tasks/task-form/task-form.component.ts
import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { TaskService } from '../../../core/services/task.service';
import { ProjectService } from '../../../core/services/project.service';
import { TaskStatus, TaskPriority } from '../../../core/models/task.model';

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="max-w-2xl mx-auto">
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-gray-900 dark:text-white">
          {{ isEditMode() ? 'Edit Task' : 'Create New Task' }}
        </h1>
      </div>

      <form [formGroup]="taskForm" (ngSubmit)="onSubmit()" class="bg-white dark:bg-gray-800 shadow rounded-lg p-6 space-y-6">
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
          <label for="title" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Task Title <span class="text-red-500">*</span>
          </label>
          <input
            id="title"
            type="text"
            formControlName="title"
            required
            class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-white bg-white dark:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            [class.border-red-500]="taskForm.get('title')?.invalid && taskForm.get('title')?.touched"
            placeholder="Enter task title"
            [attr.aria-invalid]="taskForm.get('title')?.invalid && taskForm.get('title')?.touched"
            [attr.aria-describedby]="taskForm.get('title')?.invalid && taskForm.get('title')?.touched ? 'title-error' : null"
          />
          @if (taskForm.get('title')?.invalid && taskForm.get('title')?.touched) {
            <p id="title-error" class="mt-1 text-sm text-red-600 dark:text-red-400" role="alert">
              Title is required
            </p>
          }
        </div>

        <div>
          <label for="description" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Description
          </label>
          <textarea
            id="description"
            formControlName="description"
            rows="4"
            class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-white bg-white dark:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="Enter task description"
          ></textarea>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label for="projectId" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Project <span class="text-red-500">*</span>
            </label>
            <select
              id="projectId"
              formControlName="projectId"
              required
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-white bg-white dark:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              [class.border-red-500]="taskForm.get('projectId')?.invalid && taskForm.get('projectId')?.touched"
              [attr.aria-invalid]="taskForm.get('projectId')?.invalid && taskForm.get('projectId')?.touched"
              [attr.aria-describedby]="taskForm.get('projectId')?.invalid && taskForm.get('projectId')?.touched ? 'project-error' : null"
            >
              <option value="">Select a project</option>
              @for (project of projects(); track project.id) {
                <option [value]="project.id">{{ project.name }}</option>
              }
            </select>
            @if (taskForm.get('projectId')?.invalid && taskForm.get('projectId')?.touched) {
              <p id="project-error" class="mt-1 text-sm text-red-600 dark:text-red-400" role="alert">
                Project is required
              </p>
            }
          </div>

          <div>
            <label for="status" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Status <span class="text-red-500">*</span>
            </label>
            <select
              id="status"
              formControlName="status"
              required
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-white bg-white dark:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              [attr.aria-label]="'Task status'"
            >
              <option value="todo">To Do</option>
              <option value="in-progress">In Progress</option>
              <option value="review">Review</option>
              <option value="done">Done</option>
            </select>
          </div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label for="priority" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Priority <span class="text-red-500">*</span>
            </label>
            <select
              id="priority"
              formControlName="priority"
              required
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-white bg-white dark:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              [attr.aria-label]="'Task priority'"
            >
              <option value="low">Low</option>
              <option value="medium">Medium</option>
              <option value="high">High</option>
              <option value="urgent">Urgent</option>
            </select>
          </div>

          <div>
            <label for="dueDate" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Due Date
            </label>
            <input
              id="dueDate"
              type="date"
              formControlName="dueDate"
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-white bg-white dark:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>
        </div>

        <div>
          <label for="tags" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Tags (comma-separated)
          </label>
          <input
            id="tags"
            type="text"
            formControlName="tags"
            class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-white bg-white dark:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="e.g., frontend, bug, urgent"
          />
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
            [disabled]="taskForm.invalid || isLoading()"
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
              {{ isEditMode() ? 'Update Task' : 'Create Task' }}
            }
          </button>
        </div>
      </form>
    </div>
  `
})
export class TaskFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly taskService = inject(TaskService);
  private readonly projectService = inject(ProjectService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEditMode = signal(false);
  readonly projects = this.projectService.projects;

  private taskId: string | null = null;

  readonly taskForm = this.fb.nonNullable.group({
    title: ['', [Validators.required]],
    description: [''],
    projectId: ['', [Validators.required]],
    status: ['todo' as TaskStatus, [Validators.required]],
    priority: ['medium' as TaskPriority, [Validators.required]],
    dueDate: [''],
    tags: ['']
  });

  ngOnInit(): void {
    this.projectService.loadProjects().subscribe();
    
    this.taskId = this.route.snapshot.paramMap.get('id');
    if (this.taskId) {
      this.isEditMode.set(true);
      this.loadTask(this.taskId);
    }
  }

  private loadTask(id: string): void {
    const task = this.taskService.getTaskById(id);
    if (task) {
      this.populateForm(task);
      return;
    }

    this.isLoading.set(true);
    this.taskService.loadTasks().subscribe({
      next: loadedTasks => {
        this.isLoading.set(false);
        const loadedTask = loadedTasks.find(item => item.id === id);
        if (loadedTask) {
          this.populateForm(loadedTask);
        } else {
          this.errorMessage.set('This task could not be found.');
        }
      },
      error: err => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to load task. Please try again.');
      }
    });
  }

  private populateForm(task: {
    title: string;
    description: string;
    projectId: string;
    status: TaskStatus;
    priority: TaskPriority;
    dueDate?: Date | string;
    tags: string[];
  }): void {
    this.taskForm.patchValue({
      title: task.title,
      description: task.description,
      projectId: task.projectId,
      status: task.status,
      priority: task.priority,
      dueDate: task.dueDate ? new Date(task.dueDate).toISOString().split('T')[0] : '',
      tags: task.tags.join(', ')
    });
  }

  onSubmit(): void {
    if (this.taskForm.invalid) {
      this.taskForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const formValue = this.taskForm.getRawValue();
    const taskData = {
      ...formValue,
      dueDate: formValue.dueDate ? new Date(formValue.dueDate) : undefined,
      tags: formValue.tags ? formValue.tags.split(',').map(t => t.trim()).filter(t => t) : []
    };

    const request = this.isEditMode() && this.taskId
      ? this.taskService.updateTask(this.taskId, taskData)
      : this.taskService.createTask(taskData);

    request.subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/tasks']);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to save task. Please try again.');
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/tasks']);
  }
}