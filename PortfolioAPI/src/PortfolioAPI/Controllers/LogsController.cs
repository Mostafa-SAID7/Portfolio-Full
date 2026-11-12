using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PortfolioAPI.Controllers
{
    /// <summary>
    /// Production log viewer and diagnostics API.
    /// Provides metadata, structured entries, raw downloads, and an interactive embedded UI
    /// for inspecting application logs directly on the server without filesystem access.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LogsController> _logger;

        private static readonly Regex LogLineRegex = new(
            @"^\[(?<timestamp>[^\]]+)\]\s+\[(?<level>[^\]]+)\]\s+(?:\[(?<source>[^\]]*)\])?\s*(?<message>.*?)(?:\s+(?<properties>\{.*\}))?$",
            RegexOptions.Compiled | RegexOptions.Singleline);

        public LogsController(
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<LogsController> logger)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Serves the standalone responsive HTML5 log viewer dashboard.
        /// Publicly reachable UI shell; actual data fetching delegates to authenticated API endpoints.
        /// </summary>
        [HttpGet("/logs")]
        [HttpGet("ui")]
        [AllowAnonymous]
        [Produces("text/html")]
        public IActionResult GetLogsUi()
        {
            var logDirectory = GetLogDirectory();
            var html = RenderLogViewerHtml(logDirectory);
            return Content(html, "text/html; charset=utf-8");
        }

        /// <summary>
        /// Returns metadata about log configuration, active directory path, and available rolling log files.
        /// </summary>
        [HttpGet("info")]
        [Authorize]
        public ActionResult<LogInfoResponse> GetLogInfo()
        {
            var logDir = GetLogDirectory();
            var dirExists = Directory.Exists(logDir);
            var files = new List<LogFileInfo>();

            if (dirExists)
            {
                var dirInfo = new DirectoryInfo(logDir);
                var foundFiles = dirInfo.GetFiles("portfolio-*.log")
                    .Concat(dirInfo.GetFiles("*.log"))
                    .DistinctBy(f => f.FullName)
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .ToList();

                var todayPrefix = $"portfolio-{DateTime.UtcNow:yyyyMMdd}";

                foreach (var file in foundFiles)
                {
                    files.Add(new LogFileInfo
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        SizeBytes = file.Length,
                        SizeFormatted = FormatBytes(file.Length),
                        LastModifiedUtc = file.LastWriteTimeUtc,
                        IsCurrent = file.Name.StartsWith(todayPrefix, StringComparison.OrdinalIgnoreCase)
                    });
                }
            }

            var currentFile = files.FirstOrDefault(f => f.IsCurrent) ?? files.FirstOrDefault();

            return Ok(new LogInfoResponse
            {
                Enabled = _configuration.GetValue("Logging:File:Enabled", true),
                LogDirectory = logDir,
                DirectoryExists = dirExists,
                CurrentLogFile = currentFile?.Name,
                CurrentLogFullPath = currentFile?.FullPath ?? (dirExists ? Path.Combine(logDir, $"portfolio-{DateTime.UtcNow:yyyyMMdd}.log") : null),
                TotalFiles = files.Count,
                TotalSizeBytes = files.Sum(f => f.SizeBytes),
                TotalSizeFormatted = FormatBytes(files.Sum(f => f.SizeBytes)),
                Files = files
            });
        }

        /// <summary>
        /// Reads and parses entries from a specified log file with level filtering and search.
        /// </summary>
        [HttpGet("entries")]
        [Authorize]
        public ActionResult<LogEntriesResponse> GetLogEntries(
            [FromQuery] string? fileName,
            [FromQuery] string? level,
            [FromQuery] string? search,
            [FromQuery] int limit = 200)
        {
            var logDir = GetLogDirectory();
            if (!Directory.Exists(logDir))
            {
                return Ok(new LogEntriesResponse { TotalEntries = 0, Entries = new() });
            }

            var targetPath = ResolveSafeLogFilePath(logDir, fileName);
            if (targetPath == null || !System.IO.File.Exists(targetPath))
            {
                return NotFound(new { message = $"Log file '{fileName}' was not found in directory '{logDir}'." });
            }

            limit = Math.Clamp(limit, 10, 2000);
            var parsedEntries = ReadLogEntries(targetPath, level, search, limit);

            return Ok(new LogEntriesResponse
            {
                FileName = Path.GetFileName(targetPath),
                FullPath = targetPath,
                TotalEntries = parsedEntries.TotalCount,
                FilteredEntries = parsedEntries.Entries.Count,
                Entries = parsedEntries.Entries
            });
        }

        /// <summary>
        /// Downloads the raw log file.
        /// </summary>
        [HttpGet("download")]
        [Authorize]
        public IActionResult DownloadLog([FromQuery] string? fileName)
        {
            var logDir = GetLogDirectory();
            var targetPath = ResolveSafeLogFilePath(logDir, fileName);

            if (targetPath == null || !System.IO.File.Exists(targetPath))
            {
                return NotFound(new { message = $"Log file was not found." });
            }

            return PhysicalFile(targetPath, "text/plain; charset=utf-8", Path.GetFileName(targetPath));
        }

        private string GetLogDirectory()
        {
            var configuredPath = _configuration["Logging:File:Path"];
            var baseDirectory = string.IsNullOrWhiteSpace(_environment.ContentRootPath)
                ? AppContext.BaseDirectory
                : _environment.ContentRootPath;

            return string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(baseDirectory, "App_Data", "logs")
                : (Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(baseDirectory, configuredPath));
        }

        private string? ResolveSafeLogFilePath(string logDir, string? requestedFileName)
        {
            if (string.IsNullOrWhiteSpace(requestedFileName))
            {
                var dirInfo = new DirectoryInfo(logDir);
                var latest = dirInfo.GetFiles("portfolio-*.log")
                    .Concat(dirInfo.GetFiles("*.log"))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();

                return latest?.FullName;
            }

            var safeName = Path.GetFileName(requestedFileName);
            var combined = Path.Combine(logDir, safeName);
            var fullCombined = Path.GetFullPath(combined);
            var fullLogDir = Path.GetFullPath(logDir);

            return fullCombined.StartsWith(fullLogDir, StringComparison.OrdinalIgnoreCase) ? fullCombined : null;
        }

        private (int TotalCount, List<ParsedLogEntry> Entries) ReadLogEntries(
            string filePath,
            string? levelFilter,
            string? searchFilter,
            int limit)
        {
            var entries = new List<ParsedLogEntry>();
            var allLines = new List<string>();

            // Read file with sharing permissions so active Serilog writer is never blocked
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    allLines.Add(line);
                }
            }

            // Group lines into log events (multiline exceptions belong to previous entry)
            var events = new List<List<string>>();
            List<string>? currentEvent = null;

            foreach (var line in allLines)
            {
                if (line.StartsWith("[") && line.Length > 20 && line.IndexOf("]") > 0)
                {
                    if (currentEvent != null) events.Add(currentEvent);
                    currentEvent = new List<string> { line };
                }
                else if (currentEvent != null)
                {
                    currentEvent.Add(line);
                }
            }
            if (currentEvent != null) events.Add(currentEvent);

            var totalCount = events.Count;
            var normalizedLevel = levelFilter?.Trim().ToUpperInvariant();
            var normalizedSearch = searchFilter?.Trim();

            // Reverse to view newest first
            for (int i = events.Count - 1; i >= 0 && entries.Count < limit; i--)
            {
                var evLines = events[i];
                var headerLine = evLines[0];
                var exceptionText = evLines.Count > 1 ? string.Join("\n", evLines.Skip(1)) : null;

                var match = LogLineRegex.Match(headerLine);
                string timestamp = string.Empty;
                string level = "INF";
                string? sourceContext = null;
                string message = headerLine;

                if (match.Success)
                {
                    timestamp = match.Groups["timestamp"].Value;
                    level = match.Groups["level"].Value.Trim();
                    sourceContext = match.Groups["source"].Success ? match.Groups["source"].Value : null;
                    message = match.Groups["message"].Value;
                }

                // Level Filter
                if (!string.IsNullOrEmpty(normalizedLevel) && normalizedLevel != "ALL")
                {
                    var matchesLevel = normalizedLevel switch
                    {
                        "INFO" or "INFORMATION" => level is "INF" or "INFO",
                        "WARN" or "WARNING" => level is "WRN" or "WARN",
                        "ERROR" or "ERR" => level is "ERR" or "ERROR",
                        "FATAL" or "CRITICAL" => level is "FTL" or "FATAL",
                        "DEBUG" or "DBG" => level is "DBG" or "DEBUG",
                        _ => level.Contains(normalizedLevel, StringComparison.OrdinalIgnoreCase)
                    };

                    if (!matchesLevel) continue;
                }

                // Search Filter
                if (!string.IsNullOrEmpty(normalizedSearch))
                {
                    var fullBlock = string.Join(" ", evLines);
                    if (!fullBlock.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                entries.Add(new ParsedLogEntry
                {
                    Timestamp = timestamp,
                    Level = NormalizeLevelName(level),
                    LevelCode = level,
                    SourceContext = sourceContext,
                    Message = message,
                    Exception = exceptionText,
                    Raw = headerLine
                });
            }

            return (totalCount, entries);
        }

        private static string NormalizeLevelName(string level) => level switch
        {
            "INF" or "INFO" => "Information",
            "WRN" or "WARN" => "Warning",
            "ERR" or "ERROR" => "Error",
            "FTL" or "FATAL" => "Fatal",
            "DBG" or "DEBUG" => "Debug",
            _ => level
        };

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }

        private static string RenderLogViewerHtml(string logDirectory)
        {
            return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>Production Logs | Portfolio</title>
              <style>
                :root {
                  --bg: #0b0f19;
                  --card: #111827;
                  --border: #1f2937;
                  --text: #f3f4f6;
                  --muted: #9ca3af;
                  --accent: #3b82f6;
                  --info: #0284c7;
                  --warn: #d97706;
                  --err: #ef4444;
                  --fatal: #ec4899;
                  --font: 'Segoe UI', system-ui, -apple-system, sans-serif;
                  --code: 'Cascadia Code', 'Fira Code', 'Consolas', monospace;
                }
                * { box-sizing: border-box; margin: 0; padding: 0; }
                body { background: var(--bg); color: var(--text); font-family: var(--font); font-size: 14px; line-height: 1.5; padding: 20px; }
                .container { max-width: 1400px; margin: 0 auto; }
                header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; flex-wrap: wrap; gap: 15px; }
                h1 { font-size: 22px; font-weight: 700; color: #fff; display: flex; align-items: center; gap: 10px; }
                .badge { padding: 3px 8px; border-radius: 4px; font-size: 11px; font-weight: 700; text-transform: uppercase; }
                .path-banner { background: var(--card); border: 1px solid var(--border); border-radius: 8px; padding: 12px 16px; margin-bottom: 20px; display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 10px; }
                .path-label { color: var(--muted); font-size: 12px; margin-right: 8px; }
                .path-val { font-family: var(--code); color: #38bdf8; word-break: break-all; }
                .controls { background: var(--card); border: 1px solid var(--border); border-radius: 8px; padding: 16px; margin-bottom: 20px; display: flex; flex-wrap: wrap; gap: 12px; align-items: center; }
                input, select, button { background: #1f2937; color: #fff; border: 1px solid #374151; padding: 8px 12px; border-radius: 6px; font-size: 13px; outline: none; }
                input:focus, select:focus { border-color: var(--accent); }
                input[type="text"] { flex: 1; min-width: 220px; }
                button { cursor: pointer; font-weight: 600; display: inline-flex; align-items: center; gap: 6px; transition: background 0.15s; }
                button:hover { background: #374151; }
                button.primary { background: var(--accent); border-color: var(--accent); }
                button.primary:hover { background: #2563eb; }
                .btn-group { display: flex; gap: 4px; }
                .btn-filter { padding: 6px 12px; font-size: 12px; }
                .btn-filter.active { background: #374151; border-color: #60a5fa; color: #60a5fa; }
                .auth-card { background: #1e1b4b; border: 1px solid #4338ca; border-radius: 8px; padding: 14px 18px; margin-bottom: 20px; display: none; align-items: center; gap: 12px; flex-wrap: wrap; }
                .log-table-wrap { background: var(--card); border: 1px solid var(--border); border-radius: 8px; overflow-x: auto; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.5); }
                table { width: 100%; border-collapse: collapse; font-family: var(--code); font-size: 12.5px; }
                th { background: #1f2937; text-align: left; padding: 10px 14px; color: var(--muted); font-size: 11px; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid var(--border); }
                td { padding: 8px 14px; border-bottom: 1px solid #1a2234; vertical-align: top; }
                tr:hover { background: #172033; }
                .lvl-inf { color: #38bdf8; }
                .lvl-wrn { color: #fbbf24; }
                .lvl-err { color: #f87171; font-weight: 700; }
                .lvl-ftl { color: #f472b6; font-weight: 700; }
                .msg-cell { word-break: break-word; line-height: 1.45; }
                .exception-box { margin-top: 6px; padding: 8px; background: #26161c; border-left: 3px solid var(--err); color: #fca5a5; font-size: 11.5px; white-space: pre-wrap; word-break: break-all; border-radius: 0 4px 4px 0; }
                .time-cell { white-space: nowrap; color: #94a3b8; }
                .src-cell { color: #a78bfa; max-width: 220px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
                .status-bar { margin-top: 15px; display: flex; justify-content: space-between; color: var(--muted); font-size: 12px; }
                .empty-state { padding: 40px; text-align: center; color: var(--muted); }
              </style>
            </head>
            <body>
              <div class="container">
                <header>
                  <h1>
                    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 17l6-6-6-6M12 19h8"/></svg>
                    Production Diagnostic Logs
                  </h1>
                  <div style="display: flex; gap: 10px; align-items: center;">
                    <span id="autoRefreshBadge" class="badge" style="background:#1e293b; color:#94a3b8;">Auto-refresh: OFF</span>
                    <button id="btnDownload" onclick="downloadCurrentLog()">Download Log</button>
                    <button class="primary" onclick="loadEntries()">Refresh</button>
                  </div>
                </header>

                <div id="authPrompt" class="auth-card">
                  <span>Enter Admin JWT Token for Production Authorization:</span>
                  <input type="password" id="authTokenInput" placeholder="Bearer JWT Token..." style="width: 320px;">
                  <button class="primary" onclick="saveAuthToken()">Authorize</button>
                </div>

                <div class="path-banner">
                  <div>
                    <span class="path-label">ACTIVE SERVER LOG PATH:</span>
                    <strong id="logPathDisplay" class="path-val">Loading path...</strong>
                  </div>
                  <div>
                    <button onclick="copyPath()" style="padding: 4px 10px; font-size: 11px;">Copy Path</button>
                  </div>
                </div>

                <div class="controls">
                  <select id="fileSelect" onchange="loadEntries()"></select>
                  <div class="btn-group">
                    <button class="btn-filter active" onclick="setLevel('ALL', this)">ALL</button>
                    <button class="btn-filter" onclick="setLevel('INFO', this)">INFO</button>
                    <button class="btn-filter" onclick="setLevel('WARN', this)">WARN</button>
                    <button class="btn-filter" onclick="setLevel('ERROR', this)">ERROR</button>
                  </div>
                  <input type="text" id="searchInput" placeholder="Filter by keyword, exception, or SourceContext..." oninput="debounceLoad()">
                  <select id="limitSelect" onchange="loadEntries()">
                    <option value="100">100 lines</option>
                    <option value="250" selected>250 lines</option>
                    <option value="500">500 lines</option>
                    <option value="1000">1000 lines</option>
                  </select>
                  <select id="refreshRateSelect" onchange="updateAutoRefresh()">
                    <option value="0">Auto-refresh: Off</option>
                    <option value="5000">Every 5s</option>
                    <option value="15000">Every 15s</option>
                    <option value="30000">Every 30s</option>
                  </select>
                </div>

                <div class="log-table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th style="width: 170px;">Timestamp</th>
                        <th style="width: 70px;">Level</th>
                        <th style="width: 220px;">Source</th>
                        <th>Message</th>
                      </tr>
                    </thead>
                    <tbody id="logsTbody">
                      <tr><td colspan="4" class="empty-state">Loading log events...</td></tr>
                    </tbody>
                  </table>
                </div>

                <div class="status-bar">
                  <span id="entriesCount">0 entries</span>
                  <span>Portfolio Clean Architecture Logging</span>
                </div>
              </div>

              <script>
                let currentLevel = 'ALL';
                let refreshTimer = null;
                let debounceTimer = null;

                function getAuthHeader() {
                  const token = localStorage.getItem('portfolio_admin_token') || sessionStorage.getItem('token');
                  return token ? { 'Authorization': 'Bearer ' + token } : {};
                }

                function saveAuthToken() {
                  const val = document.getElementById('authTokenInput').value.trim();
                  if (val) {
                    localStorage.setItem('portfolio_admin_token', val);
                    document.getElementById('authPrompt').style.display = 'none';
                    init();
                  }
                }

                async function init() {
                  try {
                    const res = await fetch('/api/v1/logs/info', { headers: getAuthHeader() });
                    if (res.status === 401 || res.status === 403) {
                      document.getElementById('authPrompt').style.display = 'flex';
                      document.getElementById('logPathDisplay').textContent = 'Authentication required';
                      return;
                    }
                    if (!res.ok) throw new Error('Failed to load log metadata');
                    const info = await res.json();
                    
                    document.getElementById('logPathDisplay').textContent = info.currentLogFullPath || info.logDirectory;
                    
                    const select = document.getElementById('fileSelect');
                    select.innerHTML = '';
                    if (info.files && info.files.length) {
                      info.files.forEach(f => {
                        const opt = document.createElement('option');
                        opt.value = f.name;
                        opt.textContent = `${f.name} (${f.sizeFormatted})`;
                        if (f.isCurrent) opt.selected = true;
                        select.appendChild(opt);
                      });
                    } else {
                      const opt = document.createElement('option');
                      opt.textContent = 'No log files available';
                      select.appendChild(opt);
                    }

                    loadEntries();
                  } catch (e) {
                    document.getElementById('logPathDisplay').textContent = 'Error connecting to server';
                  }
                }

                async function loadEntries() {
                  const file = document.getElementById('fileSelect').value;
                  const search = document.getElementById('searchInput').value;
                  const limit = document.getElementById('limitSelect').value;
                  const url = `/api/v1/logs/entries?fileName=${encodeURIComponent(file)}&level=${encodeURIComponent(currentLevel)}&search=${encodeURIComponent(search)}&limit=${limit}`;

                  try {
                    const res = await fetch(url, { headers: getAuthHeader() });
                    if (res.status === 401 || res.status === 403) {
                      document.getElementById('authPrompt').style.display = 'flex';
                      return;
                    }
                    if (!res.ok) throw new Error('Failed to load log entries');
                    const data = await res.json();
                    renderRows(data.entries);
                    document.getElementById('entriesCount').textContent = `Showing ${data.filteredEntries} of ${data.totalEntries} entries`;
                  } catch (e) {
                    document.getElementById('logsTbody').innerHTML = `<tr><td colspan="4" class="empty-state" style="color:var(--err)">Error loading entries: ${e.message}</td></tr>`;
                  }
                }

                function renderRows(entries) {
                  const tbody = document.getElementById('logsTbody');
                  if (!entries || !entries.length) {
                    tbody.innerHTML = '<tr><td colspan="4" class="empty-state">No matching log entries found</td></tr>';
                    return;
                  }

                  tbody.innerHTML = entries.map(e => {
                    const lvlClass = e.level === 'Error' ? 'lvl-err' : (e.level === 'Warning' ? 'lvl-wrn' : (e.level === 'Fatal' ? 'lvl-ftl' : 'lvl-inf'));
                    const exceptionHtml = e.exception ? `<div class="exception-box">${escapeHtml(e.exception)}</div>` : '';
                    return `
                      <tr>
                        <td class="time-cell">${escapeHtml(e.timestamp || '-')}</td>
                        <td class="${lvlClass}">${escapeHtml(e.levelCode || e.level)}</td>
                        <td class="src-cell" title="${escapeHtml(e.sourceContext || '')}">${escapeHtml(e.sourceContext || '')}</td>
                        <td class="msg-cell">
                          ${escapeHtml(e.message)}
                          ${exceptionHtml}
                        </td>
                      </tr>
                    `;
                  }).join('');
                }

                function setLevel(lvl, btn) {
                  currentLevel = lvl;
                  document.querySelectorAll('.btn-filter').forEach(b => b.classList.remove('active'));
                  btn.classList.add('active');
                  loadEntries();
                }

                function debounceLoad() {
                  clearTimeout(debounceTimer);
                  debounceTimer = setTimeout(loadEntries, 250);
                }

                function updateAutoRefresh() {
                  clearInterval(refreshTimer);
                  const ms = parseInt(document.getElementById('refreshRateSelect').value);
                  const badge = document.getElementById('autoRefreshBadge');
                  if (ms > 0) {
                    badge.textContent = `Auto-refresh: ${ms/1000}s`;
                    badge.style.background = '#15803d';
                    badge.style.color = '#fff';
                    refreshTimer = setInterval(loadEntries, ms);
                  } else {
                    badge.textContent = 'Auto-refresh: OFF';
                    badge.style.background = '#1e293b';
                    badge.style.color = '#94a3b8';
                  }
                }

                function copyPath() {
                  const path = document.getElementById('logPathDisplay').textContent;
                  navigator.clipboard.writeText(path).then(() => alert('Copied log path to clipboard: ' + path));
                }

                function downloadCurrentLog() {
                  const file = document.getElementById('fileSelect').value;
                  const token = localStorage.getItem('portfolio_admin_token') || sessionStorage.getItem('token') || '';
                  window.open(`/api/v1/logs/download?fileName=${encodeURIComponent(file)}&access_token=${encodeURIComponent(token)}`, '_blank');
                }

                function escapeHtml(text) {
                  return (text || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
                }

                document.addEventListener('DOMContentLoaded', init);
              </script>
            </body>
            </html>
            """;
        }
    }

    public class LogInfoResponse
    {
        public bool Enabled { get; set; }
        public string LogDirectory { get; set; } = string.Empty;
        public bool DirectoryExists { get; set; }
        public string? CurrentLogFile { get; set; }
        public string? CurrentLogFullPath { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSizeBytes { get; set; }
        public string TotalSizeFormatted { get; set; } = string.Empty;
        public List<LogFileInfo> Files { get; set; } = new();
    }

    public class LogFileInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string SizeFormatted { get; set; } = string.Empty;
        public DateTime LastModifiedUtc { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class LogEntriesResponse
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public int TotalEntries { get; set; }
        public int FilteredEntries { get; set; }
        public List<ParsedLogEntry> Entries { get; set; } = new();
    }

    public class ParsedLogEntry
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string LevelCode { get; set; } = string.Empty;
        public string? SourceContext { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public string Raw { get; set; } = string.Empty;
    }
}

