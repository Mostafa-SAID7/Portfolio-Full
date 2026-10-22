import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { AnalyticsService } from '../../core/services/analytics.service';
import { AnalyticsReport } from '../../core/models/analytics.model';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state.component';
import { PaginatorComponent } from '../../shared/paginator.component';

type RangePreset = 7 | 28 | 90;

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './analytics.component.html'
})
export class AnalyticsComponent {
  private readonly analytics = inject(AnalyticsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly report = this.analytics.report;
  readonly loading = this.analytics.loading;
  readonly error = this.analytics.error;
  readonly notConfigured = this.analytics.notConfigured;

  readonly preset = signal<RangePreset>(28);
  readonly startInput = signal<string>('');
  readonly endInput = signal<string>('');

  readonly overview = computed(() => this.report()?.overview ?? null);
  readonly trend = computed(() => this.report()?.trend ?? []);
  readonly countries = computed(() => this.report()?.countries ?? []);
  readonly sources = computed(() => this.report()?.sources ?? []);
  readonly pages = computed(() => this.report()?.pages ?? []);
  readonly devices = computed(() => this.report()?.devices ?? []);
  readonly events = computed(() => this.report()?.events ?? []);
  /** Page views series, used to scale the trend bars. */
  readonly pageViews = computed(() => this.trend().map(point => point.pageViews));
  readonly generatedAt = computed(() => {
    const r = this.report();
    if (!r) return '';
    const date = new Date(r.generatedAt);
    return Number.isNaN(date.getTime()) ? '' : date.toLocaleString();
  });

  constructor() {
    const range = AnalyticsService.defaultRange();
    this.startInput.set(range.start);
    this.endInput.set(range.end);
    this.analytics.loadReport(range.start, range.end).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ error: () => undefined });
  }

  applyPreset(days: RangePreset): void {
    this.preset.set(days);
    const end = new Date();
    const start = new Date(end);
    start.setDate(start.getDate() - (days - 1));
    const s = AnalyticsService.iso(start);
    const e = AnalyticsService.iso(end);
    this.startInput.set(s);
    this.endInput.set(e);
    this.reload(s, e);
  }

  applyCustom(): void {
    this.preset.set(28);
    this.reload(this.startInput(), this.endInput());
  }

  reload(start: string, end: string): void {
    this.analytics.loadReport(start, end).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ error: () => undefined });
  }

  /** Largest value in a series, used to scale the bars of a chart. */
  maxOf(values: number[]): number {
    return values.length ? Math.max(...values, 1) : 1;
  }

  /** Percentage change vs. the previous period, rounded to one decimal. */
  change(current: number, previous: number): string {
    if (previous === 0) return current === 0 ? '—' : '+∞';
    const delta = ((current - previous) / previous) * 100;
    return `${delta >= 0 ? '+' : ''}${delta.toFixed(1)}%`;
  }

  /** Format seconds as "m:ss". */
  formatDuration(seconds: number): string {
    if (!Number.isFinite(seconds) || seconds <= 0) return '0:00';
    const whole = Math.round(seconds);
    const m = Math.floor(whole / 60);
    const s = whole % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
  }

  /** The KPI cards shown at the top of the dashboard. */
  readonly kpis = computed(() => {
    const o = this.overview();
    if (!o) return [];
    const p = o.previous;
    return [
      { label: 'Active users', value: o.activeUsers.toLocaleString(), change: this.change(o.activeUsers, p.activeUsers) },
      { label: 'New users', value: o.newUsers.toLocaleString(), change: this.change(o.newUsers, p.newUsers) },
      { label: 'Sessions', value: o.sessions.toLocaleString(), change: this.change(o.sessions, p.sessions) },
      { label: 'Page views', value: o.pageViews.toLocaleString(), change: this.change(o.pageViews, p.pageViews) },
      { label: 'Engagement rate', value: `${o.engagementRate.toFixed(1)}%`, change: this.change(o.engagementRate, p.engagementRate) },
      { label: 'Avg. engagement time', value: this.formatDuration(o.averageEngagementSeconds), change: this.change(o.averageEngagementSeconds, p.averageEngagementSeconds) },
      { label: 'Contact conversions', value: o.contactConversions.toLocaleString(), change: this.change(o.contactConversions, p.contactConversions) }
    ];
  });
}
