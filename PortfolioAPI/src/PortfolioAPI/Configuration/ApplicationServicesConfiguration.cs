using FluentValidation;
using PortfolioAPI.Application.Services;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using PortfolioAPI.Infrastructure.Emailing;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.Configuration
{
    /// <summary>
    /// Domain + application service registrations — extracted from Program.cs.
    /// </summary>
    public static class ApplicationServicesConfiguration
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Email SMTP settings — strongly typed (same pattern as storage).
            services.Configure<EmailSettingsOptions>(
                configuration.GetSection(Infrastructure.Emailing.EmailSettingsOptions.SectionName));

            // Add services
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<ISkillService, SkillService>();
            services.AddScoped<IExperienceService, ExperienceService>();
            services.AddScoped<IContactService, ContactService>();
            services.AddScoped<ITestimonialService, TestimonialService>();

            services.AddScoped<ICredentialService, CredentialService>();
            services.AddScoped<ICategoryService, CategoryService>();

            // Add infrastructure services
            services.AddScoped<IEmailService, EmailService>();

            // Add FluentValidation
            services.AddValidatorsFromAssembly(typeof(ProjectService).Assembly);

            return services;
        }
    }
}
