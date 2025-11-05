using System;
using System.Threading.Tasks;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Project> Projects { get; }
        IRepository<Skill> Skills { get; }
        IRepository<Experience> Experiences { get; }
        IRepository<BlogPost> BlogPosts { get; }
        IRepository<Contact> Contacts { get; }
        IRepository<Testimonial> Testimonials { get; }
        IRepository<ProjectImage> ProjectImages { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}