using AutoMapper;
using Domain.Models;
using Shared.Dto;

namespace Service.MappingProfiles;

public class GradingScaleProfile : Profile
{
    public GradingScaleProfile()
    {
        CreateMap<GradingScale, GradingScaleDto>();
    }
}
