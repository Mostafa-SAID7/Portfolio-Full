using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
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
            return Ok(testimonials.OrderBy(t => t.Order).ThenByDescending(t => t.Rating).Select(Map));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TestimonialDto>> GetTestimonialById(int id)
        {
            var testimonial = await _testimonialService.GetTestimonialByIdAsync(id);
            return Ok(Map(testimonial));
        }

        [HttpGet("visible")]
        public async Task<ActionResult<IEnumerable<TestimonialDto>>> GetVisibleTestimonials()
        {
            var testimonials = await _testimonialService.GetVisibleTestimonialsAsync();
            return Ok(testimonials.OrderBy(t => t.Order).Select(Map));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<TestimonialDto>> CreateTestimonial([FromBody] Testimonial testimonial)
        {
            var createdTestimonial = await _testimonialService.CreateTestimonialAsync(testimonial);
            return CreatedAtAction(nameof(GetTestimonialById), new { id = createdTestimonial.Id }, Map(createdTestimonial));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateTestimonial(int id, [FromBody] Testimonial testimonial)
        {
            if (id != testimonial.Id)
            {
                return BadRequest();
            }

            await _testimonialService.UpdateTestimonialAsync(testimonial);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTestimonial(int id)
        {
            await _testimonialService.DeleteTestimonialAsync(id);
            return NoContent();
        }

        private static TestimonialDto Map(Testimonial t) => new()
        {
            Id = t.Id,
            Name = t.Name,
            Position = t.Position,
            Company = t.Company,
            Content = t.Content,
            ImageUrl = t.ImageUrl,
            Rating = t.Rating,
            Featured = t.Featured,
            Order = t.Order,
            IsActive = t.IsActive,
            ProjectUrl = t.ProjectUrl,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }
}
