using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Domain.Contracts;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using ServiceAbstraction;
using Shared.Common;
using Shared.Dto;

namespace Service;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGradingScaleService _gradingScaleService;

    public EnrollmentService(IUnitOfWork unitOfWork, IGradingScaleService gradingScaleService)
    {
        _unitOfWork = unitOfWork;
        _gradingScaleService = gradingScaleService;
    }

    public async Task<Response<IEnumerable<EnrollmentDto>>> GetAllEnrollmentsAsync(int? studentId = null, int? semesterId = null)
    {
        var repo = _unitOfWork.GetRepository<Enrollment, int>();
        var query = repo.GetAllQueryable();

        if (studentId.HasValue)
            query = query.Where(e => e.StudentId == studentId.Value);

        if (semesterId.HasValue)
            query = query.Where(e => e.SemesterId == semesterId.Value);

        var list = await query
            .Include(e => e.Student)
            .Include(e => e.Semester)
            .Include(e => e.Course)
            .Include(e => e.GradingScale)
            .OrderBy(e => e.Semester.StartDate)
            .ThenBy(e => e.Course.Code)
            .ToListAsync();

        var dtos = list.Select(MapToDto).ToList();

        return Response<IEnumerable<EnrollmentDto>>.Success(dtos);
    }

    public async Task<Response<EnrollmentDto>> GetEnrollmentByIdAsync(int id)
    {
        var repo = _unitOfWork.GetRepository<Enrollment, int>();
        var enrollment = await repo.GetAllQueryable()
            .Include(e => e.Student)
            .Include(e => e.Semester)
            .Include(e => e.Course)
            .Include(e => e.GradingScale)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
            return Response<EnrollmentDto>.Failure($"Enrollment with ID '{id}' was not found.", HttpStatusCode.NotFound);

        return Response<EnrollmentDto>.Success(MapToDto(enrollment));
    }

    public async Task<Response<EnrollmentDto>> EnrollCourseAsync(CreateEnrollmentDto dto)
    {
        if (dto == null)
            return Response<EnrollmentDto>.Failure("Invalid enrollment data.", HttpStatusCode.BadRequest);

        // 1. Verify Student exists
        var studentRepo = _unitOfWork.GetRepository<Student, int>();
        var student = await studentRepo.GetByIdAsync(dto.StudentId);
        if (student == null)
            return Response<EnrollmentDto>.Failure($"Student with ID '{dto.StudentId}' does not exist.", HttpStatusCode.NotFound);

        // 2. Verify Semester exists
        var semesterRepo = _unitOfWork.GetRepository<Semester, int>();
        var semester = await semesterRepo.GetByIdAsync(dto.SemesterId);
        if (semester == null)
            return Response<EnrollmentDto>.Failure($"Semester with ID '{dto.SemesterId}' does not exist.", HttpStatusCode.NotFound);

        // 3. Verify Course exists
        var courseRepo = _unitOfWork.GetRepository<Course, int>();
        var course = await courseRepo.GetByIdAsync(dto.CourseId);
        if (course == null)
            return Response<EnrollmentDto>.Failure($"Course with ID '{dto.CourseId}' does not exist.", HttpStatusCode.NotFound);

        // 4. Check duplicate enrollment (Rule 3.4: same course cannot be registered twice for the same student in the same semester)
        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        var isDuplicate = await enrollmentRepo.GetAllQueryable()
            .AnyAsync(e => e.StudentId == dto.StudentId && e.SemesterId == dto.SemesterId && e.CourseId == dto.CourseId);

        if (isDuplicate)
            return Response<EnrollmentDto>.Failure($"Course '{course.Code}' is already registered for this student in semester '{semester.Name}'.", HttpStatusCode.Conflict);

        // 5. Check 18-hour semester enrollment limit (Rule 3.4 & 3.5: All registered courses, including W and I, count toward the 18-hour limit)
        var existingSemesterEnrollments = await enrollmentRepo.GetAllQueryable()
            .Where(e => e.StudentId == dto.StudentId && e.SemesterId == dto.SemesterId)
            .ToListAsync();

        int currentRegisteredHours = existingSemesterEnrollments.Sum(e => e.CreditHours);
        if (currentRegisteredHours + course.CreditHours > 18)
        {
            return Response<EnrollmentDto>.Failure(
                $"Registering course '{course.Code}' ({course.CreditHours} credit hours) would exceed the maximum limit of 18 credit hours per semester. Current registered hours: {currentRegisteredHours}.",
                HttpStatusCode.BadRequest);
        }

        // 6. Parse and validate Status & Score (Rule 3.5: course supports either a numeric score OR a special status, never both)
        var parseResult = ParseAndValidateStatusAndScore(dto.Status, dto.Score);
        if (!parseResult.IsValid)
            return Response<EnrollmentDto>.Failure(parseResult.ErrorMessage!, HttpStatusCode.BadRequest);

        int? gradingScaleId = null;
        if (parseResult.Status == EnrollmentStatus.Normal)
        {
            var resolvedGrade = await _gradingScaleService.ResolveGradeAsync(dto.Score!.Value);
            if (resolvedGrade == null)
                return Response<EnrollmentDto>.Failure($"Could not map score '{dto.Score}' to a valid grading scale range.", HttpStatusCode.BadRequest);

            gradingScaleId = resolvedGrade.Id;
        }

        // 7. Create Enrollment record (Rule 3.4: CreditHours snapshot copied from course at registration time)
        var enrollment = new Enrollment
        {
            StudentId = dto.StudentId,
            SemesterId = dto.SemesterId,
            CourseId = dto.CourseId,
            CreditHours = course.CreditHours,
            Status = parseResult.Status,
            Score = parseResult.Status == EnrollmentStatus.Normal ? dto.Score : null,
            GradingScaleId = gradingScaleId
        };

        await enrollmentRepo.AddAsync(enrollment);
        await _unitOfWork.SaveChangesAsync();

        // Reload with navigations to return full DTO
        var saved = await enrollmentRepo.GetAllQueryable()
            .Include(e => e.Student)
            .Include(e => e.Semester)
            .Include(e => e.Course)
            .Include(e => e.GradingScale)
            .FirstAsync(e => e.Id == enrollment.Id);

        return Response<EnrollmentDto>.Success(MapToDto(saved), "Course enrolled successfully.", HttpStatusCode.Created);
    }

    public async Task<Response<EnrollmentDto>> UpdateEnrollmentAsync(int id, UpdateEnrollmentDto dto)
    {
        if (dto == null)
            return Response<EnrollmentDto>.Failure("Invalid enrollment update data.", HttpStatusCode.BadRequest);

        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        var enrollment = await enrollmentRepo.GetAllQueryable()
            .Include(e => e.Student)
            .Include(e => e.Semester)
            .Include(e => e.Course)
            .Include(e => e.GradingScale)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
            return Response<EnrollmentDto>.Failure($"Enrollment with ID '{id}' was not found.", HttpStatusCode.NotFound);

        // Validate Status & Score
        var parseResult = ParseAndValidateStatusAndScore(dto.Status, dto.Score);
        if (!parseResult.IsValid)
            return Response<EnrollmentDto>.Failure(parseResult.ErrorMessage!, HttpStatusCode.BadRequest);

        int? gradingScaleId = null;
        if (parseResult.Status == EnrollmentStatus.Normal)
        {
            var resolvedGrade = await _gradingScaleService.ResolveGradeAsync(dto.Score!.Value);
            if (resolvedGrade == null)
                return Response<EnrollmentDto>.Failure($"Could not map score '{dto.Score}' to a valid grading scale range.", HttpStatusCode.BadRequest);

            gradingScaleId = resolvedGrade.Id;
        }

        enrollment.Status = parseResult.Status;
        enrollment.Score = parseResult.Status == EnrollmentStatus.Normal ? dto.Score : null;
        enrollment.GradingScaleId = gradingScaleId;

        enrollmentRepo.Update(enrollment);
        await _unitOfWork.SaveChangesAsync();

        // Reload to refresh GradingScale navigation if changed
        var updated = await enrollmentRepo.GetAllQueryable()
            .Include(e => e.Student)
            .Include(e => e.Semester)
            .Include(e => e.Course)
            .Include(e => e.GradingScale)
            .FirstAsync(e => e.Id == enrollment.Id);

        return Response<EnrollmentDto>.Success(MapToDto(updated), "Enrollment updated successfully.");
    }

    public async Task<Response<bool>> DeleteEnrollmentAsync(int id)
    {
        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        var enrollment = await enrollmentRepo.GetByIdAsync(id);

        if (enrollment == null)
            return Response<bool>.Failure($"Enrollment with ID '{id}' was not found.", HttpStatusCode.NotFound);

        enrollmentRepo.Delete(enrollment);
        await _unitOfWork.SaveChangesAsync();

        return Response<bool>.Success(true, "Enrollment removed successfully.");
    }

    private static (bool IsValid, EnrollmentStatus Status, string? ErrorMessage) ParseAndValidateStatusAndScore(string? statusStr, decimal? score)
    {
        EnrollmentStatus status = EnrollmentStatus.Normal;

        if (!string.IsNullOrWhiteSpace(statusStr))
        {
            var normalized = statusStr.Trim();
            if (string.Equals(normalized, "Normal", StringComparison.OrdinalIgnoreCase))
                status = EnrollmentStatus.Normal;
            else if (string.Equals(normalized, "Absent", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "ABS", StringComparison.OrdinalIgnoreCase))
                status = EnrollmentStatus.Absent;
            else if (string.Equals(normalized, "Withdrawn", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "W", StringComparison.OrdinalIgnoreCase))
                status = EnrollmentStatus.Withdrawn;
            else if (string.Equals(normalized, "Incomplete", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "I", StringComparison.OrdinalIgnoreCase))
                status = EnrollmentStatus.Incomplete;
            else
                return (false, status, $"Invalid academic status '{statusStr}'. Supported statuses are: Normal, Absent, Withdrawn, Incomplete.");
        }

        // Rule: A registered course must have EITHER a numeric score OR a special status, not both.
        if (status == EnrollmentStatus.Normal)
        {
            if (!score.HasValue)
                return (false, status, "A numeric score is required when status is Normal.");

            if (score.Value < 0 || score.Value > 100)
                return (false, status, "Numeric score must be between 0 and 100 inclusive.");
        }
        else
        {
            if (score.HasValue)
                return (false, status, "A course cannot have both a numeric score and a special academic status (Absent, Withdrawn, Incomplete). Score must be null.");
        }

        return (true, status, null);
    }

    public static EnrollmentDto MapToDto(Enrollment e)
    {
        string? grade = null;
        decimal? gradePoints = null;
        decimal? qualityPoints = null;
        bool includedInGpa = false;

        switch (e.Status)
        {
            case EnrollmentStatus.Normal:
                includedInGpa = true;
                grade = e.GradingScale?.Grade;
                gradePoints = e.GradingScale?.Points;
                if (gradePoints.HasValue)
                    qualityPoints = gradePoints.Value * e.CreditHours;
                break;

            case EnrollmentStatus.Absent:
                includedInGpa = true;
                grade = "ABS";
                gradePoints = 0.0m;
                qualityPoints = 0.0m;
                break;

            case EnrollmentStatus.Withdrawn:
                includedInGpa = false;
                grade = "W";
                gradePoints = null;
                qualityPoints = null;
                break;

            case EnrollmentStatus.Incomplete:
                includedInGpa = false;
                grade = "I";
                gradePoints = null;
                qualityPoints = null;
                break;
        }

        return new EnrollmentDto
        {
            Id = e.Id,
            StudentId = e.StudentId,
            StudentCode = e.Student?.Code ?? string.Empty,
            StudentName = e.Student?.Name ?? string.Empty,
            SemesterId = e.SemesterId,
            SemesterName = e.Semester?.Name ?? string.Empty,
            CourseId = e.CourseId,
            CourseCode = e.Course?.Code ?? string.Empty,
            CourseName = e.Course?.Name ?? string.Empty,
            CreditHours = e.CreditHours,
            Status = e.Status.ToString(),
            Score = e.Score,
            GradingScaleId = e.GradingScaleId,
            Grade = grade,
            GradePoints = gradePoints,
            QualityPoints = qualityPoints,
            IncludedInGpa = includedInGpa
        };
    }
}
