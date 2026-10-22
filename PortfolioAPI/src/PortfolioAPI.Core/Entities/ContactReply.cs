using System;

namespace PortfolioAPI.Core.Entities
{
    /// <summary>
    /// A reply the admin sent back to a visitor from the dashboard inbox.
    /// Only a DELIVERED reply is stored — the service sends first and records
    /// afterwards, so the inbox can never show an answer that never went out.
    /// </summary>
    public class ContactReply : BaseEntity
    {
        public int ContactId { get; set; }
        public Contact? Contact { get; set; }

        /// <summary>Snapshot of the address used, so a later visitor edit cannot rewrite history.</summary>
        public string RecipientEmail { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public const int BodyMaxLength = 2000;

        /// <summary>Builds a pending reply for a stored message (nothing is persisted or sent yet).</summary>
        public static ContactReply ForVisitor(Contact contact, string? body) => new()
        {
            ContactId = contact.Id,
            RecipientEmail = contact.Email,
            Subject = BuildSubject(contact.Subject),
            Body = (body ?? string.Empty).Trim(),
            SentAt = DateTime.UtcNow
        };

        /// <summary>"Re: " threading, clamped to the column size so a long subject cannot 500 the insert.</summary>
        public static string BuildSubject(string contactSubject)
        {
            const int maxLength = 255;
            var title = string.IsNullOrWhiteSpace(contactSubject) ? "Your message" : contactSubject.Trim();
            var threaded = title.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? title : $"Re: {title}";
            return threaded.Length <= maxLength ? threaded : threaded[..maxLength];
        }
    }
}
