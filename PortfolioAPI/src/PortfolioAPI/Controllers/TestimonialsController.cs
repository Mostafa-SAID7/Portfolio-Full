using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestimonialsController : ControllerBase
    {
        private readonly ITestimonialService _testimonialService;

        public TestimonialsController(ITestimonialService testimonialService)
        {
            _testimonialService = testimonialService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TestimonialDto>>> GetAllTestimonials()
        {
            var testimonials = await _testimonialService.GetAllTestimonialsAsync();
            var testimonialDtos = testimonials.Select(t => new TestimonialDto
            {
                Id = t.Id,
                ClientName = t.ClientName,
                ClientPosition = t.ClientPosition,
                ClientCompany = t.ClientCompany,
                Content = t.Content,
                Rating = t.Rating,
                ClientImageUrl = t.ClientImageUrl,
                IsVisible = t.IsVisible,
                GivenDate = t.GivenDate,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            });
            return Ok(testimonialDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TestimonialDto>> GetTestimonialById(int id)
        {
            var testimonial = await _testimonialService.GetTestimonialByIdAsync(id);
            var testimonialDto = new TestimonialDto
            {
                Id = testimonial.Id,
                ClientName = testimonial.ClientName,
                ClientPosition = testimonial.ClientPosition,
                ClientCompany = testimonial.ClientCompany,
                Content = testimonial.Content,
                Rating = testimonial.Rating,
                ClientImageUrl = testimonial.ClientImageUrl,
                IsVisible = testimonial.IsVisible,
                GivenDate = testimonial.GivenDate,
                CreatedAt = testimonial.CreatedAt,
                UpdatedAt = testimonial.UpdatedAt
            };
            return Ok(testimonialDto);
        }

        [HttpGet("visible")]
        public async Task<ActionResult<IEnumerable<TestimonialDto>>> GetVisibleTestimonials()
        {
            var testimonials = await _testimonialService.GetVisibleTestimonialsAsync();
            var testimonialDtos = testimonials.Select(t => new TestimonialDto
            {
                Id = t.Id,
                ClientName = t.ClientName,
                ClientPosition = t.ClientPosition,
                ClientCompany = t.ClientCompany,
                Content = t.Content,
                Rating = t.Rating,
                ClientImageUrl = t.ClientImageUrl,
                IsVisible = t.IsVisible,
                GivenDate = t.GivenDate,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            });
            return Ok(testimonialDtos);
        }

        [HttpPost]
        public async Task<ActionResult<TestimonialDto>> CreateTestimonial(Testimonial testimonial)
        {
            var createdTestimonial = await _testimonialService.CreateTestimonialAsync(testimonial);
            var testimonialDto = new TestimonialDto
            {
                Id = createdTestimonial.Id,
                ClientName = createdTestimonial.ClientName,
                ClientPosition = createdTestimonial.ClientPosition,
                ClientCompany = createdTestimonial.ClientCompany,
                Content = createdTestimonial.Content,
                Rating = createdTestimonial.Rating,
                ClientImageUrl = createdTestimonial.ClientImageUrl,
                IsVisible = createdTestimonial.IsVisible,
                GivenDate = createdTestimonial.GivenDate,
                CreatedAt = createdTestimonial.CreatedAt,
                UpdatedAt = createdTestimonial.UpdatedAt
            };
            return CreatedAtAction(nameof(GetTestimonialById), new { id = testimonialDto.Id }, testimonialDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTestimonial(int id, Testimonial testimonial)
        {
            if (id != testimonial.Id)
            {
                return BadRequest();
            }

            await _testimonialService.UpdateTestimonialAsync(testimonial);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTestimonial(int id)
        {
            await _testimonialService.DeleteTestimonialAsync(id);
            return NoContent();
        }
    }
}