using System.Collections.Generic;
using System.Threading.Tasks;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Interfaces
{
    public interface ISkillService
    {
        Task<IEnumerable<Skill>> GetAllSkillsAsync();
        Task<Skill> GetSkillByIdAsync(int id);
        Task<IEnumerable<Skill>> GetSkillsByCategoryAsync(string category);
        Task<IEnumerable<Skill>> GetVisibleSkillsAsync();
        Task<Skill> CreateSkillAsync(Skill skill);
        Task UpdateSkillAsync(Skill skill);
        Task DeleteSkillAsync(int id);
    }
}

