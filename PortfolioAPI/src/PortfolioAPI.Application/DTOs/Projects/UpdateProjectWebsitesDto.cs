using System.Collections.Generic;

namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Payload for batch updating website assignments for a project.</summary>
    public class UpdateProjectWebsitesDto
    {
        public List<AssignProjectWebsiteDto> Websites { get; set; } = new();
    }
}
