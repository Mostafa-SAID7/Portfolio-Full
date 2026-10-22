using System.Threading.Tasks;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Core.Interfaces
{
    /// <summary>
    /// Aggregates the repositories sharing one scoped DbContext and owns the
    /// single <c>SaveChangesAsync</c> call that commits an application
    /// operation. The container owns the DbContext lifetime (no Dispose on
    /// this interface); there is intentionally no transaction API — every
    /// service operation is a single aggregate write committed atomically by
    /// one SaveChanges call.
    /// </summary>
    public interface IUnitOfWork
    {
        IProjectRepository Projects { get; }
        IRepository<Skill> Skills { get; }
        IRepository<Experience> Experiences { get; }
        IRepository<Contact> Contacts { get; }
        IRepository<ContactReply> ContactReplies { get; }
        IRepository<Testimonial> Testimonials { get; }
        IRepository<Credential> Credentials { get; }
        IRepository<Category> Categories { get; }

        Task<int> SaveChangesAsync();
    }
}
