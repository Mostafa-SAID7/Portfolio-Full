using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Data;

namespace PortfolioAPI.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PortfolioDbContext _context;
        private bool _disposed = false;

        private IProjectRepository? _projects;
        private IRepository<Skill>? _skills;
        private IRepository<Experience>? _experiences;
        private IRepository<Contact>? _contacts;
        private IRepository<Testimonial>? _testimonials;
        private IRepository<ProjectImage>? _projectImages;
        private IRepository<Credential>? _credentials;

        public UnitOfWork(PortfolioDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IProjectRepository Projects => _projects ??= new ProjectRepository(_context);
        public IRepository<Skill> Skills => _skills ??= new Repository<Skill>(_context);
        public IRepository<Experience> Experiences => _experiences ??= new Repository<Experience>(_context);
        public IRepository<Contact> Contacts => _contacts ??= new Repository<Contact>(_context);
        public IRepository<Testimonial> Testimonials => _testimonials ??= new Repository<Testimonial>(_context);
        public IRepository<ProjectImage> ProjectImages => _projectImages ??= new Repository<ProjectImage>(_context);
        public IRepository<Credential> Credentials => _credentials ??= new Repository<Credential>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            await _context.Database.CommitTransactionAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            await _context.Database.RollbackTransactionAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}