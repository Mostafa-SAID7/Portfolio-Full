using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    // The whole inbox (reads include visitor PII + every mutation) is admin-
    // only. The public submission path is POST /api/public/contact.
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
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
            return Ok(contacts.OrderByDescending(c => c.CreatedAt).Select(EntityMappings.ToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContactDto>> GetContactById(int id)
        {
            var contact = await _contactService.GetContactByIdAsync(id);
            return Ok(EntityMappings.ToDto(contact));
        }

        [HttpGet("unread")]
        public async Task<ActionResult<IEnumerable<ContactDto>>> GetUnreadContacts()
        {
            var contacts = await _contactService.GetUnreadContactsAsync();
            return Ok(contacts.OrderByDescending(c => c.CreatedAt).Select(EntityMappings.ToDto));
        }

        /// <summary>Admin-side message composition (public form: POST /api/v1/public/contact).</summary>
        [HttpPost]
        public async Task<ActionResult<ContactDto>> CreateContact([FromBody] CreateContactDto dto)
        {
            // Validation runs in ContactService via ContactValidator — failures
            // surface as 400 { message, errors } from the exception middleware.
            // Contact.From trims, null-guards and clamps IP/UA to column sizes.
            var contact = Contact.From(
                dto,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());

            var createdContact = await _contactService.CreateContactAsync(contact);
            return CreatedAtAction(nameof(GetContactById), new { id = createdContact.Id }, EntityMappings.ToDto(createdContact));
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
    }
}
