using FluentValidation;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Application.Services
{
    public class ContactService : IContactService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<Contact> _validator;

        public ContactService(IUnitOfWork unitOfWork, IValidator<Contact> validator)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<IEnumerable<Contact>> GetAllContactsAsync()
        {
            return await _unitOfWork.Contacts.GetAllAsync();
        }

        public async Task<Contact?> GetContactByIdAsync(int id)
        {
            var contact = await _unitOfWork.Contacts.GetByIdAsync(id);
            if (contact == null)
            {
                throw new NotFoundException($"Contact with id {id} not found.");
            }
            return contact;
        }

        public async Task<IEnumerable<Contact>> GetUnreadContactsAsync()
        {
            return await _unitOfWork.Contacts.FindAsync(c => !c.IsRead);
        }

        public async Task<Contact> CreateContactAsync(Contact contact)
        {
            var validationResult = await _validator.ValidateAsync(contact);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            contact.CreatedAt = DateTime.UtcNow;
            var createdContact = await _unitOfWork.Contacts.AddAsync(contact);
            await _unitOfWork.SaveChangesAsync();
            return createdContact;
        }

        public async Task MarkAsReadAsync(int id)
        {
            var contact = await _unitOfWork.Contacts.GetByIdAsync(id);
            if (contact == null)
            {
                throw new NotFoundException($"Contact with id {id} not found.");
            }

            contact.IsRead = true;
            contact.ReadAt = DateTime.UtcNow;
            await _unitOfWork.Contacts.UpdateAsync(contact);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteContactAsync(int id)
        {
            var contact = await _unitOfWork.Contacts.GetByIdAsync(id);
            if (contact == null)
            {
                throw new NotFoundException($"Contact with id {id} not found.");
            }

            await _unitOfWork.Contacts.DeleteAsync(contact);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}