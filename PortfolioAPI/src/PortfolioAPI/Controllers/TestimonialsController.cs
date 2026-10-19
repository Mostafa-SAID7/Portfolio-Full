using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
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
            return Ok(testimonials.OrderBy(t => t.Order).ThenByDescending(t => t.Rating).Select(EntityMappings.ToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TestimonialDto>> GetTestimonialById(int id)
        {
            var testimonial = await _testimonialService.GetTestimonialByIdAsync(id);
            return Ok(EntityMappings.ToDto(testimonial));
        }

        [HttpGet("visible")]
        public async Task<ActionResult<IEnumerable<TestimonialDto>>> GetVisibleTestimonials()
        {
            var testimonials = await _testimonialService.GetVisibleTestimonialsAsync();
            return Ok(testimonials.OrderBy(t => t.Order).Select(EntityMappings.ToDto));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<TestimonialDto>> CreateTestimonial([FromBody] CreateTestimonialDto dto)
        {
            // Payload DTO (not the raw EF entity): server-managed fields
            // (createdAt/updatedAt/isDeleted) can never be over-posted.
            var createdTestimonial = await _testimonialService.CreateTestimonialAsync(EntityMappings.ToEntity(dto));
            return CreatedAtAction(nameof(GetTestimonialById), new { id = createdTestimonial.Id }, EntityMappings.ToDto(createdTestimonial));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateTestimonial(int id, [FromBody] UpdateTestimonialDto dto)
        {
            // Route id wins; there is no Id in the payload to disagree with it.
            await _testimonialService.UpdateTestimonialAsync(EntityMappings.ToEntity(dto, id));
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTestimonial(int id)
        {
            await _testimonialService.DeleteTestimonialAsync(id);
            return NoContent();
        }
    }
}
