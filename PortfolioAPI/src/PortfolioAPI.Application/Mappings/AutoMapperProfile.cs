using AutoMapper;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Project mappings
            CreateMap<Project, ProjectDto>().ReverseMap();

            // Skill mappings
            CreateMap<Skill, SkillDto>().ReverseMap();

            // Contact mappings
            CreateMap<Contact, ContactDto>().ReverseMap();
        }
    }
}