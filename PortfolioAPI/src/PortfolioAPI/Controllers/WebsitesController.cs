using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Application.Interfaces;

namespace PortfolioAPI.Controllers
{
    [Route("api/v1/[controller]")]
    [Route("api/[controller]")]
    public class WebsitesController : ApiControllerBase
    {
        private readonly IWebsiteService _websiteService;

        public WebsitesController(IWebsiteService websiteService)
        {
            _websiteService = websiteService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WebsiteDto>>> GetAll()
        {
            var result = await _websiteService.GetAllAsync();
            return Mapped(result);
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<WebsiteDto>>> GetActive()
        {
            var result = await _websiteService.GetActiveWebsitesAsync();
            return Mapped(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<WebsiteDto>> GetById(int id)
        {
            var result = await _websiteService.GetByIdAsync(id);
            return Mapped(result);
        }

        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<WebsiteDto>> GetBySlug(string slug)
        {
            var result = await _websiteService.GetBySlugAsync(slug);
            return Mapped(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<WebsiteDto>> Create([FromBody] CreateWebsiteDto dto)
        {
            var result = await _websiteService.CreateAsync(dto);
            if (!result.Success || result.Data == null)
            {
                return Failure(result);
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result.Data);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult<WebsiteDto>> Update(int id, [FromBody] UpdateWebsiteDto dto)
        {
            var result = await _websiteService.UpdateAsync(id, dto);
            return Mapped(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _websiteService.DeleteAsync(id);
            return Mapped(result);
        }

        [HttpPatch("{id:int}/toggle-active")]
        [Authorize]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var result = await _websiteService.ToggleActiveAsync(id);
            return Mapped(result);
        }
    }
}

