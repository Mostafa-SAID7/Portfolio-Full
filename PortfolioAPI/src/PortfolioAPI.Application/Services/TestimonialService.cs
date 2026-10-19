using FluentValidation;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Application.Services
{
    public class TestimonialService : ITestimonialService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<Testimonial> _validator;

        public TestimonialService(IUnitOfWork unitOfWork, IValidator<Testimonial> validator)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<IEnumerable<Testimonial>> GetAllTestimonialsAsync()
        {
            return await _unitOfWork.Testimonials.GetAllAsync();
        }

        public async Task<Testimonial?> GetTestimonialByIdAsync(int id)
        {
            var testimonial = await _unitOfWork.Testimonials.GetByIdAsync(id);
            if (testimonial == null)
            {
                throw new NotFoundException($"Testimonial with id {id} not found.");
            }
            return testimonial;
        }

        public async Task<IEnumerable<Testimonial>> GetVisibleTestimonialsAsync()
        {
            return await _unitOfWork.Testimonials.FindAsync(t => t.IsActive);
        }

        public async Task<Testimonial> CreateTestimonialAsync(Testimonial testimonial)
        {
            var validationResult = await _validator.ValidateAsync(testimonial);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            testimonial.CreatedAt = DateTime.UtcNow;
            var createdTestimonial = await _unitOfWork.Testimonials.AddAsync(testimonial);
            await _unitOfWork.SaveChangesAsync();
            return createdTestimonial;
        }

        public async Task UpdateTestimonialAsync(Testimonial testimonial)
        {
            var existingTestimonial = await _unitOfWork.Testimonials.GetByIdAsync(testimonial.Id);
            if (existingTestimonial == null)
            {
                throw new NotFoundException($"Testimonial with id {testimonial.Id} not found.");
            }

            var validationResult = await _validator.ValidateAsync(testimonial);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            testimonial.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Testimonials.UpdateAsync(testimonial);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteTestimonialAsync(int id)
        {
            var testimonial = await _unitOfWork.Testimonials.GetByIdAsync(id);
            if (testimonial == null)
            {
                throw new NotFoundException($"Testimonial with id {id} not found.");
            }

            await _unitOfWork.Testimonials.DeleteAsync(testimonial);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}