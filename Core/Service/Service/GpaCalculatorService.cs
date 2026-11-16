using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Domain.Contracts;
using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using ServiceAbstraction;
using Shared.Common;
using Shared.Dto;

namespace Service;

public record CourseGpaItem(int CreditHours, EnrollmentStatus Status, decimal? GradePoints);

public record SemesterGpaCalculation(int RegisteredHours, int GpaEligibleHours, decimal QualityPoints, decimal? Gpa, string GpaDisplay);

public record CumulativeGpaCalculation(int TotalRegisteredHours, int TotalGpaEligibleHours, decimal TotalQualityPoints, decimal? CumulativeGpa, string CumulativeGpaDisplay);

public class GpaCalculatorService : IGpaCalculatorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GpaCalculatorService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public static SemesterGpaCalculation CalculateSemester(IEnumerable<CourseGpaItem> courses)
    {
        int registeredHours = 0;
        int eligibleHours = 0;
        decimal qualityPoints = 0m;

        foreach (var c in courses)
        {
            registeredHours += c.CreditHours;

            if (c.Status == EnrollmentStatus.Normal)
            {
                decimal points = c.GradePoints ?? 0m;
                eligibleHours += c.CreditHours;
                qualityPoints += points * c.CreditHours;
            }
            else if (c.Status == EnrollmentStatus.Absent)
            {
                eligibleHours += c.CreditHours;
            }
        }

        decimal? gpa = eligibleHours > 0
            ? Math.Round(qualityPoints / (decimal)eligibleHours, 2, MidpointRounding.AwayFromZero)
            : null;

        return new SemesterGpaCalculation(
            registeredHours,
            eligibleHours,
            qualityPoints,
            gpa,
            FormatGpa(gpa));
    }

    public static CumulativeGpaCalculation CalculateCumulative(IEnumerable<SemesterGpaCalculation> semesterResults)
    {
        int totalRegistered = 0;
        int totalEligible = 0;
        decimal totalQualityPoints = 0m;

        foreach (var s in semesterResults)
        {
            totalRegistered += s.RegisteredHours;
            totalEligible += s.GpaEligibleHours;
            totalQualityPoints += s.QualityPoints;
        }

        decimal? cumulativeGpa = totalEligible > 0
            ? Math.Round(totalQualityPoints / (decimal)totalEligible, 2, MidpointRounding.AwayFromZero)
            : null;

        return new CumulativeGpaCalculation(
            totalRegistered,
            totalEligible,
            totalQualityPoints,
            cumulativeGpa,
            FormatGpa(cumulativeGpa));
    }

    public static CumulativeGpaCalculation CalculateCumulative(IEnumerable<CourseGpaItem> allCourses)
    {
        int totalRegistered = 0;
        int totalEligible = 0;
        decimal totalQualityPoints = 0m;

        foreach (var c in allCourses)
        {
            totalRegistered += c.CreditHours;

            if (c.Status == EnrollmentStatus.Normal)
            {
                decimal points = c.GradePoints ?? 0m;
                totalEligible += c.CreditHours;
                totalQualityPoints += points * c.CreditHours;
            }
            else if (c.Status == EnrollmentStatus.Absent)
            {
                totalEligible += c.CreditHours;
            }
        }

        decimal? cumulativeGpa = totalEligible > 0
            ? Math.Round(totalQualityPoints / (decimal)totalEligible, 2, MidpointRounding.AwayFromZero)
            : null;

        return new CumulativeGpaCalculation(
            totalRegistered,
            totalEligible,
            totalQualityPoints,
            cumulativeGpa,
            FormatGpa(cumulativeGpa));
    }

    public static string FormatGpa(decimal? gpa) => gpa.HasValue ? gpa.Value.ToString("0.00") : "N/A";

    public async Task<Response<GpaResultDto>> CalculateGpaAsync(int studentId)
    {
        var studentRepo = _unitOfWork.GetRepository<Student, int>();
        var student = await studentRepo.GetByIdAsync(studentId);

        if (student == null)
            return Response<GpaResultDto>.Failure($"Student with ID '{studentId}' was not found.", HttpStatusCode.NotFound);

        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        var enrollments = await enrollmentRepo.GetAllQueryable()
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Semester)
            .Include(e => e.Course)
            .Include(e => e.GradingScale)
            .OrderBy(e => e.Semester.StartDate)
            .ToListAsync();

        var semesterGroups = enrollments
            .GroupBy(e => new { e.SemesterId, e.Semester.Name, e.Semester.StartDate })
            .OrderBy(g => g.Key.StartDate)
            .ToList();

        var semesterSummaries = new List<SemesterGpaSummaryDto>();
        var allCourseItems = new List<CourseGpaItem>();

        foreach (var group in semesterGroups)
        {
            var semesterCourseItems = group.Select(e => new CourseGpaItem(
                e.CreditHours,
                e.Status,
                e.GradingScale?.Points
            )).ToList();

            allCourseItems.AddRange(semesterCourseItems);

            var semCalc = CalculateSemester(semesterCourseItems);

            semesterSummaries.Add(new SemesterGpaSummaryDto
            {
                SemesterId = group.Key.SemesterId,
                SemesterName = group.Key.Name,
                RegisteredHours = semCalc.RegisteredHours,
                GpaEligibleHours = semCalc.GpaEligibleHours,
                QualityPoints = semCalc.QualityPoints,
                SemesterGpa = semCalc.Gpa,
                SemesterGpaDisplay = semCalc.GpaDisplay
            });
        }

        var cumulativeCalc = CalculateCumulative(allCourseItems);

        var result = new GpaResultDto
        {
            StudentId = student.Id,
            StudentCode = student.Code,
            StudentName = student.Name,
            TotalRegisteredHours = cumulativeCalc.TotalRegisteredHours,
            TotalGpaEligibleHours = cumulativeCalc.TotalGpaEligibleHours,
            TotalQualityPoints = cumulativeCalc.TotalQualityPoints,
            CumulativeGpa = cumulativeCalc.CumulativeGpa,
            CumulativeGpaDisplay = cumulativeCalc.CumulativeGpaDisplay,
            Semesters = semesterSummaries
        };

        return Response<GpaResultDto>.Success(result, "GPA calculated successfully.");
    }

    public async Task<Response<TranscriptDto>> GetTranscriptAsync(int studentId)
    {
        var studentRepo = _unitOfWork.GetRepository<Student, int>();
        var student = await studentRepo.GetByIdAsync(studentId);

        if (student == null)
            return Response<TranscriptDto>.Failure($"Student with ID '{studentId}' was not found.", HttpStatusCode.NotFound);

        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        var enrollments = await enrollmentRepo.GetAllQueryable()
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Semester)
            .Include(e => e.Course)
            .Include(e => e.GradingScale)
            .OrderBy(e => e.Semester.StartDate)
            .ThenBy(e => e.Course.Code)
            .ToListAsync();

        var semesterGroups = enrollments
            .GroupBy(e => new { e.SemesterId, e.Semester.Name, e.Semester.StartDate })
            .OrderBy(g => g.Key.StartDate)
            .ToList();

        var semesterTranscriptList = new List<SemesterTranscriptDto>();
        var allCourseItems = new List<CourseGpaItem>();

        foreach (var group in semesterGroups)
        {
            var semesterCourseItems = group.Select(e => new CourseGpaItem(
                e.CreditHours,
                e.Status,
                e.GradingScale?.Points
            )).ToList();

            allCourseItems.AddRange(semesterCourseItems);

            var semCalc = CalculateSemester(semesterCourseItems);
            var courseTranscriptList = group.Select(e => _mapper.Map<CourseTranscriptDto>(e)).ToList();

            semesterTranscriptList.Add(new SemesterTranscriptDto
            {
                SemesterId = group.Key.SemesterId,
                SemesterName = group.Key.Name,
                RegisteredHours = semCalc.RegisteredHours,
                GpaEligibleHours = semCalc.GpaEligibleHours,
                TotalQualityPoints = semCalc.QualityPoints,
                SemesterGpa = semCalc.Gpa,
                SemesterGpaDisplay = semCalc.GpaDisplay,
                Courses = courseTranscriptList
            });
        }

        var cumulativeCalc = CalculateCumulative(allCourseItems);

        var transcript = new TranscriptDto
        {
            StudentId = student.Id,
            StudentCode = student.Code,
            StudentName = student.Name,
            TotalRegisteredHours = cumulativeCalc.TotalRegisteredHours,
            TotalGpaEligibleHours = cumulativeCalc.TotalGpaEligibleHours,
            TotalQualityPoints = cumulativeCalc.TotalQualityPoints,
            CumulativeGpa = cumulativeCalc.CumulativeGpa,
            CumulativeGpaDisplay = cumulativeCalc.CumulativeGpaDisplay,
            Semesters = semesterTranscriptList
        };

        return Response<TranscriptDto>.Success(transcript);
    }
}
