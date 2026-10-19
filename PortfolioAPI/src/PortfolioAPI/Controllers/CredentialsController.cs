using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CredentialsController : ControllerBase
    {
        private readonly ICredentialService _credentialService;

        public CredentialsController(ICredentialService credentialService)
        {
            _credentialService = credentialService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CredentialDto>>> GetAllCredentials()
        {
            var credentials = await _credentialService.GetAllCredentialsAsync();
            return Ok(credentials.OrderBy(c => c.Order).ThenByDescending(c => c.Year).Select(Map));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CredentialDto>> GetCredentialById(int id)
        {
            var credential = await _credentialService.GetCredentialByIdAsync(id);
            return Ok(Map(credential));
        }

        [HttpGet("tab/{tab}")]
        public async Task<ActionResult<IEnumerable<CredentialDto>>> GetCredentialsByTab(string tab)
        {
            var credentials = await _credentialService.GetCredentialsByTabAsync(tab);
            return Ok(credentials.OrderBy(c => c.Order).ThenByDescending(c => c.Year).Select(Map));
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<CredentialDto>>> GetActiveCredentials()
        {
            var credentials = await _credentialService.GetActiveCredentialsAsync();
            return Ok(credentials.OrderBy(c => c.Order).Select(Map));
        }

        [HttpPost]
        public async Task<ActionResult<CredentialDto>> CreateCredential([FromBody] Credential credential)
        {
            var createdCredential = await _credentialService.CreateCredentialAsync(credential);
            return CreatedAtAction(nameof(GetCredentialById), new { id = createdCredential.Id }, Map(createdCredential));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCredential(int id, [FromBody] Credential credential)
        {
            if (id != credential.Id)
            {
                return BadRequest();
            }

            await _credentialService.UpdateCredentialAsync(credential);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCredential(int id)
        {
            await _credentialService.DeleteCredentialAsync(id);
            return NoContent();
        }

        private static CredentialDto Map(Credential c) => new()
        {
            Id = c.Id,
            Tab = c.Tab,
            Year = c.Year,
            PeriodEn = c.PeriodEn,
            PeriodAr = c.PeriodAr,
            TitleEn = c.TitleEn,
            TitleAr = c.TitleAr,
            OrgEn = c.OrgEn,
            OrgAr = c.OrgAr,
            LocationEn = c.LocationEn,
            LocationAr = c.LocationAr,
            SummaryEn = c.SummaryEn,
            SummaryAr = c.SummaryAr,
            HighlightsEn = c.HighlightsEn?.ToList() ?? new List<string>(),
            HighlightsAr = c.HighlightsAr?.ToList() ?? new List<string>(),
            Stack = c.Stack?.ToList() ?? new List<string>(),
            CredentialId = c.CredentialId,
            StatusEn = c.StatusEn,
            StatusAr = c.StatusAr,
            Featured = c.Featured,
            Order = c.Order,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }
}
