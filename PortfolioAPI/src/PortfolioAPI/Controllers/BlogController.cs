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
            var blogPostDtos = blogPosts.Select(b => new BlogPostDto
            {
                Id = b.Id,
                Title = b.Title,
                Slug = b.Slug,
                Content = b.Content,
                Excerpt = b.Excerpt,
                Author = b.Author,
                FeaturedImageUrl = b.FeaturedImageUrl,
                Tags = b.Tags,
                IsPublished = b.IsPublished,
                PublishedAt = b.PublishedAt,
                ReadTimeMinutes = b.ReadTimeMinutes,
                ViewCount = b.ViewCount,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            });
            return Ok(blogPostDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BlogPostDto>> GetBlogPostById(int id)
        {
            var blogPost = await _blogService.GetBlogPostByIdAsync(id);
            var blogPostDto = new BlogPostDto
            {
                Id = blogPost.Id,
                Title = blogPost.Title,
                Slug = blogPost.Slug,
                Content = blogPost.Content,
                Excerpt = blogPost.Excerpt,
                Author = blogPost.Author,
                FeaturedImageUrl = blogPost.FeaturedImageUrl,
                Tags = blogPost.Tags,
                IsPublished = blogPost.IsPublished,
                PublishedAt = blogPost.PublishedAt,
                ReadTimeMinutes = blogPost.ReadTimeMinutes,
                ViewCount = blogPost.ViewCount,
                CreatedAt = blogPost.CreatedAt,
                UpdatedAt = blogPost.UpdatedAt
            };
            return Ok(blogPostDto);
        }

        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<BlogPostDto>> GetBlogPostBySlug(string slug)
        {
            var blogPost = await _blogService.GetBlogPostBySlugAsync(slug);
            if (blogPost == null)
            {
                return NotFound();
            }
            var blogPostDto = new BlogPostDto
            {
                Id = blogPost.Id,
                Title = blogPost.Title,
                Slug = blogPost.Slug,
                Content = blogPost.Content,
                Excerpt = blogPost.Excerpt,
                Author = blogPost.Author,
                FeaturedImageUrl = blogPost.FeaturedImageUrl,
                Tags = blogPost.Tags,
                IsPublished = blogPost.IsPublished,
                PublishedAt = blogPost.PublishedAt,
                ReadTimeMinutes = blogPost.ReadTimeMinutes,
                ViewCount = blogPost.ViewCount,
                CreatedAt = blogPost.CreatedAt,
                UpdatedAt = blogPost.UpdatedAt
            };
            return Ok(blogPostDto);
        }

        [HttpGet("published")]
        public async Task<ActionResult<IEnumerable<BlogPostDto>>> GetPublishedBlogPosts()
        {
            var blogPosts = await _blogService.GetPublishedBlogPostsAsync();
            var blogPostDtos = blogPosts.Select(b => new BlogPostDto
            {
                Id = b.Id,
                Title = b.Title,
                Slug = b.Slug,
                Content = b.Content,
                Excerpt = b.Excerpt,
                Author = b.Author,
                FeaturedImageUrl = b.FeaturedImageUrl,
                Tags = b.Tags,
                IsPublished = b.IsPublished,
                PublishedAt = b.PublishedAt,
                ReadTimeMinutes = b.ReadTimeMinutes,
                ViewCount = b.ViewCount,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            });
            return Ok(blogPostDtos);
        }

        [HttpPost]
        public async Task<ActionResult<BlogPostDto>> CreateBlogPost(BlogPost blogPost)
        {
            var createdBlogPost = await _blogService.CreateBlogPostAsync(blogPost);
            var blogPostDto = new BlogPostDto
            {
                Id = createdBlogPost.Id,
                Title = createdBlogPost.Title,
                Slug = createdBlogPost.Slug,
                Content = createdBlogPost.Content,
                Excerpt = createdBlogPost.Excerpt,
                Author = createdBlogPost.Author,
                FeaturedImageUrl = createdBlogPost.FeaturedImageUrl,
                Tags = createdBlogPost.Tags,
                IsPublished = createdBlogPost.IsPublished,
                PublishedAt = createdBlogPost.PublishedAt,
                ReadTimeMinutes = createdBlogPost.ReadTimeMinutes,
                ViewCount = createdBlogPost.ViewCount,
                CreatedAt = createdBlogPost.CreatedAt,
                UpdatedAt = createdBlogPost.UpdatedAt
            };
            return CreatedAtAction(nameof(GetBlogPostById), new { id = blogPostDto.Id }, blogPostDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBlogPost(int id, BlogPost blogPost)
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
    }
}