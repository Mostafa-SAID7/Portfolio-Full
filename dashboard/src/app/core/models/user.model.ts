/**
 * Local admin session identity.
 *
 * The Portfolio API does not expose authentication endpoints yet, so the
 * dashboard keeps a local unlock session (name + email) in localStorage.
 * When JWT auth is added to the API, only AuthService needs to change.
 */
export interface AdminUser {
  id: string;
  name: string;
  email: string;
}
