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

        /// <summary>
        /// Copies the editable fields from another instance onto this tracked
        /// one. Attaching the incoming instance directly (the old behaviour)
        /// threw "another instance with the same key ... is already being
        /// tracked" → HTTP 500 on every credential update.
        /// </summary>
        public void Apply(Credential other)
        {
            Tab = other.Tab;
            Year = other.Year;
            PeriodEn = other.PeriodEn;
            PeriodAr = other.PeriodAr;
            TitleEn = other.TitleEn;
            TitleAr = other.TitleAr;
            OrgEn = other.OrgEn;
            OrgAr = other.OrgAr;
            LocationEn = other.LocationEn;
            LocationAr = other.LocationAr;
            SummaryEn = other.SummaryEn;
            SummaryAr = other.SummaryAr;
            HighlightsEn = other.HighlightsEn;
            HighlightsAr = other.HighlightsAr;
            Stack = other.Stack;
            CredentialId = other.CredentialId;
            StatusEn = other.StatusEn;
            StatusAr = other.StatusAr;
            Featured = other.Featured;
            Order = other.Order;
            IsActive = other.IsActive;
        }
    }
}
