/**
 * Admin identity returned by POST /api/auth/login on the Portfolio API.
 */
export interface AdminUser {
  id: string;
  name: string;
  email: string;
}

/**
 * Persisted client session: the JWT issued by the API plus the identity it
 * belongs to. Stored in localStorage and refreshed only by a successful
 * login; cleared on logout and on any 401 response (issue #20).
 */
export interface AdminSession {
  /** Raw JWT (sent as `Authorization: Bearer <token>` by authInterceptor). */
  token: string;
  /** ISO timestamp from the API — sessions past this are dropped on load. */
  expiresAt: string;
  user: AdminUser;
}
