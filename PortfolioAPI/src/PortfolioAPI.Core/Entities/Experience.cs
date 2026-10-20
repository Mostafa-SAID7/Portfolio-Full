namespace PortfolioAPI.Core.Entities
{
    public class Experience : BaseEntity
    {
        public string CompanyEn { get; set; } = string.Empty;
        public string CompanyAr { get; set; } = string.Empty;
        public string PositionEn { get; set; } = string.Empty;
        public string PositionAr { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string DescriptionEn { get; set; } = string.Empty;
        public string DescriptionAr { get; set; } = string.Empty;
        public List<string> Technologies { get; set; } = new();
        public string? LocationEn { get; set; }
        public string? LocationAr { get; set; }
        public bool Current { get; set; } = false;
        public int Order { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        /// <summary>Copies the editable fields from another instance onto this tracked one.</summary>
        public void Apply(Experience other)
        {
            CompanyEn = other.CompanyEn;
            CompanyAr = other.CompanyAr;
            PositionEn = other.PositionEn;
            PositionAr = other.PositionAr;
            StartDate = other.StartDate;
            EndDate = other.EndDate;
            DescriptionEn = other.DescriptionEn;
            DescriptionAr = other.DescriptionAr;
            Technologies = other.Technologies ?? Technologies;
            LocationEn = other.LocationEn;
            LocationAr = other.LocationAr;
            Current = other.Current;
            Order = other.Order;
            IsActive = other.IsActive;
        }
    }
}