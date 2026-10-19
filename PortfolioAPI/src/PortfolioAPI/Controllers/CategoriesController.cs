using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    /// <summary>
    /// Project categories — the list behind the dashboard's category dropdown
    /// and the filter chips on the public portfolio.
    ///
    /// Admin-only (a category drives what visitors see): every route lives
    /// under the versioned namespace; the public read-only list is served by
    /// PublicController (/api/v1/public/categories).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>All categories, ordered (Order, then name) with the usage count.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var usage = await _categoryService.GetUsageCountsAsync();

            return Ok(categories.Select(category =>
            {
                var dto = EntityMappings.ToDto(category);
                dto.ProjectCount = usage.TryGetValue(category.Name, out var count) ? count : 0;
                return dto;
            }));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            return Ok(EntityMappings.ToDto(category));
        }

        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            var category = await _categoryService.CreateCategoryAsync(EntityMappings.ToEntity(dto));
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, EntityMappings.ToDto(category));
        }

        /// <summary>
        /// Renames/reorders a category. A rename also rewrites the projects
        /// that use it, so the public site keeps showing them.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
        {
            var category = EntityMappings.ToEntity(dto, id);
            await _categoryService.UpdateCategoryAsync(category);
            return NoContent();
        }

        /// <summary>Deletes a category (refused while projects still use it).</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            await _categoryService.DeleteCategoryAsync(id);
            return NoContent();
        }
    }
}
