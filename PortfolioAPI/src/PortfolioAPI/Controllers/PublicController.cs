using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    /// <summary>
    /// Public, read-only endpoints consumed by the portfolio website
    /// (portfolio/ frontend). Only content that is active/published/visible
    /// is ever returned here; the admin surface under /api/* keeps full access.
    /// Responses are deterministic lists, ideal for the client's HTTP cache.
    /// </summary>
    [ApiController]
    [Route("api/v1/public")]
    [Route("api/public")]
    public class PublicController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ISkillService _skillService;
        private readonly IExperienceService _experienceService;
        private readonly ITestimonialService _testimonialService;
        private readonly IContactService _contactService;
        private readonly ICredentialService _credentialService;

        public PublicController(
            IProjectService projectService,
            ISkillService skillService,
            IExperienceService experienceService,
            ITestimonialService testimonialService,
            IContactService contactService,
            ICredentialService credentialService)
        {
            _projectService = projectService;
            _skillService = skillService;
            _experienceService = experienceService;
            _testimonialService = testimonialService;
            _contactService = contactService;
            _credentialService = credentialService;
        }

        /// <summary>Active projects (optionally only featured), ordered for display.</summary>
        [HttpGet("projects")]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects([FromQuery] bool? featured)
        {
            var result = featured == true
                ? await _projectService.GetFeaturedAsync()
                : await _projectService.GetAllAsync();

            // GetAllAsync is the admin query (all projects), so the public
            // surface filters to active ones here.
            var projects = (result.Data ?? Enumerable.Empty<ProjectDto>())
                .Where(p => p.IsActive)
                .OrderBy(p => p.Order)
                .ThenByDescending(p => p.CreatedAt);

            return Ok(projects);
        }

        /// <summary>Active skills, grouped-friendly (ordered). Service already filters IsActive in SQL.</summary>
        [HttpGet("skills")]
        public async Task<ActionResult<IEnumerable<SkillDto>>> GetSkills()
        {
            var skills = await _skillService.GetVisibleSkillsAsync();
            return Ok(skills.OrderBy(s => s.Order).ThenBy(s => s.Name).Select(EntityMappings.ToDto));
        }

        /// <summary>Active work experience, current position first.</summary>
        [HttpGet("experience")]
        public async Task<ActionResult<IEnumerable<ExperienceDto>>> GetExperience()
        {
            var experiences = await _experienceService.GetAllExperiencesAsync();
            return Ok(experiences
                .Where(e => e.IsActive)
                .OrderBy(e => e.Order)
                .ThenByDescending(e => e.StartDate)
                .Select(EntityMappings.ToDto));
        }

        /// <summary>Active testimonials, featured first. Service already filters IsActive in SQL.</summary>
        [HttpGet("testimonials")]
        public async Task<ActionResult<IEnumerable<TestimonialDto>>> GetTestimonials()
        {
            var testimonials = await _testimonialService.GetVisibleTestimonialsAsync();
            return Ok(testimonials
                .OrderByDescending(t => t.Featured)
                .ThenBy(t => t.Order)
                .Select(EntityMappings.ToDto));
        }

        /// <summary>Active bilingual credentials (work / projects / education / certifications). Service already filters IsActive in SQL.</summary>
        [HttpGet("credentials")]
        public async Task<ActionResult<IEnumerable<CredentialDto>>> GetCredentials()
        {
            var credentials = await _credentialService.GetActiveCredentialsAsync();
            return Ok(credentials
                .OrderBy(c => c.Order)
                .ThenByDescending(c => c.Year)
                .Select(EntityMappings.ToDto));
        }

        /// <summary>Public contact-form submission (portfolio site's "get in touch").</summary>
        [HttpPost("contact")]
        [EnableRateLimiting("public-submit")]
        public async Task<IActionResult> SubmitContact([FromBody] CreateContactDto dto)
        {
            // Validation runs in ContactService — failures surface as
            // 400 { message, errors } from the exception middleware (issue #16).
            // Contact.From trims, null-guards and clamps IP/UA to column sizes.
            var contact = Contact.From(
                dto,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());

            await _contactService.CreateContactAsync(contact);
            return Ok(new { message = "Thanks! Your message has been sent." });
        }
    }
}
