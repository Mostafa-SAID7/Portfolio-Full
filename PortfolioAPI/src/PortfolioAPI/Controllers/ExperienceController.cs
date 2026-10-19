using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Route("api/[controller]")]
    public class ExperienceController : ControllerBase
    {
        private readonly IExperienceService _experienceService;

        public ExperienceController(IExperienceService experienceService)
        {
            _experienceService = experienceService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExperienceDto>>> GetAllExperiences()
        {
            var experiences = await _experienceService.GetAllExperiencesAsync();
            return Ok(experiences.OrderBy(e => e.Order).ThenByDescending(e => e.StartDate).Select(EntityMappings.ToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExperienceDto>> GetExperienceById(int id)
        {
            var experience = await _experienceService.GetExperienceByIdAsync(id);
            return Ok(EntityMappings.ToDto(experience));
        }

        [HttpGet("current")]
        public async Task<ActionResult<IEnumerable<ExperienceDto>>> GetCurrentExperiences()
        {
            var experiences = await _experienceService.GetCurrentExperiencesAsync();
            return Ok(experiences.Select(EntityMappings.ToDto));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ExperienceDto>> CreateExperience([FromBody] CreateExperienceDto dto)
        {
            var createdExperience = await _experienceService.CreateExperienceAsync(EntityMappings.ToEntity(dto));
            return CreatedAtAction(nameof(GetExperienceById), new { id = createdExperience.Id }, EntityMappings.ToDto(createdExperience));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateExperience(int id, [FromBody] UpdateExperienceDto dto)
        {
            // DTO binding (not the raw EF entity): the route id wins and
            // server-managed fields (createdAt/updatedAt) can never be over-posted.
            await _experienceService.UpdateExperienceAsync(EntityMappings.ToEntity(dto, id));
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteExperience(int id)
        {
            await _experienceService.DeleteExperienceAsync(id);
            return NoContent();
        }
    }
}
