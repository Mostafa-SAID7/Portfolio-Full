using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using PortfolioAPI.Application.Validators;
using PortfolioAPI.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class ValidatorsTests
    {
        private readonly ProjectValidator _projectValidator = new();
        private readonly SkillValidator _skillValidator = new();
        private readonly ExperienceValidator _experienceValidator = new();
        private readonly CategoryValidator _categoryValidator = new();
        private readonly ContactValidator _contactValidator = new();
        private readonly TestimonialValidator _testimonialValidator = new();
        private readonly CredentialValidator _credentialValidator = new();

        private ValidationResult Validate<T>(IValidator<T> validator, T instance) => validator.Validate(instance);

        #region ProjectValidator Tests

        [Fact]
        public void ProjectValidator_ShouldPass_WhenAllFieldsAreValid()
        {
            var project = new Project
            {
                TitleEn = "Valid Project Title",
                TitleAr = "عنوان مشروع صالح",
                DescriptionEn = "This is a valid project description that meets the length requirements.",
                DescriptionAr = "هذا وصف مشروع صالح يلبي متطلبات الطول.",
                ImageUrl = "/images/project.png",
                GithubUrl = "https://github.com/user/project",
                LiveUrl = "https://example.com/project",
                Technologies = new List<string> { "C#", ".NET", "React" },
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
                BadgesEn = new List<string> { "Featured" },
                BadgesAr = new List<string> { "مميز" },
                MetricsEn = new List<ProjectMetric> { new() { LabelEn = "Users", LabelAr = "مستخدمون", Value = "10k" } },
                MetricsAr = new List<ProjectMetric> { new() { LabelEn = "Users", LabelAr = "مستخدمون", Value = "10k" } },
                Gradient = "from-blue-500 to-purple-500"
            };

            var result = _projectValidator.Validate(project);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenTitleIsEmpty()
        {
            var project = new Project { TitleEn = "", TitleAr = "", DescriptionEn = "Valid description", DescriptionAr = "وصف صالح" };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "TitleEn" && e.ErrorMessage == "Title (en) is required.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenTitleExceedsMaxLength()
        {
            var project = new Project
            {
                TitleEn = new string('A', 101),
                TitleAr = new string('أ', 101),
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح"
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "TitleEn" && e.ErrorMessage == "Title (en) must not exceed 100 characters.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenDescriptionIsEmpty()
        {
            var project = new Project { TitleEn = "Valid Title", TitleAr = "عنوان صالح", DescriptionEn = "", DescriptionAr = "" };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "DescriptionEn" && e.ErrorMessage == "Description (en) is required.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenDescriptionExceedsMaxLength()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = new string('A', 501),
                DescriptionAr = new string('أ', 501)
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "DescriptionEn" && e.ErrorMessage == "Description (en) must not exceed 500 characters.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenLiveUrlIsInvalid()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                LiveUrl = "invalid-url"
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "LiveUrl" && e.ErrorMessage == "Live URL must be a valid URL.");
        }

        [Fact]
        public void ProjectValidator_ShouldPass_WhenLiveUrlIsNull()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                LiveUrl = null
            };

            var result = _projectValidator.Validate(project);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenGithubUrlIsInvalid()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                GithubUrl = "not a url"
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "GithubUrl" && e.ErrorMessage == "GitHub URL must be a valid URL.");
        }

        [Fact]
        public void ProjectValidator_ShouldPass_WhenGithubUrlIsNull()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                GithubUrl = null
            };

            var result = _projectValidator.Validate(project);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenImageUrlExceedsMaxLength()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                ImageUrl = new string('A', 501)
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "ImageUrl" && e.ErrorMessage == "Image URL must not exceed 500 characters.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenTechnologiesExceedMaxCount()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                Technologies = new List<string>(Enumerable.Range(1, 21).Select(i => $"Tech{i}"))
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "Technologies" && e.ErrorMessage == "A project can list at most 20 technologies.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenCategoryExceedsMaxLength()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                CategoryEn = new string('A', 101),
                CategoryAr = new string('أ', 101)
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "CategoryEn" && e.ErrorMessage == "Category (en) must not exceed 100 characters.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenClientExceedsMaxLength()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                ClientEn = new string('A', 101),
                ClientAr = new string('أ', 101)
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "ClientEn" && e.ErrorMessage == "Client (en) must not exceed 100 characters.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenTypeExceedsMaxLength()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                TypeEn = new string('A', 51),
                TypeAr = new string('أ', 51)
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "TypeEn" && e.ErrorMessage == "Type (en) must not exceed 50 characters.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenDatabaseExceedsMaxLength()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                DatabaseEn = new string('A', 101),
                DatabaseAr = new string('أ', 101)
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "DatabaseEn" && e.ErrorMessage == "Database (en) must not exceed 100 characters.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenStatusExceedsMaxLength()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                StatusEn = new string('A', 51),
                StatusAr = new string('أ', 51)
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "StatusEn" && e.ErrorMessage == "Status (en) must not exceed 50 characters.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenBadgesExceedMaxCount()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                BadgesEn = new List<string>(Enumerable.Range(1, 6).Select(i => $"Badge{i}")),
                BadgesAr = new List<string>(Enumerable.Range(1, 6).Select(i => $"شارة{i}"))
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "BadgesEn" && e.ErrorMessage == "A project can have at most 5 badges (en).");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenMetricsExceedMaxCount()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                MetricsEn = new List<ProjectMetric>(Enumerable.Range(1, 7).Select(i => new ProjectMetric { LabelEn = $"M{i}", LabelAr = $"م{i}", Value = "100" })),
                MetricsAr = new List<ProjectMetric>(Enumerable.Range(1, 7).Select(i => new ProjectMetric { LabelEn = $"M{i}", LabelAr = $"م{i}", Value = "100" }))
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "MetricsEn" && e.ErrorMessage == "A project can show at most 6 metrics (en).");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenGradientExceedsMaxLength()
        {
            var project = new Project
            {
                TitleEn = "Valid Title",
                TitleAr = "عنوان صالح",
                DescriptionEn = "Valid description",
                DescriptionAr = "وصف صالح",
                Gradient = new string('A', 201)
            };

            var result = _projectValidator.Validate(project);

            result.Errors.Should().Contain(e => e.PropertyName == "Gradient" && e.ErrorMessage == "Gradient must not exceed 200 characters.");
        }

        #endregion

        #region SkillValidator Tests

        [Fact]
        public void SkillValidator_ShouldPass_WhenAllFieldsAreValid()
        {
            var skill = new Skill
            {
                NameEn = "C#",
                NameAr = "سي شارب",
                CategoryEn = "Programming Languages",
                CategoryAr = "لغات البرمجة",
                Proficiency = 85,
                IconUrl = "https://example.com/csharp.png",
                DescriptionEn = "Advanced C# development",
                DescriptionAr = "تطوير C# متقدم"
            };

            var result = _skillValidator.Validate(skill);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void SkillValidator_ShouldFail_WhenNameIsEmpty()
        {
            var skill = new Skill { NameEn = "", NameAr = "", CategoryEn = "Programming", CategoryAr = "برمجة", Proficiency = 50 };

            var result = _skillValidator.Validate(skill);

            result.Errors.Should().Contain(e => e.PropertyName == "NameEn" && e.ErrorMessage == "Skill name (en) is required.");
        }

        [Fact]
        public void SkillValidator_ShouldFail_WhenCategoryIsEmpty()
        {
            var skill = new Skill { NameEn = "C#", NameAr = "سي شارب", CategoryEn = "", CategoryAr = "", Proficiency = 50 };

            var result = _skillValidator.Validate(skill);

            result.Errors.Should().Contain(e => e.PropertyName == "CategoryEn" && e.ErrorMessage == "Category (en) is required.");
        }

        [Fact]
        public void SkillValidator_ShouldFail_WhenNameExceedsMaxLength()
        {
            var skill = new Skill
            {
                NameEn = new string('A', 51),
                NameAr = new string('أ', 51),
                CategoryEn = "Programming",
                CategoryAr = "برمجة",
                Proficiency = 50
            };

            var result = _skillValidator.Validate(skill);

            result.Errors.Should().Contain(e => e.PropertyName == "NameEn" && e.ErrorMessage == "Skill name (en) must not exceed 50 characters.");
        }

        [Fact]
        public void SkillValidator_ShouldFail_WhenCategoryExceedsMaxLength()
        {
            var skill = new Skill
            {
                NameEn = "C#",
                NameAr = "سي شارب",
                CategoryEn = new string('A', 101),
                CategoryAr = new string('أ', 101),
                Proficiency = 50
            };

            var result = _skillValidator.Validate(skill);

            result.Errors.Should().Contain(e => e.PropertyName == "CategoryEn" && e.ErrorMessage == "Category (en) must not exceed 100 characters.");
        }

        [Fact]
        public void SkillValidator_ShouldFail_WhenProficiencyIsOutOfRange()
        {
            var skill = new Skill
            {
                NameEn = "C#",
                NameAr = "سي شارب",
                CategoryEn = "Programming",
                CategoryAr = "برمجة",
                Proficiency = -1
            };

            var result = _skillValidator.Validate(skill);

            result.Errors.Should().Contain(e => e.PropertyName == "Proficiency" && e.ErrorMessage == "Proficiency must be between 0 and 100.");
        }

        [Fact]
        public void SkillValidator_ShouldFail_WhenIconUrlExceedsMaxLength()
        {
            var skill = new Skill
            {
                NameEn = "C#",
                NameAr = "سي شارب",
                CategoryEn = "Programming",
                CategoryAr = "برمجة",
                Proficiency = 50,
                IconUrl = new string('A', 501)
            };

            var result = _skillValidator.Validate(skill);

            result.Errors.Should().Contain(e => e.PropertyName == "IconUrl" && e.ErrorMessage == "Icon URL must not exceed 500 characters.");
        }

        [Fact]
        public void SkillValidator_ShouldFail_WhenDescriptionExceedsMaxLength()
        {
            var skill = new Skill
            {
                NameEn = "C#",
                NameAr = "سي شارب",
                CategoryEn = "Programming",
                CategoryAr = "برمجة",
                Proficiency = 50,
                DescriptionEn = new string('A', 501),
                DescriptionAr = new string('أ', 501)
            };

            var result = _skillValidator.Validate(skill);

            result.Errors.Should().Contain(e => e.PropertyName == "DescriptionEn" && e.ErrorMessage == "Description (en) must not exceed 500 characters.");
        }

        #endregion

        #region ExperienceValidator Tests

        [Fact]
        public void ExperienceValidator_ShouldPass_WhenAllFieldsAreValid()
        {
            var experience = new Experience
            {
                CompanyEn = "Acme Corp",
                CompanyAr = "أكمي كورب",
                PositionEn = "Software Engineer",
                PositionAr = "مهندس برمجيات",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2022, 12, 31),
                DescriptionEn = "Built scalable web applications",
                DescriptionAr = "بناء تطبيقات ويب قابلة للتوسع",
                Technologies = new List<string> { "C#", ".NET" },
                LocationEn = "Remote",
                LocationAr = "عن بعد",
                Current = false,
                Order = 90
            };

            var result = _experienceValidator.Validate(experience);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ExperienceValidator_ShouldFail_WhenCompanyIsEmpty()
        {
            var experience = new Experience { CompanyEn = "", CompanyAr = "", PositionEn = "Dev", PositionAr = "مطور", StartDate = DateTime.Now };

            var result = _experienceValidator.Validate(experience);

            result.Errors.Should().Contain(e => e.PropertyName == "CompanyEn" && e.ErrorMessage == "Company (en) is required.");
        }

        [Fact]
        public void ExperienceValidator_ShouldFail_WhenPositionIsEmpty()
        {
            var experience = new Experience { CompanyEn = "Acme", CompanyAr = "أكمي", PositionEn = "", PositionAr = "مطور", StartDate = DateTime.Now };

            var result = _experienceValidator.Validate(experience);

            result.Errors.Should().Contain(e => e.PropertyName == "PositionEn" && e.ErrorMessage == "Position (en) is required.");
        }

        [Fact]
        public void ExperienceValidator_ShouldFail_WhenDescriptionIsEmpty()
        {
            var experience = new Experience { CompanyEn = "Acme", CompanyAr = "أكمي", PositionEn = "Dev", PositionAr = "مطور", StartDate = DateTime.Now, DescriptionEn = "", DescriptionAr = "" };

            var result = _experienceValidator.Validate(experience);

            result.Errors.Should().Contain(e => e.PropertyName == "DescriptionEn" && e.ErrorMessage == "Description (en) is required.");
        }

        [Fact]
        public void ExperienceValidator_ShouldFail_WhenStartDateIsDefault()
        {
            var experience = new Experience
            {
                CompanyEn = "Acme",
                CompanyAr = "أكمي",
                PositionEn = "Dev",
                PositionAr = "مطور",
                StartDate = default,
                EndDate = DateTime.Now.AddYears(1)
            };

            var result = _experienceValidator.Validate(experience);

            result.Errors.Should().Contain(e => e.PropertyName == "StartDate" && e.ErrorMessage == "Start date is required.");
        }

        [Fact]
        public void ExperienceValidator_ShouldFail_WhenEndDateBeforeStartDate()
        {
            var experience = new Experience
            {
                CompanyEn = "Acme",
                CompanyAr = "أكمي",
                PositionEn = "Dev",
                PositionAr = "مطور",
                StartDate = new DateTime(2022, 1, 1),
                EndDate = new DateTime(2020, 1, 1)
            };

            var result = _experienceValidator.Validate(experience);

            result.Errors.Should().Contain(e => e.PropertyName == "EndDate" && e.ErrorMessage == "End date must be after the start date.");
        }

        [Fact]
        public void ExperienceValidator_ShouldFail_WhenCurrentRoleHasEndDate()
        {
            var experience = new Experience
            {
                CompanyEn = "Acme",
                CompanyAr = "أكمي",
                PositionEn = "Dev",
                PositionAr = "مطور",
                StartDate = DateTime.Now.AddYears(-1),
                EndDate = DateTime.Now,
                Current = true
            };

            var result = _experienceValidator.Validate(experience);

            result.Errors.Should().Contain(e => e.PropertyName == "EndDate" && e.ErrorMessage == "A current role must not have an end date.");
        }

        [Fact]
        public void ExperienceValidator_ShouldFail_WhenPastRoleMissingEndDate()
        {
            var experience = new Experience
            {
                CompanyEn = "Acme",
                CompanyAr = "أكمي",
                PositionEn = "Dev",
                PositionAr = "مطور",
                StartDate = new DateTime(2020, 1, 1),
                Current = false
            };

            var result = _experienceValidator.Validate(experience);

            result.Errors.Should().Contain(e => e.PropertyName == "EndDate" && e.ErrorMessage == "A past role needs an end date.");
        }

        [Fact]
        public void ExperienceValidator_ShouldFail_WhenTechnologiesExceedMaxCount()
        {
            var experience = new Experience
            {
                CompanyEn = "Acme",
                CompanyAr = "أكمي",
                PositionEn = "Dev",
                PositionAr = "مطور",
                StartDate = DateTime.Now.AddYears(-1),
                EndDate = DateTime.Now,
                Technologies = new List<string>(Enumerable.Range(1, 21).Select(i => $"Tech{i}"))
            };

            var result = _experienceValidator.Validate(experience);

            result.Errors.Should().Contain(e => e.PropertyName == "Technologies" && e.ErrorMessage == "An experience can list at most 20 technologies.");
        }

        [Fact]
        public void ExperienceValidator_ShouldFail_WhenLocationExceedsMaxLength()
        {
            var experience = new Experience
            {
                CompanyEn = "Acme",
                CompanyAr = "أكمي",
                PositionEn = "Dev",
                PositionAr = "مطور",
                StartDate = DateTime.Now.AddYears(-1),
                EndDate = DateTime.Now,
                Current = false,
                LocationEn = new string('A', 101),
                LocationAr = new string('أ', 101)
            };

            var result = _experienceValidator.Validate(experience);

            result.Errors.Should().Contain(e => e.PropertyName == "LocationEn" && e.ErrorMessage == "Location (en) must not exceed 100 characters.");
        }

        #endregion

        #region CategoryValidator Tests

        [Fact]
        public void CategoryValidator_ShouldPass_WhenAllFieldsAreValid()
        {
            var category = new Category
            {
                NameEn = "Web Development",
                NameAr = "تطوير الويب",
                Order = 1,
                IsActive = true
            };

            var result = _categoryValidator.Validate(category);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void CategoryValidator_ShouldFail_WhenNameEnIsEmpty()
        {
            var category = new Category { NameEn = "", NameAr = "ويب", Order = 1, IsActive = true };

            var result = _categoryValidator.Validate(category);

            result.Errors.Should().Contain(e => e.PropertyName == "NameEn" && e.ErrorMessage == "Category name (en) is required.");
        }

        [Fact]
        public void CategoryValidator_ShouldFail_WhenNameArIsEmpty()
        {
            var category = new Category { NameEn = "Web", NameAr = "", Order = 1, IsActive = true };

            var result = _categoryValidator.Validate(category);

            result.Errors.Should().Contain(e => e.PropertyName == "NameAr" && e.ErrorMessage == "Category name (ar) is required.");
        }

        [Fact]
        public void CategoryValidator_ShouldFail_WhenNameEnExceedsMaxLength()
        {
            var category = new Category { NameEn = new string('A', 101), NameAr = new string('أ', 101), Order = 1, IsActive = true };

            var result = _categoryValidator.Validate(category);

            result.Errors.Should().Contain(e => e.PropertyName == "NameEn" && e.ErrorMessage == "Category name (en) must not exceed 100 characters.");
        }

        #endregion

        #region ContactValidator Tests

        [Fact]
        public void ContactValidator_ShouldPass_WhenAllFieldsAreValid()
        {
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john@example.com",
                Subject = "Inquiry",
                Message = "Hello, I would like to inquire about your services."
            };

            var result = _contactValidator.Validate(contact);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenNameIsEmpty()
        {
            var contact = new Contact { Name = "", Email = "john@example.com", Subject = "Inquiry", Message = "Hello" };

            var result = _contactValidator.Validate(contact);

            result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Name is required.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenEmailIsInvalid()
        {
            var contact = new Contact { Name = "John", Email = "invalid-email", Subject = "Inquiry", Message = "Hello" };

            var result = _contactValidator.Validate(contact);

            result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage == "Email must be a valid email address.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenSubjectIsEmpty()
        {
            var contact = new Contact { Name = "John", Email = "john@example.com", Subject = "", Message = "Hello" };

            var result = _contactValidator.Validate(contact);

            result.Errors.Should().Contain(e => e.PropertyName == "Subject" && e.ErrorMessage == "Subject is required.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenMessageIsEmpty()
        {
            var contact = new Contact { Name = "John", Email = "john@example.com", Subject = "Inquiry", Message = "" };

            var result = _contactValidator.Validate(contact);

            result.Errors.Should().Contain(e => e.PropertyName == "Message" && e.ErrorMessage == "Message is required.");
        }

        #endregion

        #region TestimonialValidator Tests

        [Fact]
        public void TestimonialValidator_ShouldPass_WhenAllFieldsAreValid()
        {
            var testimonial = new Testimonial
            {
                Name = "John Doe",
                Position = "CTO",
                Company = "TechCorp",
                Content = "Excellent work!",
                Rating = 5
            };

            var result = _testimonialValidator.Validate(testimonial);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void TestimonialValidator_ShouldFail_WhenNameIsEmpty()
        {
            var testimonial = new Testimonial { Name = "", Position = "CTO", Company = "Acme", Content = "Great", Rating = 5 };

            var result = _testimonialValidator.Validate(testimonial);

            result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Name is required.");
        }

        [Fact]
        public void TestimonialValidator_ShouldFail_WhenRatingIsOutOfRange()
        {
            var testimonial = new Testimonial { Name = "John", Position = "CTO", Company = "Acme", Content = "Good", Rating = 6 };

            var result = _testimonialValidator.Validate(testimonial);

            result.Errors.Should().Contain(e => e.PropertyName == "Rating" && e.ErrorMessage == "Rating must be between 1 and 5.");
        }

        #endregion

        #region CredentialValidator Tests

        [Fact]
        public void CredentialValidator_ShouldPass_WhenAllFieldsAreValid()
        {
            var credential = new Credential
            {
                Tab = "work",
                Year = "2023",
                PeriodEn = "2023 - Present",
                PeriodAr = "2023 - الحاضر",
                TitleEn = "Software Developer",
                TitleAr = "مطور برمجيات",
                OrgEn = "Freelance",
                OrgAr = "عمل حر",
                LocationEn = "Remote",
                LocationAr = "عن بعد",
                SummaryEn = "Building full-stack web applications",
                SummaryAr = "بناء تطبيقات ويب كاملة",
                HighlightsEn = new List<string> { "API design", "Performance optimization" },
                HighlightsAr = new List<string> { "تصميم API", "تحسين الأداء" },
                Stack = new List<string> { ".NET", "React", "SQL Server" }
            };

            var result = _credentialValidator.Validate(credential);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void CredentialValidator_ShouldFail_WhenTabIsInvalid()
        {
            var credential = new Credential { Tab = "invalid", Year = "2023", PeriodEn = "2023", PeriodAr = "2023", TitleEn = "Dev", TitleAr = "مطور", OrgEn = "Acme", OrgAr = "أكمي" };

            var result = _credentialValidator.Validate(credential);

            result.Errors.Should().Contain(e => e.PropertyName == "Tab" && e.ErrorMessage == "Tab must be one of: work, projects, education, certifications.");
        }

        [Fact]
        public void CredentialValidator_ShouldFail_WhenPeriodEnIsEmpty()
        {
            var credential = new Credential { Tab = "work", Year = "2023", PeriodEn = "", PeriodAr = "2023", TitleEn = "Dev", TitleAr = "مطور", OrgEn = "Acme", OrgAr = "أكمي" };

            var result = _credentialValidator.Validate(credential);

            result.Errors.Should().Contain(e => e.PropertyName == "PeriodEn" && e.ErrorMessage == "Period (en) is required.");
        }

        [Fact]
        public void CredentialValidator_ShouldFail_WhenPeriodArIsEmpty()
        {
            var credential = new Credential { Tab = "work", Year = "2023", PeriodEn = "2023", PeriodAr = "", TitleEn = "Dev", TitleAr = "مطور", OrgEn = "Acme", OrgAr = "أكمي" };

            var result = _credentialValidator.Validate(credential);

            result.Errors.Should().Contain(e => e.PropertyName == "PeriodAr" && e.ErrorMessage == "Period (ar) is required.");
        }

        [Fact]
        public void CredentialValidator_ShouldFail_WhenTitleEnIsEmpty()
        {
            var credential = new Credential { Tab = "work", Year = "2023", PeriodEn = "2023", PeriodAr = "2023", TitleEn = "", TitleAr = "مطور", OrgEn = "Acme", OrgAr = "أكمي" };

            var result = _credentialValidator.Validate(credential);

            result.Errors.Should().Contain(e => e.PropertyName == "TitleEn" && e.ErrorMessage == "Title (en) is required.");
        }

        [Fact]
        public void CredentialValidator_ShouldFail_WhenTitleArIsEmpty()
        {
            var credential = new Credential { Tab = "work", Year = "2023", PeriodEn = "2023", PeriodAr = "2023", TitleEn = "Dev", TitleAr = "", OrgEn = "Acme", OrgAr = "أكمي" };

            var result = _credentialValidator.Validate(credential);

            result.Errors.Should().Contain(e => e.PropertyName == "TitleAr" && e.ErrorMessage == "Title (ar) is required.");
        }

        [Fact]
        public void CredentialValidator_ShouldFail_WhenOrgEnIsEmpty()
        {
            var credential = new Credential { Tab = "work", Year = "2023", PeriodEn = "2023", PeriodAr = "2023", TitleEn = "Dev", TitleAr = "مطور", OrgEn = "", OrgAr = "أكمي" };

            var result = _credentialValidator.Validate(credential);

            result.Errors.Should().Contain(e => e.PropertyName == "OrgEn" && e.ErrorMessage == "Organization (en) is required.");
        }

        [Fact]
        public void CredentialValidator_ShouldFail_WhenOrgArIsEmpty()
        {
            var credential = new Credential { Tab = "work", Year = "2023", PeriodEn = "2023", PeriodAr = "2023", TitleEn = "Dev", TitleAr = "مطور", OrgEn = "Acme", OrgAr = "" };

            var result = _credentialValidator.Validate(credential);

            result.Errors.Should().Contain(e => e.PropertyName == "OrgAr" && e.ErrorMessage == "Organization (ar) is required.");
        }

        #endregion
    }
}