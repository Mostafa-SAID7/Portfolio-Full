using FluentValidation;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Application.Services
{
    public class BlogService : IBlogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<BlogPost> _validator;

        public BlogService(IUnitOfWork unitOfWork, IValidator<BlogPost> validator)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<IEnumerable<BlogPost>> GetAllBlogPostsAsync()
        {
            return await _unitOfWork.BlogPosts.GetAllAsync();
        }

        public async Task<BlogPost?> GetBlogPostByIdAsync(int id)
        {
            var blogPost = await _unitOfWork.BlogPosts.GetByIdAsync(id);
            if (blogPost == null)
            {
                throw new NotFoundException($"Blog post with id {id} not found.");
            }
            return blogPost;
        }

        public async Task<BlogPost?> GetBlogPostBySlugAsync(string slug)
        {
            var blogPost = await _unitOfWork.BlogPosts.FindAsync(b => b.Slug == slug);
            return blogPost.FirstOrDefault();
        }

        public async Task<IEnumerable<BlogPost>> GetPublishedBlogPostsAsync()
        {
            return await _unitOfWork.BlogPosts.FindAsync(b => b.Published);
        }

        public async Task<BlogPost> CreateBlogPostAsync(BlogPost blogPost)
        {
            var validationResult = await _validator.ValidateAsync(blogPost);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            blogPost.CreatedAt = DateTime.UtcNow;
            var createdBlogPost = await _unitOfWork.BlogPosts.AddAsync(blogPost);
            await _unitOfWork.SaveChangesAsync();
            return createdBlogPost;
        }

        public async Task UpdateBlogPostAsync(BlogPost blogPost)
        {
            var existingBlogPost = await _unitOfWork.BlogPosts.GetByIdAsync(blogPost.Id);
            if (existingBlogPost == null)
            {
                throw new NotFoundException($"Blog post with id {blogPost.Id} not found.");
            }

            var validationResult = await _validator.ValidateAsync(blogPost);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            blogPost.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.BlogPosts.UpdateAsync(blogPost);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteBlogPostAsync(int id)
        {
            var blogPost = await _unitOfWork.BlogPosts.GetByIdAsync(id);
            if (blogPost == null)
            {
                throw new NotFoundException($"Blog post with id {id} not found.");
            }

            await _unitOfWork.BlogPosts.DeleteAsync(blogPost);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task IncrementViewCountAsync(int id)
        {
            var blogPost = await _unitOfWork.BlogPosts.GetByIdAsync(id);
            if (blogPost == null)
            {
                throw new NotFoundException($"Blog post with id {id} not found.");
            }

            blogPost.ViewCount++;
            await _unitOfWork.BlogPosts.UpdateAsync(blogPost);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}