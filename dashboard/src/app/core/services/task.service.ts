// src/app/core/services/task.service.ts
import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, tap, throwError } from 'rxjs';
import { Task, CreateTaskDto, UpdateTaskDto, TaskStatus } from '../models/task.model';
import { AuthService } from './auth.service';
import { API_URL } from '../api.config';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  private readonly tasksSignal = signal<Task[]>([]);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<string | null>(null);

  readonly tasks = this.tasksSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  readonly tasksByStatus = computed(() => {
    const allTasks = this.tasksSignal();
    return {
      todo: allTasks.filter(t => t.status === 'todo').sort((a, b) => a.order - b.order),
      'in-progress': allTasks.filter(t => t.status === 'in-progress').sort((a, b) => a.order - b.order),
      review: allTasks.filter(t => t.status === 'review').sort((a, b) => a.order - b.order),
      done: allTasks.filter(t => t.status === 'done').sort((a, b) => a.order - b.order)
    };
  });

  readonly tasksByProject = computed(() => {
    const allTasks = this.tasksSignal();
    return allTasks.reduce((acc, task) => {
      if (!acc[task.projectId]) {
        acc[task.projectId] = [];
      }
      acc[task.projectId].push(task);
      return acc;
    }, {} as Record<string, Task[]>);
  });

  loadTasks(projectId?: string): Observable<Task[]> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    if (this.authService.isDemoSession()) {
      this.tasksSignal.set([]);
      this.loadingSignal.set(false);
      return of([]);
    }

    const url = projectId ? `${API_URL}/tasks?projectId=${projectId}` : `${API_URL}/tasks`;

    return this.http.get<Task[]>(url).pipe(
      tap({
        next: tasks => {
          this.tasksSignal.set(tasks);
          this.loadingSignal.set(false);
        },
        error: err => {
          this.errorSignal.set(err.message || 'Failed to load tasks');
          this.loadingSignal.set(false);
        }
      })
    );
  }

  createTask(dto: CreateTaskDto): Observable<Task> {
    if (this.authService.isDemoSession()) {
      return throwError(() => new Error('The demo workspace is read-only.'));
    }

    return this.http.post<Task>(`${API_URL}/tasks`, dto).pipe(
      tap(task => {
        this.tasksSignal.update(tasks => [...tasks, task]);
      })
    );
  }

  updateTask(id: string, updates: UpdateTaskDto): Observable<Task> {
    if (this.authService.isDemoSession()) {
      return throwError(() => new Error('The demo workspace is read-only.'));
    }

    return this.http.patch<Task>(`${API_URL}/tasks/${id}`, updates).pipe(
      tap(updated => {
        this.tasksSignal.update(tasks =>
          tasks.map(t => t.id === id ? updated : t)
        );
      })
    );
  }

  moveTask(taskId: string, newStatus: TaskStatus, newOrder: number): Observable<Task> {
    return this.updateTask(taskId, { status: newStatus, order: newOrder });
  }

  deleteTask(id: string): Observable<void> {
    if (this.authService.isDemoSession()) {
      return throwError(() => new Error('The demo workspace is read-only.'));
    }

    return this.http.delete<void>(`${API_URL}/tasks/${id}`).pipe(
      tap(() => {
        this.tasksSignal.update(tasks => tasks.filter(t => t.id !== id));
      })
    );
  }

  getTaskById(id: string): Task | undefined {
    return this.tasksSignal().find(t => t.id === id);
  }
}