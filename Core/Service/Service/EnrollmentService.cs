using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Domain.Contracts;
using Domain.Enums;
using Domain.Exceptions;
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
    private readonly IMapper _mapper;

    public EnrollmentService(IUnitOfWork unitOfWork, IGradingScaleService gradingScaleService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _gradingScaleService = gradingScaleService;
        _mapper = mapper;
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

        var dtos = _mapper.Map<IEnumerable<EnrollmentDto>>(list);

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

        return Response<EnrollmentDto>.Success(_mapper.Map<EnrollmentDto>(enrollment));
    }

    public async Task<Response<EnrollmentDto>> EnrollCourseAsync(CreateEnrollmentDto dto)
    {
        if (dto == null)
            return Response<EnrollmentDto>.Failure("Invalid enrollment data.", HttpStatusCode.BadRequest);

        var studentRepo = _unitOfWork.GetRepository<Student, int>();
        var student = await studentRepo.GetByIdAsync(dto.StudentId);
        if (student == null)
            return Response<EnrollmentDto>.Failure($"Student with ID '{dto.StudentId}' does not exist.", HttpStatusCode.NotFound);

        var semesterRepo = _unitOfWork.GetRepository<Semester, int>();
        var semester = await semesterRepo.GetByIdAsync(dto.SemesterId);
        if (semester == null)
            return Response<EnrollmentDto>.Failure($"Semester with ID '{dto.SemesterId}' does not exist.", HttpStatusCode.NotFound);

        var courseRepo = _unitOfWork.GetRepository<Course, int>();
        var course = await courseRepo.GetByIdAsync(dto.CourseId);
        if (course == null)
            return Response<EnrollmentDto>.Failure($"Course with ID '{dto.CourseId}' does not exist.", HttpStatusCode.NotFound);

        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        var isDuplicate = await enrollmentRepo.GetAllQueryable()
            .AnyAsync(e => e.StudentId == dto.StudentId && e.SemesterId == dto.SemesterId && e.CourseId == dto.CourseId);

        if (isDuplicate)
            return Response<EnrollmentDto>.Failure($"Course '{course.Code}' is already registered for this student in semester '{semester.Name}'.", HttpStatusCode.Conflict);

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

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            studentRepo.Update(student);

            var existingSemesterEnrollments = await enrollmentRepo.GetAllQueryable()
                .Where(e => e.StudentId == dto.StudentId && e.SemesterId == dto.SemesterId)
                .ToListAsync();

            int currentRegisteredHours = existingSemesterEnrollments.Sum(e => e.CreditHours);
            if (currentRegisteredHours + course.CreditHours > 18)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Response<EnrollmentDto>.Failure(
                    $"Registering course '{course.Code}' ({course.CreditHours} credit hours) would exceed the maximum limit of 18 credit hours per semester. Current registered hours: {currentRegisteredHours}.",
                    HttpStatusCode.BadRequest);
            }

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
            await _unitOfWork.CommitTransactionAsync();

            var saved = await enrollmentRepo.GetAllQueryable()
                .Include(e => e.Student)
                .Include(e => e.Semester)
                .Include(e => e.Course)
                .Include(e => e.GradingScale)
                .FirstAsync(e => e.Id == enrollment.Id);

            return Response<EnrollmentDto>.Success(_mapper.Map<EnrollmentDto>(saved), "Course enrolled successfully.", HttpStatusCode.Created);
        }
        catch (DbUpdateConcurrencyException)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Response<EnrollmentDto>.Failure(
                "A concurrency conflict occurred while registering the course. The student record was modified by another operation. Please retry.",
                HttpStatusCode.Conflict);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
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

        var otherSemesterEnrollments = await enrollmentRepo.GetAllQueryable()
            .Where(e => e.StudentId == enrollment.StudentId && e.SemesterId == enrollment.SemesterId && e.Id != id)
            .ToListAsync();

        int otherHours = otherSemesterEnrollments.Sum(e => e.CreditHours);
        if (otherHours + enrollment.CreditHours > 18)
        {
            return Response<EnrollmentDto>.Failure(
                $"Updating enrollment would exceed the maximum limit of 18 credit hours per semester. Other registered hours: {otherHours}.",
                HttpStatusCode.BadRequest);
        }

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

        var updated = await enrollmentRepo.GetAllQueryable()
            .Include(e => e.Student)
            .Include(e => e.Semester)
            .Include(e => e.Course)
            .Include(e => e.GradingScale)
            .FirstAsync(e => e.Id == enrollment.Id);

        return Response<EnrollmentDto>.Success(_mapper.Map<EnrollmentDto>(updated), "Enrollment updated successfully.");
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
}
