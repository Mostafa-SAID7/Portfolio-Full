using System;
using PortfolioAPI.Core.DTOs;

namespace PortfolioAPI.Core.Entities
{
    public class Contact : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        /// <summary>
        /// Single factory for contact-form submissions (both the admin and the
        /// public controller used to duplicate this block). Trims input and
        /// clamps request-derived metadata to the column sizes so an oversized
        /// User-Agent header can never cause a SQL truncation 500. Null JSON
        /// values become empty strings so the validator produces a clean
        /// 400 instead of a NullReferenceException 500.
        /// </summary>
        public static Contact From(CreateContactDto dto, string? ipAddress, string? userAgent)
        {
            return new Contact
            {
                Name = dto.Name?.Trim() ?? string.Empty,
                Email = dto.Email?.Trim() ?? string.Empty,
                Subject = dto.Subject?.Trim() ?? string.Empty,
                Message = dto.Message?.Trim() ?? string.Empty,
                IpAddress = Clamp(ipAddress, 45),
                UserAgent = Clamp(userAgent, 500)
            };
        }

        /// <summary>Marks the message as read (server-owned timestamps).</summary>
        public void MarkRead()
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }

        private static string? Clamp(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}