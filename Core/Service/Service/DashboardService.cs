using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Domain.Contracts;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using ServiceAbstraction;
using Shared.Common;
using Shared.Dto;

namespace Service;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DashboardService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Response<DashboardDto>> GetDashboardSummaryAsync()
    {
        var studentRepo = _unitOfWork.GetRepository<Student, int>();
        var courseRepo = _unitOfWork.GetRepository<Course, int>();
        var semesterRepo = _unitOfWork.GetRepository<Semester, int>();
        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();

        var students = await studentRepo.GetAllQueryable()
            .OrderBy(s => s.Code)
            .ToListAsync();

        var activeCoursesCount = await courseRepo.GetAllQueryable().CountAsync();

        var semesters = await semesterRepo.GetAllQueryable()
            .OrderBy(s => s.StartDate)
            .ToListAsync();
        var currentSemester = semesters.LastOrDefault();

        var enrollments = await enrollmentRepo.GetAllQueryable()
            .Include(e => e.GradingScale)
            .ToListAsync();

        // 1. Calculate cumulative GPA and academic standing per student
        var enrollmentsByStudent = enrollments
            .GroupBy(e => e.StudentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var studentGpas = new List<decimal>();
        var studentDtoList = new List<StudentDto>();
        var honorStudentsList = new List<HonorStudentItemDto>();

        foreach (var student in students)
        {
            var dto = _mapper.Map<StudentDto>(student);
            dto.Major = "علوم الحاسب";

            if (enrollmentsByStudent.TryGetValue(student.Id, out var studentEnrollments) && studentEnrollments.Count > 0)
            {
                var courseItems = studentEnrollments.Select(e => new CourseGpaItem(
                    e.CreditHours,
                    e.Status,
                    e.GradingScale?.Points
                )).ToList();

                var calc = GpaCalculatorService.CalculateCumulative(courseItems);
                dto.Gpa = calc.CumulativeGpa;

                if (calc.CumulativeGpa.HasValue)
                {
                    decimal gpa = calc.CumulativeGpa.Value;
                    studentGpas.Add(gpa);

                    if (gpa >= 3.50m)
                    {
                        dto.Status = "متفوق";
                        honorStudentsList.Add(new HonorStudentItemDto
                        {
                            StudentId = student.Id,
                            StudentCode = student.Code,
                            StudentName = student.Name,
                            CumulativeGpa = gpa
                        });
                    }
                    else if (gpa < 2.00m)
                    {
                        dto.Status = "إنذار أكاديمي";
                    }
                    else
                    {
                        dto.Status = "منتظم";
                    }
                }
                else
                {
                    dto.Status = "منتظم";
                }
            }
            else
            {
                dto.Gpa = null;
                dto.Status = "منتظم";
            }

            studentDtoList.Add(dto);
        }

        // 2. Aggregate Institute KPIs
        decimal instituteAvgGpa = studentGpas.Count > 0
            ? Math.Round(studentGpas.Average(), 2, MidpointRounding.AwayFromZero)
            : 0.00m;

        int honorCount = honorStudentsList.Count;
        decimal honorPercentage = studentGpas.Count > 0
            ? Math.Round((decimal)honorCount / studentGpas.Count * 100m, 1, MidpointRounding.AwayFromZero)
            : 0.0m;

        // 3. Calculate Academic Performance Trend per Semester
        var trendList = new List<SemesterTrendDto>();

        foreach (var semester in semesters)
        {
            var semesterEnrollments = enrollments.Where(e => e.SemesterId == semester.Id).ToList();
            var distinctEnrolledCount = semesterEnrollments.Select(e => e.StudentId).Distinct().Count();

            decimal semesterAvgGpa = 0.00m;
            if (distinctEnrolledCount > 0)
            {
                var semGpas = semesterEnrollments
                    .GroupBy(e => e.StudentId)
                    .Select(g =>
                    {
                        var items = g.Select(e => new CourseGpaItem(e.CreditHours, e.Status, e.GradingScale?.Points));
                        return GpaCalculatorService.CalculateSemester(items).Gpa;
                    })
                    .Where(g => g.HasValue)
                    .Select(g => g!.Value)
                    .ToList();

                if (semGpas.Count > 0)
                {
                    semesterAvgGpa = Math.Round(semGpas.Average(), 2, MidpointRounding.AwayFromZero);
                }
            }

            trendList.Add(new SemesterTrendDto
            {
                SemesterId = semester.Id,
                SemesterName = semester.Name,
                AverageGpa = semesterAvgGpa,
                EnrolledStudentsCount = distinctEnrolledCount
            });
        }

        // 4. Calculate Grade Distribution across Academic Tiers
        int totalEvaluated = studentGpas.Count;
        int excellentCount = studentGpas.Count(g => g >= 3.40m);
        int veryGoodCount = studentGpas.Count(g => g >= 2.80m && g < 3.40m);
        int goodCount = studentGpas.Count(g => g >= 2.20m && g < 2.80m);
        int passFailCount = studentGpas.Count(g => g < 2.20m);

        decimal CalcPercentage(int count) => totalEvaluated > 0
            ? Math.Round((decimal)count / totalEvaluated * 100m, 1, MidpointRounding.AwayFromZero)
            : 0.0m;

        var gradeDistribution = new GradeDistributionDto
        {
            TotalStudentsEvaluated = totalEvaluated,
            Excellent = new GradeTierDto
            {
                Count = excellentCount,
                Percentage = CalcPercentage(excellentCount)
            },
            VeryGood = new GradeTierDto
            {
                Count = veryGoodCount,
                Percentage = CalcPercentage(veryGoodCount)
            },
            Good = new GradeTierDto
            {
                Count = goodCount,
                Percentage = CalcPercentage(goodCount)
            },
            PassFail = new GradeTierDto
            {
                Count = passFailCount,
                Percentage = CalcPercentage(passFailCount)
            }
        };

        var dashboard = new DashboardDto
        {
            TotalStudents = students.Count,
            TotalSemesters = semesters.Count,
            CompletedSemestersCount = currentSemester is null ? 0 : semesters.Count - 1,
            InstituteAverageGpa = instituteAvgGpa,
            ActiveCoursesCount = activeCoursesCount,
            HonorStudentsCount = honorCount,
            HonorStudentsPercentage = honorPercentage,
            HonorStudents = honorStudentsList,
            CurrentSemester = currentSemester is null
                ? null
                : CreateCurrentSemesterDto(currentSemester, enrollments),
            PerformanceTrend = trendList,
            GradeDistribution = gradeDistribution,
            Students = studentDtoList
        };

        return Response<DashboardDto>.Success(dashboard);
    }

    private SemesterDto CreateCurrentSemesterDto(Semester semester, List<Enrollment> enrollments)
    {
        var dto = _mapper.Map<SemesterDto>(semester);
        dto.IsCurrent = true;
        dto.EnrolledStudentsCount = enrollments
            .Where(e => e.SemesterId == semester.Id)
            .Select(e => e.StudentId)
            .Distinct()
            .Count();

        return dto;
    }
}
