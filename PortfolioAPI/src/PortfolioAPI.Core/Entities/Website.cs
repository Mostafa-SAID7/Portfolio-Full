using System.Collections.Generic;

namespace PortfolioAPI.Core.Entities
{
    /// <summary>
    /// Represents an independent frontend website/client consuming the centralized portfolio platform
    /// (e.g. Marketplace, Fintech, EduTech, Real Estate, Healthcare, etc.).
    /// </summary>
    public class Website : BaseEntity
    {
        /// <summary>Display name of the frontend website (e.g. "Marketplace", "Fintech / Banking").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Stable, URL-safe identifier for public API routing and client identification
        /// (e.g. "marketplace", "banking", "edutech", "real-estate").
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>Optional description of the target audience or business domain.</summary>
        public string? Description { get; set; }

        /// <summary>Base URL or deployment URL of this frontend (e.g. "https://portfolio-market.vercel.app").</summary>
        public string? BaseUrl { get; set; }

        /// <summary>Sort order for admin listings and switcher menus.</summary>
        public int Order { get; set; } = 0;

        /// <summary>Inactive websites are hidden from public API queries.</summary>
        public bool IsActive { get; set; } = true;

        // Navigation property to many-to-many join
        public ICollection<ProjectWebsite> ProjectWebsites { get; set; } = new List<ProjectWebsite>();
    }
}

