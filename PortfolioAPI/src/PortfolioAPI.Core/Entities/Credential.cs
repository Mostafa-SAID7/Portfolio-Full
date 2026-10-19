using System.Collections.Generic;

namespace PortfolioAPI.Core.Entities
{
    /// <summary>
    /// A bilingual (en/ar) career credential shown on the portfolio's
    /// Experience page: work roles, showcased platforms, education and
    /// certifications. Owned by the backend so the dashboard can manage it.
    /// </summary>
    public class Credential : BaseEntity
    {
        /// <summary>work | projects | education | certifications</summary>
        public string Tab { get; set; } = "work";
        public string Year { get; set; } = string.Empty;

        public string PeriodEn { get; set; } = string.Empty;
        public string PeriodAr { get; set; } = string.Empty;
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public string OrgEn { get; set; } = string.Empty;
        public string OrgAr { get; set; } = string.Empty;
        public string LocationEn { get; set; } = string.Empty;
        public string LocationAr { get; set; } = string.Empty;
        public string SummaryEn { get; set; } = string.Empty;
        public string SummaryAr { get; set; } = string.Empty;

        // Nullable so EF reads NULL columns without invoking the converters.
        public List<string>? HighlightsEn { get; set; } = new();
        public List<string>? HighlightsAr { get; set; } = new();
        public List<string>? Stack { get; set; } = new();

        public string? CredentialId { get; set; }
        public string? StatusEn { get; set; }
        public string? StatusAr { get; set; }
        public bool Featured { get; set; } = false;
        public int Order { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
