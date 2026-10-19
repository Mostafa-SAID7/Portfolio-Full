using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Data;

namespace PortfolioAPI.Infrastructure.Repositories
{
    /// <summary>
    /// Aggregates the repositories sharing one scoped DbContext behind a single
    /// SaveChanges call. The container owns the DbContext lifetime, so there is
    /// no Dispose surface here. There is deliberately no transaction API: every
    /// service operation is a single aggregate write committed by one
    /// SaveChanges (which is atomic), and user-initiated transactions are
    /// unsupported anyway while the context runs with EnableRetryOnFailure.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PortfolioDbContext _context;

        private IProjectRepository? _projects;
        private IRepository<Skill>? _skills;
        private IRepository<Experience>? _experiences;
        private IRepository<Contact>? _contacts;
        private IRepository<Testimonial>? _testimonials;
        private IRepository<Credential>? _credentials;

        public UnitOfWork(PortfolioDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Repositories are created lazily over the SAME context (one unit of
        // work per request); unit tests configure these properties before the
        // service performs its first call.
        public IProjectRepository Projects => _projects ??= new ProjectRepository(_context);
        public IRepository<Skill> Skills => _skills ??= new Repository<Skill>(_context);
        public IRepository<Experience> Experiences => _experiences ??= new Repository<Experience>(_context);
        public IRepository<Contact> Contacts => _contacts ??= new Repository<Contact>(_context);
        public IRepository<Testimonial> Testimonials => _testimonials ??= new Repository<Testimonial>(_context);
        public IRepository<Credential> Credentials => _credentials ??= new Repository<Credential>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
