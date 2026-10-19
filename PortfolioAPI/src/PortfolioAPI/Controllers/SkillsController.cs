using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Route("api/[controller]")]
    public class SkillsController : ControllerBase
    {
        private readonly ISkillService _skillService;

        public SkillsController(ISkillService skillService)
        {
            _skillService = skillService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SkillDto>>> GetAllSkills()
        {
            var skills = await _skillService.GetAllSkillsAsync();
            return Ok(skills.OrderBy(s => s.Order).ThenBy(s => s.Name).Select(EntityMappings.ToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SkillDto>> GetSkillById(int id)
        {
            var skill = await _skillService.GetSkillByIdAsync(id);
            return Ok(EntityMappings.ToDto(skill));
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<SkillDto>>> GetSkillsByCategory(string category)
        {
            var skills = await _skillService.GetSkillsByCategoryAsync(category);
            return Ok(skills.OrderBy(s => s.Order).Select(EntityMappings.ToDto));
        }

        [HttpGet("visible")]
        public async Task<ActionResult<IEnumerable<SkillDto>>> GetVisibleSkills()
        {
            var skills = await _skillService.GetVisibleSkillsAsync();
            return Ok(skills.OrderBy(s => s.Order).Select(EntityMappings.ToDto));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<SkillDto>> CreateSkill([FromBody] CreateSkillDto dto)
        {
            var createdSkill = await _skillService.CreateSkillAsync(EntityMappings.ToEntity(dto));
            return CreatedAtAction(nameof(GetSkillById), new { id = createdSkill.Id }, EntityMappings.ToDto(createdSkill));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateSkill(int id, [FromBody] UpdateSkillDto dto)
        {
            // DTO binding (not the raw EF entity): the route id wins and
            // server-managed fields (createdAt/updatedAt) can never be over-posted.
            await _skillService.UpdateSkillAsync(EntityMappings.ToEntity(dto, id));
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            await _skillService.DeleteSkillAsync(id);
            return NoContent();
        }
    }
}
