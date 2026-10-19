using System;
using System.Collections.Generic;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Read model for a bilingual career credential (api/Credentials, api/public/credentials).</summary>
    public class CredentialDto
    {
        public int Id { get; set; }
        /// <summary>work | projects | education | certifications</summary>
        public string Tab { get; set; } = string.Empty;
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
        public List<string> HighlightsEn { get; set; } = new();
        public List<string> HighlightsAr { get; set; } = new();
        public List<string> Stack { get; set; } = new();
        public string? CredentialId { get; set; }
        public string? StatusEn { get; set; }
        public string? StatusAr { get; set; }
        public bool Featured { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
