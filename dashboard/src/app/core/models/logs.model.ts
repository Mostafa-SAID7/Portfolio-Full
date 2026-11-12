export interface LogFileInfo {
  name: string;
  fullPath: string;
  sizeBytes: number;
  sizeFormatted: string;
  lastModifiedUtc: string;
  isCurrent: boolean;
}

export interface LogInfoResponse {
  enabled: boolean;
  logDirectory: string;
  directoryExists: boolean;
  currentLogFile: string | null;
  currentLogFullPath: string | null;
  totalFiles: number;
  totalSizeBytes: number;
  totalSizeFormatted: string;
  files: LogFileInfo[];
}

export interface ParsedLogEntry {
  timestamp: string;
  level: string;
  levelCode: string;
  sourceContext?: string;
  message: string;
  exception?: string;
  raw: string;
}

export interface LogEntriesResponse {
  fileName: string;
  fullPath: string;
  totalEntries: number;
  filteredEntries: number;
  entries: ParsedLogEntry[];
}

