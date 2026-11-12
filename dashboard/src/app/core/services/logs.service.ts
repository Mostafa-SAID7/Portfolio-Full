import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { LogEntriesResponse, LogInfoResponse } from '../models/logs.model';

@Injectable({ providedIn: 'root' })
export class LogsService {
  private readonly http = inject(HttpClient);

  getLogInfo(): Observable<LogInfoResponse> {
    return this.http.get<LogInfoResponse>('/api/v1/logs/info');
  }

  getLogEntries(
    fileName?: string,
    level?: string,
    search?: string,
    limit: number = 250
  ): Observable<LogEntriesResponse> {
    let params = new HttpParams().set('limit', limit);
    if (fileName) params = params.set('fileName', fileName);
    if (level && level !== 'ALL') params = params.set('level', level);
    if (search) params = params.set('search', search);

    return this.http.get<LogEntriesResponse>('/api/v1/logs/entries', { params });
  }

  getDownloadUrl(fileName?: string): string {
    const fileParam = fileName ? `?fileName=${encodeURIComponent(fileName)}` : '';
    return `/api/v1/logs/download${fileParam}`;
  }
}

