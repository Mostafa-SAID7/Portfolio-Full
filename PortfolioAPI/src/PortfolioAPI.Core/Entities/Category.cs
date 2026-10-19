namespace PortfolioAPI.Core.Entities
{
    /// <summary>
    /// A project category ("Clothing", "Dashboard", …). The category list is
    /// managed from the dashboard; the public portfolio uses it for the filter
    /// chips, so a new category shows up on the site without a code change.
    /// </summary>
    public class Category : BaseEntity
    {
        /// <summary>Display name, unique across categories.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Sort order for the filter chips and the admin list.</summary>
        public int Order { get; set; }

        /// <summary>Inactive categories are hidden from the public site.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Applies an incoming payload onto this instance.</summary>
        public void Apply(string name, int order, bool isActive)
        {
            Name = name;
            Order = order;
            IsActive = isActive;
        }
    }
}
