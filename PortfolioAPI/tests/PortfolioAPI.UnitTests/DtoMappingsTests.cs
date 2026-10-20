using FluentAssertions;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
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
                TitleEn = "Title",
                TitleAr = "عنوان",
                DescriptionEn = "Desc",
                DescriptionAr = "وصف",
                ImageUrl = "/api/files/cover.png",
                Technologies = new List<string> { ".NET 9", "React" },
                GithubUrl = "https://github.com/x/y",
                LiveUrl = "https://example.com",
                Featured = true,
                Order = 4,
                IsActive = false,
                CategoryEn = "Web",
                CategoryAr = "ويب",
                ClientEn = "Acme",
                ClientAr = "أكمي",
                TypeEn = "Freelance",
                TypeAr = "عمل حر",
                DatabaseEn = "SQL Server",
                DatabaseAr = "سيرفر SQL",
                StatusEn = "Completed",
                StatusAr = "مكتمل",
                BadgesEn = new List<string> { "New" },
                BadgesAr = new List<string> { "جديد" },
                MetricsEn = new List<ProjectMetric> { new() { LabelEn = "Users", LabelAr = "مستخدمون", Value = "10k" } },
                MetricsAr = new List<ProjectMetric> { new() { LabelEn = "Users", LabelAr = "مستخدمون", Value = "10k" } },
                Gradient = "from-blue-500"
            };

            var dto = EntityMappings.ToDto(project);

            dto.Id.Should().Be(3);
            dto.TitleEn.Should().Be("Title");
            dto.TitleAr.Should().Be("عنوان");
            dto.Technologies.Should().Equal(".NET 9", "React");
            dto.GithubUrl.Should().Be("https://github.com/x/y");
            dto.LiveUrl.Should().Be("https://example.com");
            dto.Featured.Should().BeTrue();
            dto.Order.Should().Be(4);
            dto.IsActive.Should().BeFalse();
            dto.CategoryEn.Should().Be("Web");
            dto.CategoryAr.Should().Be("ويب");
            dto.ClientEn.Should().Be("Acme");
            dto.ClientAr.Should().Be("أكمي");
            dto.TypeEn.Should().Be("Freelance");
            dto.TypeAr.Should().Be("عمل حر");
            dto.DatabaseEn.Should().Be("SQL Server");
            dto.DatabaseAr.Should().Be("سيرفر SQL");
            dto.StatusEn.Should().Be("Completed");
            dto.StatusAr.Should().Be("مكتمل");
            dto.BadgesEn.Should().Equal("New");
            dto.BadgesAr.Should().Equal("جديد");
            dto.MetricsEn.Should().ContainSingle(m => m.LabelEn == "Users");
            dto.MetricsAr.Should().ContainSingle(m => m.LabelAr == "مستخدمون");
            dto.MetricsEn[0].Value.Should().Be("10k");
            dto.Gradient.Should().Be("from-blue-500");
        }

        [Fact]
        public void ToEntity_Skill_TrimsInput_AndRouteIdWins()
        {
            var dto = new CreateSkillDto { NameEn = "  C#  ", NameAr = "  سي شارب  ", CategoryEn = " Lang ", CategoryAr = " لغات ", Proficiency = 50, Order = 3, IsActive = false };

            var entity = EntityMappings.ToEntity(dto, id: 42);

            entity.Id.Should().Be(42); // route id, never the payload's
            entity.NameEn.Should().Be("C#");
            entity.NameAr.Should().Be("سي شارب");
            entity.CategoryEn.Should().Be("Lang");
            entity.CategoryAr.Should().Be("لغات");
            entity.Proficiency.Should().Be(50);
            entity.IsActive.Should().BeFalse();
        }

        [Fact]
        public void ToEntity_Experience_MapsAllEditableFields()
        {
            var dto = new CreateExperienceDto
            {
                CompanyEn = "Acme",
                CompanyAr = "أكمي",
                PositionEn = "Dev",
                PositionAr = "مطور",
                StartDate = new DateTime(2020, 1, 1),
                DescriptionEn = "Work",
                DescriptionAr = "عمل",
                Technologies = new List<string> { "C#" },
                Current = true
            };

            var entity = EntityMappings.ToEntity(dto, id: 7);

            entity.Id.Should().Be(7); // route id, never the payload's
            entity.CompanyEn.Should().Be("Acme");
            entity.CompanyAr.Should().Be("أكمي");
            entity.PositionEn.Should().Be("Dev");
            entity.PositionAr.Should().Be("مطور");
            entity.Order.Should().Be(0); // default
        }

        [Fact]
        public void ToEntity_Project_TrimsInput_AndRouteIdWins()
        {
            var dto = new CreateProjectDto { TitleEn = "  C#  ", TitleAr = "  سي شارب  ", DescriptionEn = "  Desc  ", DescriptionAr = "  وصف  ", CategoryEn = " Lang ", CategoryAr = " لغات ", Order = 3, IsActive = false };

            var entity = EntityMappings.ToEntity(dto, id: 42);

            entity.Id.Should().Be(42); // route id, never the payload's
            entity.TitleEn.Should().Be("C#");
            entity.TitleAr.Should().Be("سي شارب");
            entity.DescriptionEn.Should().Be("Desc");
            entity.DescriptionAr.Should().Be("وصف");
            entity.CategoryEn.Should().Be("Lang");
            entity.CategoryAr.Should().Be("لغات");
            entity.IsActive.Should().BeFalse();
        }
    }
}