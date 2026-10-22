/** Mirrors PortfolioAPI.Core.DTOs.ContactDto (api/Contact inbox). */
export interface ContactMessage {
  id: number;
  name: string;
  email: string;
  subject: string;
  message: string;
  isRead: boolean;
  readAt: string | null;
  ipAddress: string | null;
  userAgent: string | null;
  createdAt: string;
  updatedAt: string | null;
}

/** Mirrors PortfolioAPI.Core.DTOs.ContactReplyDto (api/Contact/{id}/replies). */
export interface ContactReply {
  id: number;
  contactId: number;
  recipientEmail: string;
  subject: string;
  body: string;
  sentAt: string;
}

/** ContactReply.BodyMaxLength — the API rejects anything longer. */
export const REPLY_BODY_MAX_LENGTH = 2000;

/** Same threading rule the API applies when it builds the reply subject. */
export function replySubjectOf(messageSubject: string): string {
  const title = messageSubject?.trim() || 'Your message';
  return /^re:/i.test(title) ? title : `Re: ${title}`;
}
