using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
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
            return Ok(credentials.OrderBy(c => c.Order).ThenByDescending(c => c.Year).Select(EntityMappings.ToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CredentialDto>> GetCredentialById(int id)
        {
            var credential = await _credentialService.GetCredentialByIdAsync(id);
            return Ok(EntityMappings.ToDto(credential));
        }

        [HttpGet("tab/{tab}")]
        public async Task<ActionResult<IEnumerable<CredentialDto>>> GetCredentialsByTab(string tab)
        {
            var credentials = await _credentialService.GetCredentialsByTabAsync(tab);
            return Ok(credentials.OrderBy(c => c.Order).ThenByDescending(c => c.Year).Select(EntityMappings.ToDto));
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<CredentialDto>>> GetActiveCredentials()
        {
            var credentials = await _credentialService.GetActiveCredentialsAsync();
            return Ok(credentials.OrderBy(c => c.Order).Select(EntityMappings.ToDto));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CredentialDto>> CreateCredential([FromBody] CreateCredentialDto dto)
        {
            // Payload DTO (not the raw EF entity): server-managed fields
            // (createdAt/updatedAt/isDeleted) can never be over-posted.
            var createdCredential = await _credentialService.CreateCredentialAsync(EntityMappings.ToEntity(dto));
            return CreatedAtAction(nameof(GetCredentialById), new { id = createdCredential.Id }, EntityMappings.ToDto(createdCredential));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCredential(int id, [FromBody] UpdateCredentialDto dto)
        {
            // Route id wins; there is no Id in the payload to disagree with it.
            await _credentialService.UpdateCredentialAsync(EntityMappings.ToEntity(dto, id));
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCredential(int id)
        {
            await _credentialService.DeleteCredentialAsync(id);
            return NoContent();
        }
    }
}
