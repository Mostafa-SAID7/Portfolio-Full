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
            var skillDtos = skills.Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                ProficiencyLevel = s.ProficiencyLevel,
                IconUrl = s.IconUrl,
                IsVisible = s.IsVisible,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            });
            return Ok(skillDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SkillDto>> GetSkillById(int id)
        {
            var skill = await _skillService.GetSkillByIdAsync(id);
            var skillDto = new SkillDto
            {
                Id = skill.Id,
                Name = skill.Name,
                Category = skill.Category,
                ProficiencyLevel = skill.ProficiencyLevel,
                IconUrl = skill.IconUrl,
                IsVisible = skill.IsVisible,
                CreatedAt = skill.CreatedAt,
                UpdatedAt = skill.UpdatedAt
            };
            return Ok(skillDto);
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<SkillDto>>> GetSkillsByCategory(string category)
        {
            var skills = await _skillService.GetSkillsByCategoryAsync(category);
            var skillDtos = skills.Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                ProficiencyLevel = s.ProficiencyLevel,
                IconUrl = s.IconUrl,
                IsVisible = s.IsVisible,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            });
            return Ok(skillDtos);
        }

        [HttpGet("visible")]
        public async Task<ActionResult<IEnumerable<SkillDto>>> GetVisibleSkills()
        {
            var skills = await _skillService.GetVisibleSkillsAsync();
            var skillDtos = skills.Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                ProficiencyLevel = s.ProficiencyLevel,
                IconUrl = s.IconUrl,
                IsVisible = s.IsVisible,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            });
            return Ok(skillDtos);
        }

        [HttpPost]
        public async Task<ActionResult<SkillDto>> CreateSkill(Skill skill)
        {
            var createdSkill = await _skillService.CreateSkillAsync(skill);
            var skillDto = new SkillDto
            {
                Id = createdSkill.Id,
                Name = createdSkill.Name,
                Category = createdSkill.Category,
                ProficiencyLevel = createdSkill.ProficiencyLevel,
                IconUrl = createdSkill.IconUrl,
                IsVisible = createdSkill.IsVisible,
                CreatedAt = createdSkill.CreatedAt,
                UpdatedAt = createdSkill.UpdatedAt
            };
            return CreatedAtAction(nameof(GetSkillById), new { id = skillDto.Id }, skillDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSkill(int id, Skill skill)
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
    }
}