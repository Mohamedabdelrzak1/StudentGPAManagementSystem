using AutoMapper;
using Domain.Enums;
using Domain.Models;
using Shared.Dto;

namespace Service.MappingProfiles;

public class EnrollmentProfile : Profile
{
    public EnrollmentProfile()
    {
        CreateMap<Enrollment, EnrollmentDto>()
            .ForMember(d => d.StudentCode, opt => opt.MapFrom(s => s.Student != null ? s.Student.Code : string.Empty))
            .ForMember(d => d.StudentName, opt => opt.MapFrom(s => s.Student != null ? s.Student.Name : string.Empty))
            .ForMember(d => d.SemesterName, opt => opt.MapFrom(s => s.Semester != null ? s.Semester.Name : string.Empty))
            .ForMember(d => d.CourseCode, opt => opt.MapFrom(s => s.Course != null ? s.Course.Code : string.Empty))
            .ForMember(d => d.CourseName, opt => opt.MapFrom(s => s.Course != null ? s.Course.Name : string.Empty))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Grade, opt => opt.MapFrom(s => ResolveGrade(s)))
            .ForMember(d => d.GradePoints, opt => opt.MapFrom(s => ResolveGradePoints(s)))
            .ForMember(d => d.QualityPoints, opt => opt.MapFrom(s => ResolveQualityPoints(s)))
            .ForMember(d => d.IncludedInGpa, opt => opt.MapFrom(s => s.Status == EnrollmentStatus.Normal || s.Status == EnrollmentStatus.Absent));

        CreateMap<Enrollment, CourseTranscriptDto>()
            .ForMember(d => d.EnrollmentId, opt => opt.MapFrom(s => s.Id))
            .ForMember(d => d.CourseCode, opt => opt.MapFrom(s => s.Course != null ? s.Course.Code : string.Empty))
            .ForMember(d => d.CourseName, opt => opt.MapFrom(s => s.Course != null ? s.Course.Name : string.Empty))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.DisplayedGrade, opt => opt.MapFrom(s => ResolveGrade(s) ?? string.Empty))
            .ForMember(d => d.Points, opt => opt.MapFrom(s => ResolveGradePoints(s)))
            .ForMember(d => d.QualityPoints, opt => opt.MapFrom(s => ResolveQualityPoints(s)))
            .ForMember(d => d.IncludedInGpa, opt => opt.MapFrom(s => s.Status == EnrollmentStatus.Normal || s.Status == EnrollmentStatus.Absent));
    }

    private static string? ResolveGrade(Enrollment s) => s.Status switch
    {
        EnrollmentStatus.Normal => s.GradingScale?.Grade,
        EnrollmentStatus.Absent => "ABS",
        EnrollmentStatus.Withdrawn => "W",
        EnrollmentStatus.Incomplete => "I",
        _ => s.Status.ToString()
    };

    private static decimal? ResolveGradePoints(Enrollment s) => s.Status switch
    {
        EnrollmentStatus.Normal => s.GradingScale?.Points,
        EnrollmentStatus.Absent => 0.0m,
        _ => null
    };

    private static decimal? ResolveQualityPoints(Enrollment s) => s.Status switch
    {
        EnrollmentStatus.Normal => (s.GradingScale?.Points ?? 0m) * s.CreditHours,
        EnrollmentStatus.Absent => 0.0m,
        _ => null
    };
}
