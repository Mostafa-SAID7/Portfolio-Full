using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Core.Interfaces.Services
{
    public interface ITestimonialService
    {
        Task<IEnumerable<Testimonial>> GetAllTestimonialsAsync();
        Task<Testimonial> GetTestimonialByIdAsync(int id);
        Task<IEnumerable<Testimonial>> GetVisibleTestimonialsAsync();
        Task<Testimonial> CreateTestimonialAsync(Testimonial testimonial);
        Task UpdateTestimonialAsync(Testimonial testimonial);
        Task DeleteTestimonialAsync(int id);
    }
}