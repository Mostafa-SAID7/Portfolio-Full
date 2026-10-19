using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IContactService _contactService;

        public ContactController(IContactService contactService)
        {
            _contactService = contactService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContactDto>>> GetAllContacts()
        {
            var contacts = await _contactService.GetAllContactsAsync();
            return Ok(contacts.OrderByDescending(c => c.CreatedAt).Select(Map));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContactDto>> GetContactById(int id)
        {
            var contact = await _contactService.GetContactByIdAsync(id);
            return Ok(Map(contact));
        }

        [HttpGet("unread")]
        public async Task<ActionResult<IEnumerable<ContactDto>>> GetUnreadContacts()
        {
            var contacts = await _contactService.GetUnreadContactsAsync();
            return Ok(contacts.OrderByDescending(c => c.CreatedAt).Select(Map));
        }

        /// <summary>Public contact-form submission (from the portfolio site).</summary>
        [HttpPost]
        public async Task<ActionResult<ContactDto>> CreateContact([FromBody] CreateContactDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var contact = new Contact
            {
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim(),
                Subject = dto.Subject.Trim(),
                Message = dto.Message.Trim(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };

            var createdContact = await _contactService.CreateContactAsync(contact);
            return CreatedAtAction(nameof(GetContactById), new { id = createdContact.Id }, Map(createdContact));
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _contactService.MarkAsReadAsync(id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            await _contactService.DeleteContactAsync(id);
            return NoContent();
        }

        private static ContactDto Map(Contact c) => new()
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
    }
}
