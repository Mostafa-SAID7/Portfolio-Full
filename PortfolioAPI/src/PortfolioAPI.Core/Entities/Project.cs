using System.Collections.Generic;

namespace PortfolioAPI.Core.Entities
{
    public class Project : BaseEntity
    {
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;

        /// <summary>
        /// Stable, URL-safe identifier of the project (e.g. "haxel-footwear").
        /// It is generated once and never changed by a rename, so the public
        /// link of a project survives every title edit (the old behaviour
        /// derived the link from the title, which broke every deep link and
        /// every indexed URL as soon as a project was renamed).
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        public string DescriptionEn { get; set; } = string.Empty;
        public string DescriptionAr { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        /// <summary>
        /// Which storage backend owns this project's cover image: "local" or "cloudinary".
        /// Null means unknown/legacy and defaults to local for backward compatibility.
        /// </summary>
        public string? StorageProvider { get; set; }
        /// <summary>
        /// Provider-native identity of the cover image (Cloudinary public id). Null for local files.
        /// This is the value that must be passed to delete/replace operations - never derived from the URL.
        /// </summary>
        public string? StoragePublicId { get; set; }
        public List<string> Technologies { get; set; } = new();
        public string? GithubUrl { get; set; }
        public string? LiveUrl { get; set; }
        public bool Featured { get; set; } = false;
        public int Order { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Editorial/presentation fields consumed by the public portfolio
        // (filter chips, client badge, gradient fallback art, metrics strip).
        public string? CategoryEn { get; set; }
        public string? CategoryAr { get; set; }
        public string? ClientEn { get; set; }
        public string? ClientAr { get; set; }
        public string? TypeEn { get; set; }          // Team | Freelance | Self
        public string? TypeAr { get; set; }
        public string? DatabaseEn { get; set; }
        public string? DatabaseAr { get; set; }
        public string? StatusEn { get; set; }        // In Progress | Completed | Deployed
        public string? StatusAr { get; set; }
        // Nullable: the columns are nullable (ALTERed in on existing databases)
        // and EF bypasses value converters when the stored value is NULL.
        public List<string>? BadgesEn { get; set; } = new();
        public List<string>? BadgesAr { get; set; } = new();
        public List<ProjectMetric>? MetricsEn { get; set; } = new();
        public List<ProjectMetric>? MetricsAr { get; set; } = new();
        public string? Gradient { get; set; }

        // Navigation properties
        public ICollection<ProjectImage> Images { get; set; } = new List<ProjectImage>();
    }
}