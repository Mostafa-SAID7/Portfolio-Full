# Backend Architecture & Directory Structure Reference

An authoritative engineering reference documenting the Clean Architecture implementation of the **PortfolioAPI** backend.

---

## 1. Architectural Layers & Separation of Concerns

```
PortfolioAPI/
├── src/
│   ├── PortfolioAPI/               # Presentation & Web API Host
│   ├── PortfolioAPI.Core/          # Domain Layer (Pure Domain)
│   ├── PortfolioAPI.Application/   # Use Cases & Application Logic
│   └── PortfolioAPI.Infrastructure/# Data Persistence & External Adapters
└── tests/
    ├── PortfolioAPI.UnitTests/     # Unit test suite (xUnit, Moq, FluentAssertions)
    └── PortfolioAPI.IntegrationTests/ # Integration tests (MsSql Testcontainers)
```

### Layer Dependency Rules

```
PortfolioAPI (Host)
  ├── references ──> PortfolioAPI.Application
  ├── references ──> PortfolioAPI.Infrastructure
  └── references ──> PortfolioAPI.Core

PortfolioAPI.Infrastructure
  ├── references ──> PortfolioAPI.Application
  └── references ──> PortfolioAPI.Core

PortfolioAPI.Application
  └── references ──> PortfolioAPI.Core

PortfolioAPI.Core
  └── (Zero project references — completely independent)
```

---

## 2. Comprehensive Directory Tree

```
PortfolioAPI/src/
├── PortfolioAPI/
│   ├── Auth/
│   │   ├── ITokenService.cs
│   │   └── TokenService.cs
│   ├── Commands/
│   │   └── MediaMigrationCommand.cs
│   ├── Configuration/
│   │   ├── AnalyticsConfiguration.cs
│   │   ├── ApiDefaultsConfiguration.cs
│   │   ├── ApplicationServicesConfiguration.cs
│   │   ├── CorsConfiguration.cs
│   │   ├── DatabaseConfiguration.cs
│   │   ├── JwtAuthenticationConfiguration.cs
│   │   ├── LoggingConfiguration.cs
│   │   ├── ProxyConfiguration.cs
│   │   ├── RateLimitingConfiguration.cs
│   │   ├── StorageConfiguration.cs
│   │   └── SwaggerConfiguration.cs
│   ├── Controllers/
│   │   ├── AnalyticsController.cs
│   │   ├── AuthController.cs
│   │   ├── CategoriesController.cs
│   │   ├── ContactController.cs
│   │   ├── CredentialsController.cs
│   │   ├── ExperienceController.cs
│   │   ├── LogsController.cs               # Production HTML log browser & API
│   │   ├── ProjectsController.cs
│   │   ├── PublicController.cs             # Public portfolio endpoints
│   │   ├── SkillsController.cs
│   │   ├── TestimonialsController.cs
│   │   ├── UploadController.cs
│   │   └── WebsitesController.cs
│   ├── Endpoints/
│   │   └── InfoEndpoints.cs
│   ├── Pipelines/
│   │   ├── CorrelationPipeline.cs
│   │   ├── ErrorHandlingPipeline.cs
│   │   ├── HeadersPipeline.cs
│   │   └── UploadsPipeline.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── web.config
│
├── PortfolioAPI.Core/
│   ├── Common/
│   │   ├── Error.cs
│   │   └── Result.cs
│   ├── Entities/
│   │   ├── BaseEntity.cs
│   │   ├── Category.cs
│   │   ├── Contact.cs
│   │   ├── ContactReply.cs
│   │   ├── Credential.cs
│   │   ├── Experience.cs
│   │   ├── Project.cs
│   │   ├── ProjectCategory.cs
│   │   ├── ProjectWebsite.cs
│   │   ├── Skill.cs
│   │   ├── Testimonial.cs
│   │   └── Website.cs
│   ├── Exceptions/
│   │   ├── EmailDeliveryException.cs
│   │   └── StorageExceptions.cs
│   └── Interfaces/
│       ├── IEmailService.cs
│       ├── IFileStorageService.cs
│       ├── IProjectRepository.cs
│       ├── IRepository.cs
│       ├── IUnitOfWork.cs
│       └── MediaContracts.cs
│
├── PortfolioAPI.Application/
│   ├── DTOs/                               # 1 class per file (Single Responsibility)
│   │   ├── Analytics/
│   │   │   ├── AnalyticsDeviceBreakdownDto.cs
│   │   │   ├── AnalyticsGeoBreakdownDto.cs
│   │   │   ├── AnalyticsOverviewDto.cs
│   │   │   ├── AnalyticsPageMetricDto.cs
│   │   │   └── AnalyticsTimeSeriesPointDto.cs
│   │   ├── Categories/
│   │   │   ├── CategoryDto.cs
│   │   │   ├── CreateCategoryDto.cs
│   │   │   └── UpdateCategoryDto.cs
│   │   ├── Contacts/
│   │   │   ├── ContactDto.cs
│   │   │   ├── ContactReplyDto.cs
│   │   │   ├── CreateContactDto.cs
│   │   │   └── ReplyToContactDto.cs
│   │   ├── Credentials/
│   │   │   ├── CredentialDto.cs
│   │   │   ├── CreateCredentialDto.cs
│   │   │   └── UpdateCredentialDto.cs
│   │   ├── Experiences/
│   │   │   ├── ExperienceDto.cs
│   │   │   ├── CreateExperienceDto.cs
│   │   │   └── UpdateExperienceDto.cs
│   │   ├── Projects/
│   │   │   ├── ProjectDto.cs
│   │   │   ├── ProjectDetailDto.cs
│   │   │   ├── CreateProjectDto.cs
│   │   │   └── UpdateProjectDto.cs
│   │   ├── Skills/
│   │   │   ├── SkillDto.cs
│   │   │   ├── CreateSkillDto.cs
│   │   │   └── UpdateSkillDto.cs
│   │   ├── Testimonials/
│   │   │   ├── TestimonialDto.cs
│   │   │   ├── CreateTestimonialDto.cs
│   │   │   └── UpdateTestimonialDto.cs
│   │   └── Websites/
│   │       ├── WebsiteDto.cs
│   │       ├── CreateWebsiteDto.cs
│   │       └── UpdateWebsiteDto.cs
│   ├── Exceptions/
│   │   ├── NotFoundException.cs
│   │   └── ValidationException.cs
│   ├── Interfaces/
│   │   ├── IAnalyticsReportingService.cs
│   │   ├── ICategoryService.cs
│   │   ├── IContactService.cs
│   │   ├── ICredentialService.cs
│   │   ├── IExperienceService.cs
│   │   ├── IProjectService.cs
│   │   ├── ISkillService.cs
│   │   ├── ITestimonialService.cs
│   │   └── IWebsiteService.cs
│   ├── Mappings/                           # Clean partial mappings per aggregate
│   │   ├── EntityMappings.cs
│   │   ├── CategoryMappings.cs
│   │   ├── ContactMappings.cs
│   │   ├── CredentialMappings.cs
│   │   ├── ExperienceMappings.cs
│   │   ├── ProjectMappings.cs
│   │   ├── SkillMappings.cs
│   │   ├── TestimonialMappings.cs
│   │   └── WebsiteMappings.cs
│   ├── Services/
│   │   ├── CrudService.cs
│   │   ├── CategoryService.cs
│   │   ├── ContactService.cs
│   │   ├── CredentialService.cs
│   │   ├── ExperienceService.cs
│   │   ├── ProjectService.cs
│   │   ├── SkillService.cs
│   │   ├── TestimonialService.cs
│   │   └── WebsiteService.cs
│   └── Validators/
│       ├── CategoryValidator.cs
│       ├── ContactReplyValidator.cs
│       ├── ContactValidator.cs
│       ├── CredentialValidator.cs
│       ├── ExperienceValidator.cs
│       ├── ProjectValidator.cs
│       ├── SkillValidator.cs
│       ├── TestimonialValidator.cs
│       └── WebsiteValidator.cs
│
└── PortfolioAPI.Infrastructure/
    ├── Data/
    │   ├── Configurations/                 # EF Core entity type configurations
    │   │   ├── CategoryConfiguration.cs
    │   │   ├── ContactConfiguration.cs
    │   │   ├── CredentialConfiguration.cs
    │   │   ├── ExperienceConfiguration.cs
    │   │   ├── ProjectCategoryConfiguration.cs
    │   │   ├── ProjectConfiguration.cs
    │   │   ├── ProjectWebsiteConfiguration.cs
    │   │   ├── SkillConfiguration.cs
    │   │   ├── TestimonialConfiguration.cs
    │   │   └── WebsiteConfiguration.cs
    │   ├── DbInitializer.cs
    │   └── PortfolioDbContext.cs
    ├── Emailing/
    │   ├── EmailService.cs
    │   ├── EmailSettingsOptions.cs
    │   └── Templates/
    ├── Migrations/                         # Incremental EF Core migrations
    ├── Repositories/
    │   ├── ProjectRepository.cs
    │   ├── Repository.cs
    │   └── UnitOfWork.cs
    ├── Services/
    │   └── AnalyticsReportingService.cs    # Google Analytics 4 API client
    └── Storage/
        ├── CloudinaryStorageService.cs
        ├── FileStorageOptions.cs
        ├── ImageValidator.cs
        └── LocalFileStorageService.cs
```

---

## 3. Key Design Patterns

1. **Clean Architecture**: Strong inward dependency flow protecting Core business entities from framework changes.
2. **Single Responsibility Principle (SRP)**: Each DTO and validator is isolated in its own file under a domain feature folder.
3. **Partial Class Mappings**: Pure C# static mapping extensions without heavy reflection or runtime overhead (replaces runtime AutoMapper).
4. **Repository & Unit of Work**: Encapsulates EF Core operations behind `IRepository<T>` and `IUnitOfWork` for transactional consistency.
5. **FluentValidation**: Explicit validation rules executed automatically inside application service pipelines.
6. **Result Pattern**: Uniform typed error handling (`Result<T>`) alongside domain exception handling.
7. **Serilog Enterprise Logging**: JSON and human-readable structured logs with correlation identifiers and dedicated live browser UI.
