using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogController : ControllerBase
    {
        private readonly IBlogService _blogService;

        public BlogController(IBlogService blogService)
        {
            _blogService = blogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlogPostDto>>> GetAllBlogPosts()
        {
            var blogPosts = await _blogService.GetAllBlogPostsAsync();
            return Ok(blogPosts.OrderByDescending(b => b.PublishedAt).Select(Map));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BlogPostDto>> GetBlogPostById(int id)
        {
            var blogPost = await _blogService.GetBlogPostByIdAsync(id);
            return Ok(Map(blogPost));
        }

        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<BlogPostDto>> GetBlogPostBySlug(string slug)
        {
            var blogPost = await _blogService.GetBlogPostBySlugAsync(slug);
            if (blogPost == null)
            {
                return NotFound();
            }
            return Ok(Map(blogPost));
        }

        [HttpGet("published")]
        public async Task<ActionResult<IEnumerable<BlogPostDto>>> GetPublishedBlogPosts()
        {
            var blogPosts = await _blogService.GetPublishedBlogPostsAsync();
            return Ok(blogPosts.OrderByDescending(b => b.PublishedAt).Select(Map));
        }

        [HttpPost]
        public async Task<ActionResult<BlogPostDto>> CreateBlogPost([FromBody] BlogPost blogPost)
        {
            var createdBlogPost = await _blogService.CreateBlogPostAsync(blogPost);
            return CreatedAtAction(nameof(GetBlogPostById), new { id = createdBlogPost.Id }, Map(createdBlogPost));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBlogPost(int id, [FromBody] BlogPost blogPost)
        {
            if (id != blogPost.Id)
            {
                return BadRequest();
            }

            await _blogService.UpdateBlogPostAsync(blogPost);
            return NoContent();
        }

        [HttpPut("{id}/view")]
        public async Task<IActionResult> IncrementViewCount(int id)
        {
            await _blogService.IncrementViewCountAsync(id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBlogPost(int id)
        {
            await _blogService.DeleteBlogPostAsync(id);
            return NoContent();
        }

        private static BlogPostDto Map(BlogPost b) => new()
        {
            Id = b.Id,
            Title = b.Title,
            Slug = b.Slug,
            Excerpt = b.Excerpt,
            Content = b.Content,
            Author = b.Author,
            PublishedAt = b.PublishedAt,
            Tags = b.Tags?.ToList() ?? new List<string>(),
            FeaturedImage = b.FeaturedImage,
            ReadingTime = b.ReadingTime,
            Published = b.Published,
            ViewCount = b.ViewCount,
            MetaTitle = b.MetaTitle,
            MetaDescription = b.MetaDescription,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };
    }
}
