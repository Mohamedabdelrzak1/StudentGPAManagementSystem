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

public class GpaCalculatorService : IGpaCalculatorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GpaCalculatorService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

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

        decimal cumulativeQualityPoints = 0m;
        int cumulativeEligibleHours = 0;
        int cumulativeRegisteredHours = 0;

        foreach (var group in semesterGroups)
        {
            int semesterRegisteredHours = 0;
            int semesterEligibleHours = 0;
            decimal semesterQualityPoints = 0m;

            foreach (var e in group)
            {
                semesterRegisteredHours += e.CreditHours;

                if (e.Status == EnrollmentStatus.Normal)
                {
                    decimal points = e.GradingScale?.Points ?? 0m;
                    decimal qp = points * e.CreditHours;

                    semesterEligibleHours += e.CreditHours;
                    semesterQualityPoints += qp;
                }
                else if (e.Status == EnrollmentStatus.Absent)
                {
                    // Absent (ABS): The course counts toward GPA credit hours and contributes ZERO grade points.
                    semesterEligibleHours += e.CreditHours;
                    // Quality points are 0.0
                }
                // Withdrawn (W) and Incomplete (I) are excluded from both numerator and denominator
            }

            decimal? semesterGpa = semesterEligibleHours > 0
                ? Math.Round(semesterQualityPoints / (decimal)semesterEligibleHours, 2, MidpointRounding.AwayFromZero)
                : null;

            cumulativeRegisteredHours += semesterRegisteredHours;
            cumulativeEligibleHours += semesterEligibleHours;
            cumulativeQualityPoints += semesterQualityPoints;

            semesterSummaries.Add(new SemesterGpaSummaryDto
            {
                SemesterId = group.Key.SemesterId,
                SemesterName = group.Key.Name,
                RegisteredHours = semesterRegisteredHours,
                GpaEligibleHours = semesterEligibleHours,
                QualityPoints = semesterQualityPoints,
                SemesterGpa = semesterGpa,
                SemesterGpaDisplay = semesterGpa.HasValue ? semesterGpa.Value.ToString("0.00") : "N/A"
            });
        }

        // Cumulative GPA = Sum of Eligible Quality Points Across All Semesters / Sum of Eligible Credit Hours Across All Semesters
        // Weighted by credit hours, not arithmetic average of semester GPAs
        decimal? cumulativeGpa = cumulativeEligibleHours > 0
            ? Math.Round(cumulativeQualityPoints / (decimal)cumulativeEligibleHours, 2, MidpointRounding.AwayFromZero)
            : null;

        var result = new GpaResultDto
        {
            StudentId = student.Id,
            StudentCode = student.Code,
            StudentName = student.Name,
            TotalRegisteredHours = cumulativeRegisteredHours,
            TotalGpaEligibleHours = cumulativeEligibleHours,
            TotalQualityPoints = cumulativeQualityPoints,
            CumulativeGpa = cumulativeGpa,
            CumulativeGpaDisplay = cumulativeGpa.HasValue ? cumulativeGpa.Value.ToString("0.00") : "N/A",
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

        decimal cumulativeQualityPoints = 0m;
        int cumulativeEligibleHours = 0;
        int cumulativeRegisteredHours = 0;

        foreach (var group in semesterGroups)
        {
            int semesterRegisteredHours = 0;
            int semesterEligibleHours = 0;
            decimal semesterQualityPoints = 0m;

            var courseTranscriptList = new List<CourseTranscriptDto>();

            foreach (var e in group)
            {
                semesterRegisteredHours += e.CreditHours;

                if (e.Status == EnrollmentStatus.Normal)
                {
                    decimal points = e.GradingScale?.Points ?? 0m;
                    decimal qp = points * e.CreditHours;

                    semesterEligibleHours += e.CreditHours;
                    semesterQualityPoints += qp;
                }
                else if (e.Status == EnrollmentStatus.Absent)
                {
                    semesterEligibleHours += e.CreditHours;
                }

                courseTranscriptList.Add(_mapper.Map<CourseTranscriptDto>(e));
            }

            decimal? semesterGpa = semesterEligibleHours > 0
                ? Math.Round(semesterQualityPoints / (decimal)semesterEligibleHours, 2, MidpointRounding.AwayFromZero)
                : null;

            cumulativeRegisteredHours += semesterRegisteredHours;
            cumulativeEligibleHours += semesterEligibleHours;
            cumulativeQualityPoints += semesterQualityPoints;

            semesterTranscriptList.Add(new SemesterTranscriptDto
            {
                SemesterId = group.Key.SemesterId,
                SemesterName = group.Key.Name,
                RegisteredHours = semesterRegisteredHours,
                GpaEligibleHours = semesterEligibleHours,
                TotalQualityPoints = semesterQualityPoints,
                SemesterGpa = semesterGpa,
                SemesterGpaDisplay = semesterGpa.HasValue ? semesterGpa.Value.ToString("0.00") : "N/A",
                Courses = courseTranscriptList
            });
        }

        decimal? cumulativeGpa = cumulativeEligibleHours > 0
            ? Math.Round(cumulativeQualityPoints / (decimal)cumulativeEligibleHours, 2, MidpointRounding.AwayFromZero)
            : null;

        var transcript = new TranscriptDto
        {
            StudentId = student.Id,
            StudentCode = student.Code,
            StudentName = student.Name,
            TotalRegisteredHours = cumulativeRegisteredHours,
            TotalGpaEligibleHours = cumulativeEligibleHours,
            TotalQualityPoints = cumulativeQualityPoints,
            CumulativeGpa = cumulativeGpa,
            CumulativeGpaDisplay = cumulativeGpa.HasValue ? cumulativeGpa.Value.ToString("0.00") : "N/A",
            Semesters = semesterTranscriptList
        };

        return Response<TranscriptDto>.Success(transcript);
    }
}
