/**
 * Application error reporting and telemetry handler.
 * Provides structured error capture for React error boundaries,
 * unexpected promise rejections, and runtime exceptions.
 */

export interface AppErrorPayload {
  message: string;
  stack?: string;
  source?: string;
  severity?: "error" | "warning" | "info";
  context?: Record<string, unknown>;
  timestamp: string;
}

export function reportAppError(error: unknown, context: Record<string, unknown> = {}): void {
  if (typeof window === "undefined") return;

  const message =
    error instanceof Response
      ? `Response ${error.status}${error.url ? ` at ${error.url}` : ""}`
      : error instanceof Error
        ? error.message
        : String(error);

  const stack = error instanceof Error ? error.stack : undefined;

  const payload: AppErrorPayload = {
    message,
    stack,
    source: (context.source as string) ?? "react_error_boundary",
    severity: (context.severity as AppErrorPayload["severity"]) ?? "error",
    context: {
      route: window.location.pathname,
      timestamp: new Date().toISOString(),
      ...context,
    },
    timestamp: new Date().toISOString(),
  };

  if (import.meta.env.DEV) {
    console.groupCollapsed(`%c[AppErrorBoundary] ${message}`, "color: #f59e0b; font-weight: bold;");
    console.error(error);
    if (stack) console.debug(stack);
    console.dir(payload.context);
    console.groupEnd();
  }
}

/** Legacy alias retained for safety */
export const reportLovableError = reportAppError;
