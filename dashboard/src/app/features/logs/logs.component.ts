import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LogsService } from '../../core/services/logs.service';
import { LogFileInfo, LogInfoResponse, ParsedLogEntry } from '../../core/models/logs.model';
import { SelectFieldComponent, SelectOptionValue } from '../../shared/select-field.component';
import { PaginatorComponent } from '../../shared/paginator.component';

@Component({
  selector: 'app-logs',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, SelectFieldComponent, PaginatorComponent],
  templateUrl: './logs.component.html'
})
export class LogsComponent implements OnInit, OnDestroy {
  private readonly logsService = inject(LogsService);

  readonly loading = signal<boolean>(false);
  readonly error = signal<string | null>(null);
  readonly info = signal<LogInfoResponse | null>(null);
  readonly entries = signal<ParsedLogEntry[]>([]);
  readonly totalEntries = signal<number>(0);
  readonly filteredEntries = signal<number>(0);

  selectedFile: string = '';
  selectedLevel: string = 'ALL';
  searchQuery: string = '';
  selectedLimit: string = '250';
  autoRefreshRate: string = '0';

  /** Pagination state: 25 log entries per page for fast DOM rendering */
  readonly page = signal<number>(1);
  readonly pageSize = signal<number>(25);

  readonly pagedEntries = computed<ParsedLogEntry[]>(() => {
    const list = this.entries();
    const p = this.page();
    const size = this.pageSize();
    const start = (p - 1) * size;
    return list.slice(start, start + size);
  });

  readonly pageFrom = computed<number>(() => {
    return this.entries().length === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1;
  });

  readonly pageTo = computed<number>(() => {
    return Math.min(this.page() * this.pageSize(), this.entries().length);
  });

  readonly limitOptions: readonly SelectOptionValue[] = [
    { value: '100', label: '100 entries' },
    { value: '250', label: '250 entries' },
    { value: '500', label: '500 entries' },
    { value: '1000', label: '1000 entries' },
  ];

  readonly refreshOptions: readonly SelectOptionValue[] = [
    { value: '0', label: 'Auto-refresh: Off' },
    { value: '5000', label: 'Every 5s' },
    { value: '15000', label: 'Every 15s' },
    { value: '30000', label: 'Every 30s' },
  ];

  readonly fileOptions = computed<readonly SelectOptionValue[]>(() => {
    const files = this.info()?.files ?? [];
    return files.map(f => ({
      value: f.name,
      label: `${f.name} (${f.sizeFormatted})`
    }));
  });

  private refreshTimer: any = null;
  private searchDebounce: any = null;

  ngOnInit(): void {
    this.loadInfoAndEntries();
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
    if (this.searchDebounce) clearTimeout(this.searchDebounce);
  }

  loadInfoAndEntries(): void {
    this.loading.set(true);
    this.error.set(null);

    this.logsService.getLogInfo().subscribe({
      next: (res) => {
        this.info.set(res);
        if (res.files && res.files.length) {
          const current = res.files.find(f => f.isCurrent) || res.files[0];
          this.selectedFile = current.name;
        }
        this.fetchEntries();
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.message || 'Failed to load log metadata.');
      }
    });
  }

  fetchEntries(): void {
    this.loading.set(true);
    this.logsService.getLogEntries(
      this.selectedFile,
      this.selectedLevel,
      this.searchQuery,
      parseInt(this.selectedLimit, 10) || 250
    ).subscribe({
      next: (res) => {
        this.entries.set(res.entries || []);
        this.totalEntries.set(res.totalEntries || 0);
        this.filteredEntries.set(res.filteredEntries || 0);
        this.page.set(1);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.message || 'Failed to load log entries.');
      }
    });
  }

  setLevel(level: string): void {
    this.selectedLevel = level;
    this.fetchEntries();
  }

  onSearchChange(): void {
    if (this.searchDebounce) clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => {
      this.fetchEntries();
    }, 250);
  }

  onFileChange(fileName: string): void {
    this.selectedFile = fileName;
    this.fetchEntries();
  }

  onLimitChange(limit: string): void {
    this.selectedLimit = limit;
    this.fetchEntries();
  }

  onAutoRefreshChange(rate?: string): void {
    if (rate !== undefined) {
      this.autoRefreshRate = rate;
    }
    this.stopAutoRefresh();
    const intervalMs = parseInt(this.autoRefreshRate, 10) || 0;
    if (intervalMs > 0) {
      this.refreshTimer = setInterval(() => {
        this.fetchEntries();
      }, intervalMs);
    }
  }

  private stopAutoRefresh(): void {
    if (this.refreshTimer) {
      clearInterval(this.refreshTimer);
      this.refreshTimer = null;
    }
  }

  copyLogPath(): void {
    const path = this.info()?.currentLogFullPath || this.info()?.logDirectory || '';
    if (path) {
      navigator.clipboard.writeText(path).then(() => {
        alert('Log path copied to clipboard:\n' + path);
      });
    }
  }

  downloadLog(): void {
    const url = this.logsService.getDownloadUrl(this.selectedFile);
    window.open(url, '_blank');
  }

  getLevelClass(level: string): string {
    switch (level?.toLowerCase()) {
      case 'error': return 'badge-err';
      case 'warning': return 'badge-wrn';
      case 'fatal': return 'badge-fatal';
      default: return 'badge-inf';
    }
  }
}
