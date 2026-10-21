export function renderErrorPage(): string {
  return `<!doctype html>
<html lang="en" class="dark">
  <head>
    <meta charset="utf-8" />
    <title>Application Error | Mostafa Samir Said</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <style>
      :root {
        --bg: #14100d;
        --card: rgba(26, 21, 18, 0.85);
        --border: rgba(255, 255, 255, 0.12);
        --text: #f4ede4;
        --muted: #a39589;
        --accent: #f59e0b;
        --accent-hover: #d97706;
      }
      * { box-sizing: border-box; margin: 0; padding: 0; }
      body {
        font: 15px/1.6 system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
        background: var(--bg);
        color: var(--text);
        display: grid;
        place-items: center;
        min-height: 100vh;
        padding: 1.5rem;
        position: relative;
        overflow: hidden;
      }
      .glow {
        position: absolute;
        width: 380px;
        height: 380px;
        background: radial-gradient(circle, rgba(245, 158, 11, 0.15) 0%, transparent 70%);
        pointer-events: none;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        border-radius: 50%;
      }
      .card {
        position: relative;
        max-width: 32rem;
        width: 100%;
        text-align: center;
        padding: 2.5rem 2rem;
        background: var(--card);
        border: 1px solid var(--border);
        border-radius: 1.5rem;
        box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.6);
        backdrop-filter: blur(20px);
        -webkit-backdrop-filter: blur(20px);
      }
      .icon-box {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 4rem;
        height: 4rem;
        border-radius: 1rem;
        background: rgba(245, 158, 11, 0.12);
        border: 1px solid rgba(245, 158, 11, 0.25);
        color: var(--accent);
        margin-bottom: 1.5rem;
      }
      .icon-box svg {
        width: 2rem;
        height: 2rem;
      }
      .badge {
        display: inline-block;
        font-size: 0.75rem;
        font-weight: 600;
        letter-spacing: 0.05em;
        text-transform: uppercase;
        color: var(--accent);
        background: rgba(245, 158, 11, 0.1);
        border: 1px solid rgba(245, 158, 11, 0.2);
        padding: 0.25rem 0.75rem;
        border-radius: 9999px;
        margin-bottom: 0.75rem;
      }
      h1 {
        font-size: 1.5rem;
        font-weight: 700;
        margin-bottom: 0.75rem;
        color: var(--text);
        letter-spacing: -0.02em;
      }
      p {
        color: var(--muted);
        font-size: 0.9375rem;
        margin-bottom: 2rem;
        line-height: 1.6;
      }
      .actions {
        display: flex;
        gap: 0.75rem;
        justify-content: center;
        flex-wrap: wrap;
      }
      a, button {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        padding: 0.625rem 1.25rem;
        border-radius: 0.75rem;
        font: inherit;
        font-size: 0.875rem;
        font-weight: 500;
        cursor: pointer;
        text-decoration: none;
        transition: all 0.2s ease;
      }
      .primary {
        background: var(--accent);
        color: #14100d;
        border: 1px solid var(--accent);
        font-weight: 600;
      }
      .primary:hover {
        background: var(--accent-hover);
        border-color: var(--accent-hover);
      }
      .secondary {
        background: rgba(255, 255, 255, 0.05);
        color: var(--text);
        border: 1px solid var(--border);
      }
      .secondary:hover {
        background: rgba(255, 255, 255, 0.1);
      }
    </style>
  </head>
  <body>
    <div class="glow" aria-hidden="true"></div>
    <div class="card">
      <div class="icon-box">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/>
          <line x1="12" y1="9" x2="12" y2="13"/>
          <line x1="12" y1="17" x2="12.01" y2="17"/>
        </svg>
      </div>
      <div>
        <span class="badge">Application Exception</span>
      </div>
      <h1>This page didn't load</h1>
      <p>An unexpected error occurred during server rendering. You can try refreshing the page or head back home.</p>
      <div class="actions">
        <button class="primary" onclick="location.reload()">Try again</button>
        <a class="secondary" href="/">Return home</a>
      </div>
    </div>
  </body>
</html>`;
}
