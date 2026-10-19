using FluentAssertions;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    /// <summary>
    /// Covers EntityMappings — the single mapping owner shared by the admin
    /// controllers, PublicController and ProjectService (replaces the old
    /// AutoMapperTests: AutoMapper was registered but never invoked anywhere;
    /// the hand-written mappers duplicated each other seven times).
    /// </summary>
    public class DtoMappingsTests
    {
        [Fact]
        public void Project_ToDto_MapsEveryField_IncludingMetrics()
        {
            var project = new Project
            {
                Id = 3,
                Title = "Title",
                Description = "Desc",
                ImageUrl = "/api/files/cover.png",
                Technologies = new List<string> { ".NET 9", "React" },
                GithubUrl = "https://github.com/x/y",
                LiveUrl = "https://example.com",
                Featured = true,
                Order = 4,
                IsActive = false,
                Category = "Web",
                Client = "Acme",
                Type = "Freelance",
                Database = "SQL Server",
                Status = "Completed",
                Badges = new List<string> { "New" },
                Metrics = new List<ProjectMetric> { new() { Label = "Users", Value = "10k" } },
                Gradient = "from-blue-500"
            };

            var dto = EntityMappings.ToDto(project);

            dto.Id.Should().Be(3);
            dto.Title.Should().Be("Title");
            dto.Technologies.Should().Equal(".NET 9", "React");
            dto.GithubUrl.Should().Be("https://github.com/x/y");
            dto.LiveUrl.Should().Be("https://example.com");
            dto.Featured.Should().BeTrue();
            dto.Order.Should().Be(4);
            dto.IsActive.Should().BeFalse();
            dto.Category.Should().Be("Web");
            dto.Client.Should().Be("Acme");
            dto.Type.Should().Be("Freelance");
            dto.Database.Should().Be("SQL Server");
            dto.Status.Should().Be("Completed");
            dto.Badges.Should().Equal("New");
            dto.Metrics.Should().ContainSingle();
            dto.Metrics[0].Label.Should().Be("Users");
            dto.Metrics[0].Value.Should().Be("10k");
            dto.Gradient.Should().Be("from-blue-500");
        }

        [Fact]
        public void Project_ToDto_MaterializesNullLists_AsEmpty()
        {
            var project = new Project { Title = "T", Description = "D", Technologies = null!, Badges = null!, Metrics = null! };

            var dto = EntityMappings.ToDto(project);

            dto.Technologies.Should().NotBeNull().And.BeEmpty();
            dto.Badges.Should().NotBeNull().And.BeEmpty();
            dto.Metrics.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void Skill_ToDto_MapsEveryField()
        {
            var skill = new Skill { Id = 1, Name = "C#", Category = "Languages", Proficiency = 90, IconUrl = "i", Description = "d", Order = 2, IsActive = false };

            var dto = EntityMappings.ToDto(skill);

            dto.Id.Should().Be(1);
            dto.Name.Should().Be("C#");
            dto.Category.Should().Be("Languages");
            dto.Proficiency.Should().Be(90);
            dto.IconUrl.Should().Be("i");
            dto.Description.Should().Be("d");
            dto.Order.Should().Be(2);
            dto.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Experience_ToDto_MapsTechnologies_AndNullListBecomesEmpty()
        {
            var experience = new Experience
            {
                Id = 2,
                Company = "Acme",
                Position = "Dev",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2021, 1, 1),
                Description = "Work",
                Technologies = new List<string> { "C#" },
                Location = "Remote",
                Current = false,
                Order = 1,
                IsActive = true
            };

            var dto = EntityMappings.ToDto(experience);

            dto.Company.Should().Be("Acme");
            dto.Technologies.Should().Equal("C#");
            dto.Current.Should().BeFalse();

            experience.Technologies = null!;
            EntityMappings.ToDto(experience).Technologies.Should().BeEmpty();
        }

        [Fact]
        public void Contact_ToDto_MapsInboxFields()
        {
            var contact = new Contact
            {
                Id = 9,
                Name = "Jane",
                Email = "jane@x.com",
                Subject = "Hi",
                Message = "Hello",
                IsRead = true,
                ReadAt = DateTime.UtcNow,
                IpAddress = "1.2.3.4",
                UserAgent = "UA"
            };

            var dto = EntityMappings.ToDto(contact);

            dto.Id.Should().Be(9);
            dto.IsRead.Should().BeTrue();
            dto.ReadAt.Should().NotBeNull();
            dto.IpAddress.Should().Be("1.2.3.4");
            dto.UserAgent.Should().Be("UA");
        }

        [Fact]
        public void Testimonial_ToDto_MapsEveryField()
        {
            var testimonial = new Testimonial
            {
                Id = 5,
                Name = "John",
                Position = "CTO",
                Company = "Acme",
                Content = "Great",
                ImageUrl = "/i.png",
                Rating = 4,
                Featured = true,
                Order = 2,
                IsActive = false,
                ProjectUrl = "https://example.com"
            };

            var dto = EntityMappings.ToDto(testimonial);

            dto.Rating.Should().Be(4);
            dto.Featured.Should().BeTrue();
            dto.IsActive.Should().BeFalse();
            dto.ProjectUrl.Should().Be("https://example.com");
        }

        [Fact]
        public void Credential_ToDto_MaterializesNullHighlightLists_AsEmpty()
        {
            var credential = new Credential
            {
                Id = 6,
                Tab = "work",
                Year = "2024",
                TitleEn = "Dev",
                TitleAr = "مطور",
                HighlightsEn = null!,
                HighlightsAr = null!,
                Stack = null!
            };

            var dto = EntityMappings.ToDto(credential);

            dto.TitleEn.Should().Be("Dev");
            dto.HighlightsEn.Should().BeEmpty();
            dto.HighlightsAr.Should().BeEmpty();
            dto.Stack.Should().BeEmpty();
        }

        [Fact]
        public void ToEntity_Skill_TrimsInput_AndRouteIdWins()
        {
            var dto = new CreateSkillDto { Name = "  C#  ", Category = " Lang ", Proficiency = 50, Order = 3, IsActive = false };

            var entity = EntityMappings.ToEntity(dto, id: 42);

            entity.Id.Should().Be(42); // route id, never the payload's
            entity.Name.Should().Be("C#");
            entity.Category.Should().Be("Lang");
            entity.Proficiency.Should().Be(50);
            entity.IsActive.Should().BeFalse();
        }

        [Fact]
        public void ToEntity_Experience_MapsAllEditableFields()
        {
            var dto = new CreateExperienceDto
            {
                Company = "Acme",
                Position = "Dev",
                StartDate = new DateTime(2020, 1, 1),
                Description = "Work",
                Technologies = new List<string> { "C#" },
                Current = true
            };

            var entity = EntityMappings.ToEntity(dto, id: 7);

            entity.Id.Should().Be(7);
            entity.Company.Should().Be("Acme");
            entity.Current.Should().BeTrue();
            entity.Technologies.Should().Equal("C#");
        }

        [Fact]
        public void ToEntity_Credential_MapsBilingualFields_AndListDefaults()
        {
            var dto = new CreateCredentialDto
            {
                Tab = "education",
                Year = "2023",
                TitleEn = "BSc",
                TitleAr = "بكالوريوس",
                HighlightsEn = new List<string> { "Honors" }
            };

            var entity = EntityMappings.ToEntity(dto, id: 11);

            entity.Id.Should().Be(11);
            entity.Tab.Should().Be("education");
            entity.TitleAr.Should().Be("بكالوريوس");
            entity.HighlightsEn.Should().Equal("Honors");
            entity.HighlightsAr.Should().NotBeNull().And.BeEmpty();
            entity.Stack.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void ToEntity_Testimonial_MapsFields_AndNullTrim()
        {
            var dto = new CreateTestimonialDto { Name = "  John  ", Content = " Nice ", Rating = 5 };

            var entity = EntityMappings.ToEntity(dto, id: 3);

            entity.Id.Should().Be(3);
            entity.Name.Should().Be("John");
            entity.Content.Should().Be("Nice");
            entity.Rating.Should().Be(5);
        }

        [Fact]
        public void ContactFrom_TrimsWithNulls_AndClampsRequestMetadata()
        {
            var dto = new CreateContactDto { Name = "  Jane ", Email = " jane@x.com ", Subject = " Hi ", Message = "Hello" };

            var contact = Contact.From(dto, ipAddress: "1.2.3.4", userAgent: new string('u', 600));

            contact.Name.Should().Be("Jane");
            contact.Email.Should().Be("jane@x.com");
            contact.IpAddress.Should().Be("1.2.3.4");
            contact.UserAgent!.Length.Should().Be(500); // clamped to column size
        }

        [Fact]
        public void ContactFrom_TreatsNullJsonStrings_AsEmpty_ForValidation()
        {
            var dto = new CreateContactDto { Name = null!, Email = null!, Subject = null!, Message = null! };

            var contact = Contact.From(dto, null, null);

            // Empty (not null, not an NRE) → ContactValidator produces the 400.
            contact.Name.Should().BeEmpty();
            contact.Email.Should().BeEmpty();
            contact.Subject.Should().BeEmpty();
            contact.Message.Should().BeEmpty();
        }

        [Fact]
        public void Contact_MarkRead_SetsFlagAndTimestamp()
        {
            var contact = new Contact { IsRead = false, ReadAt = null };

            contact.MarkRead();

            contact.IsRead.Should().BeTrue();
            contact.ReadAt.Should().NotBeNull();
        }
    }
}
