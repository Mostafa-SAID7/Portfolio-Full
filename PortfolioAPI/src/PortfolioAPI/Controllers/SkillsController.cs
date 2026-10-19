using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
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
            return Ok(skills.OrderBy(s => s.Order).ThenBy(s => s.Name).Select(Map));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SkillDto>> GetSkillById(int id)
        {
            var skill = await _skillService.GetSkillByIdAsync(id);
            return Ok(Map(skill));
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<SkillDto>>> GetSkillsByCategory(string category)
        {
            var skills = await _skillService.GetSkillsByCategoryAsync(category);
            return Ok(skills.OrderBy(s => s.Order).Select(Map));
        }

        [HttpGet("visible")]
        public async Task<ActionResult<IEnumerable<SkillDto>>> GetVisibleSkills()
        {
            var skills = await _skillService.GetVisibleSkillsAsync();
            return Ok(skills.OrderBy(s => s.Order).Select(Map));
        }

        [HttpPost]
        public async Task<ActionResult<SkillDto>> CreateSkill([FromBody] Skill skill)
        {
            var createdSkill = await _skillService.CreateSkillAsync(skill);
            return CreatedAtAction(nameof(GetSkillById), new { id = createdSkill.Id }, Map(createdSkill));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSkill(int id, [FromBody] Skill skill)
        {
            if (id != skill.Id)
            {
                return BadRequest();
            }

            await _skillService.UpdateSkillAsync(skill);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            await _skillService.DeleteSkillAsync(id);
            return NoContent();
        }

        private static SkillDto Map(Skill s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Category = s.Category,
            Proficiency = s.Proficiency,
            IconUrl = s.IconUrl,
            Description = s.Description,
            Order = s.Order,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }
}
