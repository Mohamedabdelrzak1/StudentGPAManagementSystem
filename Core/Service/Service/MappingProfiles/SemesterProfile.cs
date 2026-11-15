using AutoMapper;
using Domain.Models;
using Shared.Dto;

namespace Service.MappingProfiles;

public class SemesterProfile : Profile
{
    public SemesterProfile()
    {
        CreateMap<Semester, SemesterDto>();
        CreateMap<CreateSemesterDto, Semester>();
        CreateMap<UpdateSemesterDto, Semester>();
    }
}
