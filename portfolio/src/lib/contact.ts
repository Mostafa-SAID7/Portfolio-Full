/**
 * Contact submission layer.
 *
 * Two channels, deliberately ordered (see docs/DASHBOARD_AUDIT.md "Contact
 * delivery"):
 *
 *  1. PortfolioAPI `POST /api/v1/public/contact` — the source of truth. It
 *     validates server-side (FluentValidation), rate-limits (`public-submit`),
 *     persists to SQL Server and feeds the dashboard inbox; the API also fires
 *     its own best-effort SMTP notification.
 *  2. Formspree — redundant email notification, so a submission still reaches
 *     the owner's inbox even when the API's SMTP settings are unusable.
 *
 * Only a successful API call counts as "sent". A Formspree failure never turns
 * a saved message into a fake error, and it is never reported as success.
 */
import { apiOrigin } from "@/lib/api-origin";

export interface ContactFormValues {
  name: string;
  email: string;
  subject: string;
  message: string;
}

export type ContactField = keyof ContactFormValues;

/**
 * What a message is about. The visitor picks one instead of inventing a
 * subject line; the option's localized label is what gets stored, so the
 * dashboard inbox and the owner's email stay readable in the visitor's own
 * language ("other" stores the few words they typed instead).
 */
export const CONTACT_SUBJECT_OPTIONS = [
  "project",
  "fulltime",
  "freelance",
  "collaboration",
  "question",
  "other",
] as const;

export type ContactSubjectOption = (typeof CONTACT_SUBJECT_OPTIONS)[number];

/** ContactValidator's Subject ceiling — "other" text is bounded by the column. */
export const SUBJECT_MAX_LENGTH = 200;

export function isContactSubjectOption(value: unknown): value is ContactSubjectOption {
  return (
    typeof value === "string" &&
    (CONTACT_SUBJECT_OPTIONS as readonly string[]).includes(value)
  );
}

/**
 * Plain boolean variant for schema validation: a type predicate would narrow
 * the form's inferred output type, and the form state must stay the raw string
 * the `<select>` holds (including its empty placeholder value).
 */
export function hasKnownSubjectOption(value: string): boolean {
  return isContactSubjectOption(value);
}

/** Dictionary key carrying an option's label. */
export function contactSubjectLabelKey(option: ContactSubjectOption): string {
  return `contact.subject.${option}`;
}

export interface ContactOutcome {
  ok: boolean;
  message: string;
  /** Server-side rejections, already mapped to form field names. */
  fieldErrors: Partial<Record<ContactField, string>>;
  status?: number;
}

/** Messages must never be lost on a hanging request. */
const SUBMIT_TIMEOUT_MS = 15_000;

/** Public integration identifier (not a secret); override per deployment. */
const FORMSPREE_ID =
  (import.meta.env["VITE_FORMSPREE_ID"] as string | undefined)?.trim() || "xpwlrnla";

const GENERIC_FAILURE =
  "Your message couldn't be sent. Please check your connection and try again.";

function toFormField(key: string): ContactField | null {
  const word = /^(name|email|subject|message)$/i.exec(key)?.[1]?.toLowerCase();
  if (!word) return null;
  return (word.charAt(0).toUpperCase() + word.slice(1)) as ContactField;
}

function readFieldErrors(body: unknown): Partial<Record<ContactField, string>> {
  const errors: Partial<Record<ContactField, string>> = {};
  if (typeof body !== "object" || body === null || !("errors" in body)) return errors;
  const raw = (body as { errors: unknown }).errors;
  if (typeof raw !== "object" || raw === null) return errors;
  for (const [key, value] of Object.entries(raw as Record<string, unknown>)) {
    const field = toFormField(key);
    if (!field) continue;
    const text = Array.isArray(value)
      ? value.filter((v) => typeof v === "string").join(" ")
      : value;
    if (typeof text === "string" && text.trim()) errors[field] = text.trim();
  }
  return errors;
}

function serverMessage(status: number, body: unknown): string {
  const detail =
    typeof body === "object" && body !== null && "message" in body
      ? (body as { message: unknown }).message
      : undefined;
  if (typeof detail === "string" && detail.trim()) return detail.trim();
  if (status === 429) {
    return "Too many messages at once — please wait a minute and try again.";
  }
  return GENERIC_FAILURE;
}

async function post(url: string, payload: unknown): Promise<Response> {
  return fetch(url, {
    method: "POST",
    headers: { "content-type": "application/json", accept: "application/json" },
    body: JSON.stringify(payload),
    signal: AbortSignal.timeout(SUBMIT_TIMEOUT_MS),
  });
}

/** Submit to the PortfolioAPI: persists the message and feeds the dashboard inbox. */
export async function submitContact(values: ContactFormValues): Promise<ContactOutcome> {
  try {
    const response = await post(`${apiOrigin()}/api/v1/public/contact`, values);
    const body: unknown = await response.json().catch(() => null);

    if (!response.ok) {
      // Never log the payload — it carries the visitor's name, email and text.
      console.error(`[contact] API rejected the submission (${response.status}).`);
      return {
        ok: false,
        message: serverMessage(response.status, body),
        fieldErrors: readFieldErrors(body),
        status: response.status,
      };
    }

    return {
      ok: true,
      message:
        typeof body === "object" && body !== null && "message" in body
          ? String((body as { message: unknown }).message)
          : "Your message has been received.",
      fieldErrors: {},
      status: response.status,
    };
  } catch (error) {
    console.error("[contact] The API submission could not be delivered.", error);
    return { ok: false, message: GENERIC_FAILURE, fieldErrors: {} };
  }
}

/**
 * Best-effort email notification through Formspree. Formspree uses the `email`
 * field as Reply-To, so the owner can answer the visitor directly; the visitor
 * is never treated as a trusted sender identity.
 */
export async function notifyViaFormspree(values: ContactFormValues): Promise<boolean> {
  try {
    const response = await post(`https://formspree.io/f/${FORMSPREE_ID}`, {
      name: values.name,
      email: values.email,
      message: values.message,
      _subject: values.subject,
    });
    if (!response.ok) {
      console.error(`[contact] Formspree notification failed (${response.status}).`);
      return false;
    }
    return true;
  } catch (error) {
    console.error("[contact] Formspree notification could not be delivered.", error);
    return false;
  }
}
