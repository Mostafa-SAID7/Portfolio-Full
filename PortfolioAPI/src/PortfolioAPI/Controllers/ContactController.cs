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
            var contactDtos = contacts.Select(c => new ContactDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                Subject = c.Subject,
                Message = c.Message,
                IsRead = c.IsRead,
                ReadAt = c.ReadAt,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            });
            return Ok(contactDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContactDto>> GetContactById(int id)
        {
            var contact = await _contactService.GetContactByIdAsync(id);
            var contactDto = new ContactDto
            {
                Id = contact.Id,
                Name = contact.Name,
                Email = contact.Email,
                Phone = contact.Phone,
                Subject = contact.Subject,
                Message = contact.Message,
                IsRead = contact.IsRead,
                ReadAt = contact.ReadAt,
                CreatedAt = contact.CreatedAt,
                UpdatedAt = contact.UpdatedAt
            };
            return Ok(contactDto);
        }

        [HttpGet("unread")]
        public async Task<ActionResult<IEnumerable<ContactDto>>> GetUnreadContacts()
        {
            var contacts = await _contactService.GetUnreadContactsAsync();
            var contactDtos = contacts.Select(c => new ContactDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                Subject = c.Subject,
                Message = c.Message,
                IsRead = c.IsRead,
                ReadAt = c.ReadAt,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            });
            return Ok(contactDtos);
        }

        [HttpPost]
        public async Task<ActionResult<ContactDto>> CreateContact(Contact contact)
        {
            var createdContact = await _contactService.CreateContactAsync(contact);
            var contactDto = new ContactDto
            {
                Id = createdContact.Id,
                Name = createdContact.Name,
                Email = createdContact.Email,
                Phone = createdContact.Phone,
                Subject = createdContact.Subject,
                Message = createdContact.Message,
                IsRead = createdContact.IsRead,
                ReadAt = createdContact.ReadAt,
                CreatedAt = createdContact.CreatedAt,
                UpdatedAt = createdContact.UpdatedAt
            };
            return CreatedAtAction(nameof(GetContactById), new { id = contactDto.Id }, contactDto);
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