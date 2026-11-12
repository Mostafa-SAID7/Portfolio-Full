using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Mappings
{
    /// <summary>Contact and ContactReply entity <-> DTO mappings.</summary>
    public static partial class EntityMappings
    {
        public static ContactDto ToDto(Contact c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Subject = c.Subject,
            Message = c.Message,
            IsRead = c.IsRead,
            ReadAt = c.ReadAt,
            IpAddress = c.IpAddress,
            UserAgent = c.UserAgent,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };

        public static ContactReplyDto ToDto(ContactReply r) => new()
        {
            Id = r.Id,
            ContactId = r.ContactId,
            RecipientEmail = r.RecipientEmail,
            Subject = r.Subject,
            Body = r.Body,
            SentAt = r.SentAt
        };

        public static Contact ToEntity(CreateContactDto dto, string? ipAddress = null, string? userAgent = null)
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

        private static string? Clamp(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
