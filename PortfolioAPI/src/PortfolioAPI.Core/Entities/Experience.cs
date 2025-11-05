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
    }
}