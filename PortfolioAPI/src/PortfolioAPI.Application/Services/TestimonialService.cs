using FluentValidation;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Application.Services
{
    public class TestimonialService : CrudService<Testimonial>, ITestimonialService
    {
        public TestimonialService(IUnitOfWork unitOfWork, IValidator<Testimonial> validator)
            : base(unitOfWork, validator)
        {
        }

        protected override IRepository<Testimonial> Repository => UnitOfWork.Testimonials;

        public async Task<IEnumerable<Testimonial>> GetAllTestimonialsAsync()
        {
            return await UnitOfWork.Testimonials.GetAllAsync();
        }

        public async Task<Testimonial> GetTestimonialByIdAsync(int id)
        {
            return await GetRequiredAsync(id);
        }

        public async Task<IEnumerable<Testimonial>> GetVisibleTestimonialsAsync()
        {
            return await UnitOfWork.Testimonials.FindAsync(t => t.IsActive);
        }

        public async Task<Testimonial> CreateTestimonialAsync(Testimonial testimonial)
        {
            return await CreateValidatedAsync(testimonial);
        }

        public async Task UpdateTestimonialAsync(Testimonial testimonial)
        {
            // Apply onto the tracked instance — attaching the incoming
            // detached entity (the previous behaviour) threw EF's
            // "another instance with the same key ... is already being tracked"
            // and surfaced as an HTTP 500 on every testimonial update.
            await UpdateTrackedAsync(testimonial.Id, existing => existing.Apply(testimonial));
        }

        public async Task DeleteTestimonialAsync(int id)
        {
            await DeleteOrThrowAsync(id);
        }
    }
}
