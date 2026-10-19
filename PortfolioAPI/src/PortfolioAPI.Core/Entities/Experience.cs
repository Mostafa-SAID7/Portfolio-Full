namespace PortfolioAPI.Core.Entities
{
    public class Experience : BaseEntity
    {
        public string Company { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Technologies { get; set; } = new();
        public string? Location { get; set; }
        public bool Current { get; set; } = false;
        public int Order { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        /// <summary>Copies the editable fields from another instance onto this tracked one.</summary>
        public void Apply(Experience other)
        {
            Company = other.Company;
            Position = other.Position;
            StartDate = other.StartDate;
            EndDate = other.EndDate;
            Description = other.Description;
            Technologies = other.Technologies ?? Technologies;
            Location = other.Location;
            Current = other.Current;
            Order = other.Order;
            IsActive = other.IsActive;
        }
    }
}