import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProjectService } from '../../core/services/project.service';
import { TaskService } from '../../core/services/task.service';
import { AuthService } from '../../core/services/auth.service';
import { Project } from '../../core/models/project.model';
import { Task } from '../../core/models/task.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="space-y-7">
      <section class="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
        <div>
          <p class="eyebrow mb-3">Monday, September 7, 2026</p>
          <h1 class="font-display text-4xl font-semibold tracking-[-0.06em] text-[#f4f1ea] sm:text-5xl">
            Good morning<span class="text-[#ff6b35]">,</span>
            <span class="block text-[#8f96a5]">{{ firstName() }}.</span>
          </h1>
        </div>
        <div class="flex items-center gap-3">
          <a routerLink="/projects/new" class="hidden rounded-xl border border-white/[0.1] px-4 py-2.5 text-sm font-semibold text-[#c8ccd4] transition hover:border-white/20 hover:bg-white/[0.04] sm:block">New project</a>
          <a routerLink="/tasks/new" class="orange-glow flex items-center gap-2 rounded-xl bg-[#ff6b35] px-4 py-2.5 text-sm font-bold text-[#1d120d] transition hover:-translate-y-0.5 hover:bg-[#ff8254]">
            <span class="text-lg leading-none">+</span> Add task
          </a>
        </div>
      </section>

      <section class="grid gap-5 xl:grid-cols-[1.5fr_1fr]">
        <div class="surface-card relative min-h-[248px] overflow-hidden p-6 sm:p-8">
          <div class="absolute -right-20 -top-24 h-72 w-72 rounded-full bg-[#ff6b35]/10 blur-3xl"></div>
          <div class="relative z-10 max-w-md">
            <div class="mb-6 flex items-center gap-2">
              <span class="flex h-7 w-7 items-center justify-center rounded-lg bg-[#ff6b35]/15 text-[#ff8254]">
                <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none"><path d="M12 4v16M4 12h16" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>
              </span>
              <span class="eyebrow text-[#ff9d7c]">Your focus today</span>
            </div>
            <h2 class="font-display text-3xl font-semibold leading-[1.05] tracking-[-0.05em] text-[#f4f1ea] sm:text-4xl">Make space for<br><span class="text-[#ff6b35]">deep work.</span></h2>
            <p class="mt-5 max-w-xs text-sm leading-6 text-[#8f96a5]">You have {{ focusTaskCount() }} focused tasks left today. A little progress is still progress.</p>
          </div>
          <div class="absolute bottom-7 right-7 flex h-28 w-28 items-center justify-center rounded-full border-[10px] border-[#ff6b35]/15 sm:h-32 sm:w-32">
            <div class="absolute inset-[-10px] rounded-full border-[10px] border-transparent border-t-[#ff6b35] border-r-[#ff6b35] border-b-[#ff6b35] rotate-[-34deg]"></div>
            <div class="text-center"><strong class="font-display text-3xl font-semibold text-[#f4f1ea]">72</strong><span class="block text-[10px] font-bold uppercase tracking-widest text-[#8f96a5]">focus</span></div>
          </div>
        </div>

        <div class="surface-card p-6">
          <div class="mb-6 flex items-start justify-between">
            <div><p class="eyebrow mb-2">This week</p><h2 class="font-display text-xl font-semibold tracking-[-0.04em] text-[#f4f1ea]">Your productivity</h2></div>
            <button type="button" class="rounded-lg border border-white/[0.08] px-2.5 py-1 text-[11px] font-semibold text-[#8f96a5]">7 days⌄</button>
          </div>
          <div class="flex h-[120px] items-end gap-2">
            @for (day of week; track day.label) {
              <div class="flex h-full flex-1 flex-col items-center justify-end gap-2">
                <div class="flex h-[92px] w-full items-end justify-center rounded-t-lg bg-white/[0.025]">
                  <div class="w-2/3 rounded-t-md transition hover:bg-[#ff8254]" [style.background]="day.active ? '#ff6b35' : '#343b48'" [style.height.%]="day.value"></div>
                </div>
                <span class="text-[10px] font-semibold" [style.color]="day.active ? '#f4f1ea' : '#687080'">{{ day.label }}</span>
              </div>
            }
          </div>
        </div>
      </section>

      <section class="grid grid-cols-2 gap-3 lg:grid-cols-4">
        @for (stat of stats(); track stat.label) {
          <div class="surface-card-raised p-4 sm:p-5">
            <div class="mb-5 flex items-center justify-between"><span class="flex h-8 w-8 items-center justify-center rounded-lg" [style.background]="stat.background" [style.color]="stat.color" [innerHTML]="stat.icon"></span><span class="text-[11px] font-semibold" [style.color]="stat.color">{{ stat.change }}</span></div>
            <p class="font-display text-2xl font-semibold tracking-[-0.05em] text-[#f4f1ea]">{{ stat.value }}</p><p class="mt-1 text-xs text-[#8f96a5]">{{ stat.label }}</p>
          </div>
        }
      </section>

      <section class="grid gap-5 xl:grid-cols-[1.35fr_0.65fr]">
        <div class="surface-card overflow-hidden">
          <div class="flex items-center justify-between border-b border-white/[0.07] px-6 py-5">
            <div><p class="eyebrow mb-2">Priority queue</p><h2 class="font-display text-xl font-semibold tracking-[-0.04em] text-[#f4f1ea]">Today’s tasks</h2></div>
            <a routerLink="/tasks" class="text-xs font-semibold text-[#ff8254] transition hover:text-[#ffad91]">View all <span aria-hidden="true">↗</span></a>
          </div>
          <div class="divide-y divide-white/[0.06]">
            @for (task of todayTasks(); track task.id) {
              <div class="group flex items-center gap-4 px-6 py-4 transition hover:bg-white/[0.025]">
                <button type="button" class="flex h-5 w-5 shrink-0 items-center justify-center rounded-full border border-[#687080] text-transparent transition hover:border-[#ff6b35] hover:text-[#ff6b35]" [attr.aria-label]="'Complete ' + task.title">✓</button>
                <div class="min-w-0 flex-1">
                  <div class="flex flex-wrap items-center gap-2"><h3 class="truncate text-sm font-semibold text-[#e5e3dd]">{{ task.title }}</h3><span class="rounded-md px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide" [ngClass]="priorityClass(task.priority)">{{ task.priority }}</span></div>
                  <p class="mt-1 truncate text-xs text-[#747c8c]">{{ task.description }}</p>
                </div>
                <span class="hidden shrink-0 text-xs text-[#747c8c] sm:block">{{ task.dueDate | date:'h:mm a' }}</span>
                <div class="hidden h-7 w-7 items-center justify-center rounded-full bg-[#9b8cff] text-[10px] font-bold text-[#201c38] sm:flex">{{ avatar(task.assigneeId) }}</div>
                <button type="button" class="text-[#687080] opacity-0 transition group-hover:opacity-100" [attr.aria-label]="'More options for ' + task.title">•••</button>
              </div>
            }
          </div>
        </div>

        <div id="activity" class="surface-card p-6">
          <div class="mb-6 flex items-center justify-between"><div><p class="eyebrow mb-2">Stay in the loop</p><h2 class="font-display text-xl font-semibold tracking-[-0.04em] text-[#f4f1ea]">Recent activity</h2></div><span class="h-2 w-2 rounded-full bg-[#7ee2c0] shadow-[0_0_12px_#7ee2c0]"></span></div>
           <div class="space-y-5">
             @for (event of visibleActivity(); track event.name) {
              <div class="flex gap-3"><div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-[10px] font-bold" [style.background]="event.background" [style.color]="event.color">{{ event.initials }}</div><div class="min-w-0"><p class="text-xs leading-5 text-[#c8ccd4]"><strong class="font-semibold text-[#f4f1ea]">{{ event.name }}</strong> {{ event.action }}</p><p class="mt-1 text-[10px] text-[#687080]">{{ event.time }}</p></div></div>
            }
             @if (visibleActivity().length === 0) {
               <p class="text-sm leading-6 text-[#8f96a5]">Activity from your workspace will appear here.</p>
             }
          </div>
        </div>
      </section>

      <section>
        <div class="mb-4 flex items-end justify-between"><div><p class="eyebrow mb-2">Workspaces</p><h2 class="font-display text-xl font-semibold tracking-[-0.04em] text-[#f4f1ea]">Active projects</h2></div><a routerLink="/projects" class="text-xs font-semibold text-[#ff8254]">See all projects ↗</a></div>
        <div class="grid gap-4 md:grid-cols-3">
          @for (project of projectCards(); track project.id) {
            <a [routerLink]="['/projects', project.id]" class="surface-card-raised group p-5 transition duration-200 hover:-translate-y-1 hover:border-white/20">
              <div class="mb-9 flex items-start justify-between"><span class="h-2.5 w-2.5 rounded-full" [style.background]="project.color"></span><span class="text-[#687080] transition group-hover:translate-x-1 group-hover:text-[#f4f1ea]">↗</span></div>
              <h3 class="font-display text-lg font-semibold tracking-[-0.04em] text-[#f4f1ea]">{{ project.name }}</h3><p class="mt-2 line-clamp-1 text-xs text-[#8f96a5]">{{ project.description }}</p>
              <div class="mt-6 flex items-center justify-between"><div class="flex -space-x-2"><span class="flex h-6 w-6 items-center justify-center rounded-full border-2 border-[#171b24] bg-[#9b8cff] text-[8px] font-bold text-[#201c38]">AM</span><span class="flex h-6 w-6 items-center justify-center rounded-full border-2 border-[#171b24] bg-[#7ee2c0] text-[8px] font-bold text-[#1c3932]">JD</span><span class="flex h-6 w-6 items-center justify-center rounded-full border-2 border-[#171b24] bg-[#ffb38e] text-[8px] font-bold text-[#402317]">+3</span></div><span class="text-xs font-semibold text-[#8f96a5]">{{ projectProgress(project.id) }}% complete</span></div>
              <div class="mt-3 h-1.5 overflow-hidden rounded-full bg-white/[0.07]"><div class="h-full rounded-full transition-all group-hover:brightness-125" [style.width.%]="projectProgress(project.id)" [style.background]="project.color"></div></div>
            </a>
          }
        </div>
      </section>
    </div>
  `
})
export class DashboardComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly taskService = inject(TaskService);
  private readonly authService = inject(AuthService);

  readonly currentUser = this.authService.currentUser;
  readonly isDemoSession = this.authService.isDemoSession;
  readonly projects = this.projectService.projects;
  readonly tasks = this.taskService.tasks;
  readonly firstName = computed(() => this.currentUser()?.name?.split(' ')[0] ?? 'there');
  readonly projectCards = computed(() => this.isDemoSession() ? this.demoProjects : this.projects().slice(0, 3));
  readonly todayTasks = computed(() => {
    const tasks = this.isDemoSession() ? this.demoTasks : this.tasks();
    return tasks.filter(task => task.status !== 'done').slice(0, 4);
  });
  readonly focusTaskCount = computed(() => this.todayTasks().length);
  readonly stats = computed(() => {
    const tasks = this.isDemoSession() ? this.demoTasks : this.tasks();
    const dueThisWeek = tasks.filter(task => {
      if (!task.dueDate) return false;
      const dueDate = new Date(task.dueDate);
      const now = new Date();
      const daysAway = (dueDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24);
      return daysAway >= 0 && daysAway <= 7;
    }).length;

    return [
      { label: 'Tasks completed', value: tasks.filter(task => task.status === 'done').length, change: this.isDemoSession() ? '+18%' : '', color: '#7ee2c0', background: 'rgba(126,226,192,.12)', icon: '✓' },
      { label: 'In progress', value: tasks.filter(task => task.status === 'in-progress').length, change: this.isDemoSession() ? '+4%' : '', color: '#ffb38e', background: 'rgba(255,179,142,.12)', icon: '◔' },
      { label: 'Due this week', value: dueThisWeek, change: this.isDemoSession() ? '-12%' : '', color: '#9b8cff', background: 'rgba(155,140,255,.12)', icon: '⌁' },
      { label: 'Focus hours', value: this.isDemoSession() ? '24h' : '—', change: this.isDemoSession() ? '+22%' : '', color: '#ff8254', background: 'rgba(255,107,53,.12)', icon: '↗' }
    ];
  });

  readonly week = [
    { label: 'M', value: 44, active: false }, { label: 'T', value: 69, active: false }, { label: 'W', value: 52, active: false },
    { label: 'T', value: 82, active: true }, { label: 'F', value: 61, active: false }, { label: 'S', value: 36, active: false }, { label: 'S', value: 28, active: false }
  ];
  readonly activity = [
    { name: 'Jordan Davis', action: 'completed “Audit mobile flows”', time: '12 minutes ago', initials: 'JD', background: 'rgba(126,226,192,.16)', color: '#7ee2c0' },
    { name: 'You', action: 'moved “Research references” to review', time: '48 minutes ago', initials: 'AM', background: 'rgba(155,140,255,.18)', color: '#b4aaff' },
    { name: 'Alex Morgan', action: 'commented on Brand refresh', time: '2 hours ago', initials: 'AM', background: 'rgba(255,179,142,.16)', color: '#ffb38e' }
  ];
  readonly visibleActivity = computed(() => this.isDemoSession() ? this.activity : []);
  private readonly demoProjects: Project[] = [
    { id: 'brand-refresh', name: 'Brand refresh', description: 'A sharper identity for the next chapter.', color: '#ff6b35', createdAt: new Date(), updatedAt: new Date(), ownerId: 'demo', teamIds: [] },
    { id: 'mobile-flows', name: 'Mobile flows', description: 'Make every tap feel effortless.', color: '#9b8cff', createdAt: new Date(), updatedAt: new Date(), ownerId: 'demo', teamIds: [] },
    { id: 'launch-week', name: 'Launch week', description: 'Everything we need to ship with confidence.', color: '#7ee2c0', createdAt: new Date(), updatedAt: new Date(), ownerId: 'demo', teamIds: [] }
  ];
  private readonly demoTasks: Task[] = [
    { id: 'task-1', title: 'Finalize onboarding copy', description: 'Review the last pass with the content team', status: 'in-progress', priority: 'high', projectId: 'brand-refresh', assigneeId: 'AM', dueDate: new Date(2026, 8, 7, 10, 30), tags: ['Copy'], order: 1, createdAt: new Date(), updatedAt: new Date() },
    { id: 'task-2', title: 'Audit mobile flows', description: 'Check the handoff from welcome to setup', status: 'review', priority: 'urgent', projectId: 'mobile-flows', assigneeId: 'JD', dueDate: new Date(2026, 8, 7, 12, 0), tags: ['UX'], order: 2, createdAt: new Date(), updatedAt: new Date() },
    { id: 'task-3', title: 'Prepare launch checklist', description: 'Align owners and deadlines for launch week', status: 'todo', priority: 'medium', projectId: 'launch-week', assigneeId: 'AM', dueDate: new Date(2026, 8, 7, 15, 0), tags: ['Planning'], order: 3, createdAt: new Date(), updatedAt: new Date() },
    { id: 'task-4', title: 'Research references', description: 'Collect three visual references for the new system', status: 'todo', priority: 'low', projectId: 'brand-refresh', assigneeId: 'JD', dueDate: new Date(2026, 8, 7, 17, 30), tags: ['Research'], order: 4, createdAt: new Date(), updatedAt: new Date() }
  ];

  ngOnInit(): void {
    this.projectService.loadProjects().subscribe({ error: () => undefined });
    this.taskService.loadTasks().subscribe({ error: () => undefined });
  }

  priorityClass(priority: Task['priority']): string {
    return { urgent: 'bg-[#ff6b35]/15 text-[#ff8b61]', high: 'bg-[#ffb38e]/15 text-[#ffb38e]', medium: 'bg-[#9b8cff]/15 text-[#b4aaff]', low: 'bg-[#7ee2c0]/15 text-[#7ee2c0]' }[priority];
  }

  avatar(assigneeId?: string): string {
    return assigneeId?.slice(0, 2).toUpperCase() ?? 'AM';
  }

  projectProgress(projectId: string): number {
    if (this.isDemoSession()) {
      return { 'brand-refresh': 68, 'mobile-flows': 42, 'launch-week': 84 }[projectId] ?? 0;
    }

    const projectTasks = this.tasks().filter(task => task.projectId === projectId);
    return projectTasks.length
      ? Math.round((projectTasks.filter(task => task.status === 'done').length / projectTasks.length) * 100)
      : 0;
  }
}