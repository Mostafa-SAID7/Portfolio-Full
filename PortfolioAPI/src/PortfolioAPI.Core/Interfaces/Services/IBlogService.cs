using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Core.Interfaces.Services
{
    public interface IBlogService
    {
        Task<IEnumerable<BlogPost>> GetAllBlogPostsAsync();
        Task<BlogPost?> GetBlogPostByIdAsync(int id);
        Task<BlogPost?> GetBlogPostBySlugAsync(string slug);
        Task<IEnumerable<BlogPost>> GetPublishedBlogPostsAsync();
        Task<BlogPost> CreateBlogPostAsync(BlogPost blogPost);
        Task UpdateBlogPostAsync(BlogPost blogPost);
        Task DeleteBlogPostAsync(int id);
        Task IncrementViewCountAsync(int id);
    }
}