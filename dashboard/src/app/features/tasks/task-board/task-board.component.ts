import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CdkDragDrop, DragDropModule, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { TaskService } from '../../../core/services/task.service';
import { ProjectService } from '../../../core/services/project.service';
import { Task, TaskStatus } from '../../../core/models/task.model';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

interface Column { id: TaskStatus; title: string; color: string; }

@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [CommonModule, FormsModule, DragDropModule, RouterLink],
  template: `
    <div class="space-y-7">
      <div class="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
        <div><p class="eyebrow mb-3">Your workspace</p><h1 class="font-display text-4xl font-semibold tracking-[-0.07em] text-[#f4f1ea]">My tasks<span class="text-[#ff6b35]">.</span></h1><p class="mt-3 text-sm text-[#8f96a5]">Move work forward, one clear step at a time.</p></div>
        <div class="flex items-center gap-3">
          <select [(ngModel)]="selectedProjectFilter" (change)="onProjectFilterChange()" class="rounded-xl border border-white/[0.1] bg-[#171b24] px-3 py-2.5 text-xs font-semibold text-[#c8ccd4] outline-none focus:border-[#ff6b35]" aria-label="Filter by project"><option value="">All projects</option>@for (project of projects(); track project.id) { <option [value]="project.id">{{ project.name }}</option> }</select>
          <a routerLink="/tasks/new" class="orange-glow flex items-center gap-2 rounded-xl bg-[#ff6b35] px-4 py-2.5 text-sm font-bold text-[#1d120d] transition hover:bg-[#ff8254]"><span class="text-lg leading-none">+</span> Add task</a>
        </div>
      </div>

      <div class="flex items-center gap-2 overflow-x-auto border-b border-white/[0.07] pb-3">
        <button type="button" class="rounded-lg bg-[#ff6b35]/12 px-3 py-2 text-xs font-bold text-[#ff8b61]">Board view</button>
        <button type="button" class="rounded-lg px-3 py-2 text-xs font-semibold text-[#8f96a5] transition hover:bg-white/[0.05] hover:text-[#f4f1ea]">List view</button>
        <button type="button" class="rounded-lg px-3 py-2 text-xs font-semibold text-[#8f96a5] transition hover:bg-white/[0.05] hover:text-[#f4f1ea]">Calendar</button>
        <span class="ml-auto hidden text-xs text-[#687080] sm:block">Drag cards to update status</span>
      </div>

      @if (loading()) {
        <div class="surface-card flex min-h-[420px] items-center justify-center"><div class="h-8 w-8 animate-spin rounded-full border-2 border-[#ff6b35]/20 border-t-[#ff6b35]" role="status" aria-label="Loading tasks"></div></div>
      } @else {
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4" cdkDropListGroup>
          @for (column of columns; track column.id) {
            <section class="rounded-2xl border border-white/[0.07] bg-[#11141b] p-3" [attr.aria-label]="column.title + ' column'">
              <div class="mb-4 flex items-center justify-between px-2 pt-1"><div class="flex items-center gap-2"><span class="h-2 w-2 rounded-full" [style.background]="column.color"></span><h2 class="text-xs font-bold uppercase tracking-[0.12em] text-[#c8ccd4]">{{ column.title }}</h2><span class="text-xs text-[#687080]">{{ getTasksByStatus(column.id).length }}</span></div><button type="button" class="text-lg leading-none text-[#687080] hover:text-[#f4f1ea]" aria-label="Column options">···</button></div>
              <div cdkDropList [id]="column.id" [cdkDropListData]="getTasksByStatus(column.id)" (cdkDropListDropped)="onDrop($event)" class="min-h-[460px] space-y-3 rounded-xl bg-white/[0.018] p-2">
                @for (task of getTasksByStatus(column.id); track task.id) {
                  <article cdkDrag class="group cursor-move rounded-xl border border-white/[0.08] bg-[#1a1e27] p-4 transition hover:-translate-y-0.5 hover:border-white/20 hover:bg-[#202530]" [attr.aria-label]="'Task: ' + task.title" tabindex="0">
                    <div class="mb-4 flex items-start justify-between gap-3"><span class="rounded-md px-2 py-1 text-[9px] font-bold uppercase tracking-[0.12em]" [ngClass]="priorityClass(task.priority)">{{ task.priority }}</span><button type="button" class="text-[#687080] opacity-0 transition group-hover:opacity-100" [attr.aria-label]="'More options for ' + task.title">···</button></div>
                    <a [routerLink]="['/tasks', task.id, 'edit']" class="block text-sm font-semibold leading-5 text-[#f4f1ea] hover:text-[#ff9d7c]">{{ task.title }}</a>
                    <p class="mt-2 line-clamp-2 text-xs leading-5 text-[#8f96a5]">{{ task.description }}</p>
                    @if (task.tags.length > 0) { <div class="mt-4 flex flex-wrap gap-1.5">@for (tag of task.tags; track tag) { <span class="rounded-md border border-white/[0.08] px-2 py-1 text-[10px] font-semibold text-[#8f96a5]">{{ tag }}</span> }</div> }
                    <div class="mt-5 flex items-center justify-between border-t border-white/[0.07] pt-3"><span class="flex items-center gap-1.5 text-[10px] text-[#747c8c]"><svg class="h-3.5 w-3.5" viewBox="0 0 24 24" fill="none"><path d="M8 3v4M16 3v4M4 10h16M6 5h12a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2Z" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/></svg>{{ task.dueDate | date:'MMM d' }}</span><span class="flex items-center gap-2"><a [routerLink]="['/tasks', task.id, 'edit']" class="text-[10px] font-semibold text-[#8f96a5] hover:text-[#f4f1ea]">Edit</a><button type="button" (click)="deleteTask(task)" class="text-[10px] font-semibold text-[#8f96a5] hover:text-[#ff8b61]">Delete</button><span class="flex h-6 w-6 items-center justify-center rounded-full bg-[#9b8cff] text-[8px] font-bold text-[#201c38]">{{ avatar(task.assigneeId) }}</span></span></div>
                  </article>
                }
                @if (getTasksByStatus(column.id).length === 0) { <div class="flex min-h-[130px] items-center justify-center rounded-xl border border-dashed border-white/[0.09] text-xs text-[#687080]">Drop tasks here</div> }
              </div>
            </section>
          }
        </div>
      }
    </div>
  `
})
export class TaskBoardComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly projectService = inject(ProjectService);
  private readonly authService = inject(AuthService);
  readonly loading = this.taskService.loading;
  readonly projects = this.projectService.projects;
  readonly tasksByStatus = this.taskService.tasksByStatus;
  selectedProjectFilter = signal<string>('');
  readonly columns: Column[] = [
    { id: 'todo', title: 'To do', color: '#687080' }, { id: 'in-progress', title: 'In progress', color: '#ffb38e' },
    { id: 'review', title: 'Review', color: '#9b8cff' }, { id: 'done', title: 'Done', color: '#7ee2c0' }
  ];
  private readonly demoTasks = signal<Task[]>([
    { id: 'board-1', title: 'Finalize onboarding copy', description: 'Review the last pass with the content team', status: 'in-progress', priority: 'high', projectId: 'brand-refresh', assigneeId: 'AM', dueDate: new Date(2026, 8, 7), tags: ['Copy'], order: 1, createdAt: new Date(), updatedAt: new Date() },
    { id: 'board-2', title: 'Audit mobile flows', description: 'Check the handoff from welcome to setup', status: 'review', priority: 'urgent', projectId: 'mobile-flows', assigneeId: 'JD', dueDate: new Date(2026, 8, 8), tags: ['UX'], order: 2, createdAt: new Date(), updatedAt: new Date() },
    { id: 'board-3', title: 'Prepare launch checklist', description: 'Align owners and deadlines for launch week', status: 'todo', priority: 'medium', projectId: 'launch-week', assigneeId: 'AM', dueDate: new Date(2026, 8, 9), tags: ['Planning'], order: 3, createdAt: new Date(), updatedAt: new Date() },
    { id: 'board-4', title: 'Research references', description: 'Collect three visual references for the new system', status: 'todo', priority: 'low', projectId: 'brand-refresh', assigneeId: 'JD', dueDate: new Date(2026, 8, 10), tags: ['Research'], order: 4, createdAt: new Date(), updatedAt: new Date() },
    { id: 'board-5', title: 'Share design principles', description: 'Document the new product language for the team', status: 'done', priority: 'medium', projectId: 'brand-refresh', assigneeId: 'AM', dueDate: new Date(2026, 8, 6), tags: ['Design'], order: 5, createdAt: new Date(), updatedAt: new Date() }
  ]);

  ngOnInit(): void {
    this.projectService.loadProjects().subscribe({ error: () => undefined });
    this.loadTasks();
  }

  loadTasks(): void { this.taskService.loadTasks(this.selectedProjectFilter() || undefined).subscribe({ error: () => undefined }); }
  onProjectFilterChange(): void { this.loadTasks(); }
  getTasksByStatus(status: TaskStatus): Task[] {
    if (this.authService.isDemoSession()) return this.demoTasks().filter(task => task.status === status);
    return (this.tasksByStatus() as Record<TaskStatus, Task[]>)[status] ?? [];
  }
  priorityClass(priority: Task['priority']): string {
    return { urgent: 'bg-[#ff6b35]/15 text-[#ff8b61]', high: 'bg-[#ffb38e]/15 text-[#ffb38e]', medium: 'bg-[#9b8cff]/15 text-[#b4aaff]', low: 'bg-[#7ee2c0]/15 text-[#7ee2c0]' }[priority];
  }
  avatar(assigneeId?: string): string { return assigneeId?.slice(0, 2).toUpperCase() ?? 'AM'; }
  onDrop(event: CdkDragDrop<Task[]>): void {
    if (event.previousContainer === event.container) moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    else transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
    const task = event.container.data[event.currentIndex];

    if (this.authService.isDemoSession()) {
      this.demoTasks.update(tasks => tasks.map(item =>
        item.id === task.id
          ? { ...item, status: event.container.id as TaskStatus, order: event.currentIndex, updatedAt: new Date() }
          : item
      ));
      return;
    }

    this.taskService.moveTask(task.id, event.container.id as TaskStatus, event.currentIndex).subscribe({ error: () => undefined });
  }

  deleteTask(task: Task): void {
    if (!confirm(`Are you sure you want to delete "${task.title}"?`)) return;
    this.taskService.deleteTask(task.id).subscribe({ error: () => undefined });
  }
}