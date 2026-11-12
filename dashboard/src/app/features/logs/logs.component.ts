import { CommonModule } from '@angular/common';
import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LogsService } from '../../core/services/logs.service';
import { LogFileInfo, LogInfoResponse, ParsedLogEntry } from '../../core/models/logs.model';

@Component({
  selector: 'app-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
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
  selectedLimit: number = 250;
  autoRefreshRate: number = 0;

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
      this.selectedLimit
    ).subscribe({
      next: (res) => {
        this.entries.set(res.entries || []);
        this.totalEntries.set(res.totalEntries || 0);
        this.filteredEntries.set(res.filteredEntries || 0);
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

  onFileChange(): void {
    this.fetchEntries();
  }

  onLimitChange(): void {
    this.fetchEntries();
  }

  onAutoRefreshChange(): void {
    this.stopAutoRefresh();
    if (this.autoRefreshRate > 0) {
      this.refreshTimer = setInterval(() => {
        this.fetchEntries();
      }, this.autoRefreshRate);
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

