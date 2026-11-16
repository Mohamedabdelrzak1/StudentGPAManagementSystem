using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Domain.Contracts;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using ServiceAbstraction;
using Shared.Common;
using Shared.Dto;

namespace Service;

public class SemesterService : ISemesterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SemesterService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Response<IEnumerable<SemesterDto>>> GetAllSemestersAsync()
    {
        var repo = _unitOfWork.GetRepository<Semester, int>();
        var semesters = await repo.GetAllQueryable()
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.EndDate)
            .ToListAsync();

        var studentCounts = await GetEnrolledStudentCountsAsync();
        var currentSemesterId = semesters.FirstOrDefault()?.Id;
        var dtos = _mapper.Map<List<SemesterDto>>(semesters);
        foreach (var dto in dtos)
        {
            dto.IsCurrent = dto.Id == currentSemesterId;
            dto.EnrolledStudentsCount = studentCounts.GetValueOrDefault(dto.Id);
        }

        var response = Response<IEnumerable<SemesterDto>>.Success(dtos);
        response.TotalSemesters = dtos.Count;
        response.CurrentSemester = dtos.FirstOrDefault(s => s.IsCurrent);
        response.CompletedSemestersCount = Math.Max(0, dtos.Count - (response.CurrentSemester is null ? 0 : 1));

        return response;
    }

    public async Task<Response<SemesterDto>> GetSemesterByIdAsync(int id)
    {
        var repo = _unitOfWork.GetRepository<Semester, int>();
        var semester = await repo.GetByIdAsync(id);

        if (semester == null)
            return Response<SemesterDto>.Failure($"Semester with ID '{id}' was not found.", HttpStatusCode.NotFound);

        var dto = _mapper.Map<SemesterDto>(semester);
        dto.IsCurrent = await IsCurrentSemesterAsync(semester.Id);
        dto.EnrolledStudentsCount = await GetEnrolledStudentCountAsync(semester.Id);
        return Response<SemesterDto>.Success(dto);
    }

    public async Task<Response<SemesterDto>> CreateSemesterAsync(CreateSemesterDto dto)
    {
        if (dto == null)
            return Response<SemesterDto>.Failure("Invalid semester data.", HttpStatusCode.BadRequest);

        if (dto.EndDate <= dto.StartDate)
            return Response<SemesterDto>.Failure("End date must be greater than start date.", HttpStatusCode.BadRequest);

        var name = dto.Name.Trim();
        var repo = _unitOfWork.GetRepository<Semester, int>();
        var exists = await repo.GetAllQueryable()
            .AnyAsync(s => s.Name.ToUpper() == name.ToUpper());

        if (exists)
            return Response<SemesterDto>.Failure($"Semester with Name '{name}' already exists.", HttpStatusCode.Conflict);

        if (await HasOverlappingSemesterAsync(repo.GetAllQueryable(), dto.StartDate, dto.EndDate))
            return Response<SemesterDto>.Failure("Semester dates overlap with an existing semester.", HttpStatusCode.Conflict);

        var semester = _mapper.Map<Semester>(dto);
        semester.Name = name;
        semester.StartDate = dto.StartDate;
        semester.EndDate = dto.EndDate;

        await repo.AddAsync(semester);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<SemesterDto>(semester);
        resultDto.IsCurrent = true;
        resultDto.EnrolledStudentsCount = 0;
        return Response<SemesterDto>.Success(resultDto, "Semester created successfully.", HttpStatusCode.Created);
    }

    public async Task<Response<SemesterDto>> UpdateSemesterAsync(int id, UpdateSemesterDto dto)
    {
        if (dto == null)
            return Response<SemesterDto>.Failure("Invalid semester data.", HttpStatusCode.BadRequest);

        if (dto.EndDate <= dto.StartDate)
            return Response<SemesterDto>.Failure("End date must be greater than start date.", HttpStatusCode.BadRequest);

        var repo = _unitOfWork.GetRepository<Semester, int>();
        var semester = await repo.GetByIdAsync(id);

        if (semester == null)
            return Response<SemesterDto>.Failure($"Semester with ID '{id}' was not found.", HttpStatusCode.NotFound);

        var name = dto.Name.Trim();
        var duplicate = await repo.GetAllQueryable()
            .AnyAsync(s => s.Id != id && s.Name.ToUpper() == name.ToUpper());

        if (duplicate)
            return Response<SemesterDto>.Failure($"Another semester with Name '{name}' already exists.", HttpStatusCode.Conflict);

        if (await HasOverlappingSemesterAsync(
            repo.GetAllQueryable().Where(s => s.Id != id),
            dto.StartDate,
            dto.EndDate))
        {
            return Response<SemesterDto>.Failure("Semester dates overlap with an existing semester.", HttpStatusCode.Conflict);
        }

        _mapper.Map(dto, semester);
        semester.Name = name;
        semester.StartDate = dto.StartDate;
        semester.EndDate = dto.EndDate;

        repo.Update(semester);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<SemesterDto>(semester);
        resultDto.IsCurrent = await IsCurrentSemesterAsync(semester.Id);
        resultDto.EnrolledStudentsCount = await GetEnrolledStudentCountAsync(semester.Id);
        return Response<SemesterDto>.Success(resultDto, "Semester updated successfully.");
    }

    private async Task<Dictionary<int, int>> GetEnrolledStudentCountsAsync()
    {
        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        return await enrollmentRepo.GetAllQueryable()
            .GroupBy(e => e.SemesterId)
            .Select(g => new
            {
                SemesterId = g.Key,
                StudentCount = g.Select(e => e.StudentId).Distinct().Count()
            })
            .ToDictionaryAsync(x => x.SemesterId, x => x.StudentCount);
    }

    private async Task<int> GetEnrolledStudentCountAsync(int semesterId)
    {
        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        return await enrollmentRepo.GetAllQueryable()
            .Where(e => e.SemesterId == semesterId)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync();
    }

    private static Task<bool> HasOverlappingSemesterAsync(
        IQueryable<Semester> semesters,
        DateOnly startDate,
        DateOnly endDate)
    {
        return semesters.AnyAsync(s => s.StartDate <= endDate && s.EndDate >= startDate);
    }

    private async Task<bool> IsCurrentSemesterAsync(int semesterId)
    {
        var repo = _unitOfWork.GetRepository<Semester, int>();
        var latestSemesterId = await repo.GetAllQueryable()
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.EndDate)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        return latestSemesterId == semesterId;
    }

    public async Task<Response<bool>> DeleteSemesterAsync(int id)
    {
        var repo = _unitOfWork.GetRepository<Semester, int>();
        var semester = await repo.GetByIdAsync(id);

        if (semester == null)
            return Response<bool>.Failure($"Semester with ID '{id}' was not found.", HttpStatusCode.NotFound);

        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        var hasEnrollments = await enrollmentRepo.GetAllQueryable()
            .AnyAsync(e => e.SemesterId == id);

        if (hasEnrollments)
            return Response<bool>.Failure("Cannot delete semester because it contains registered enrollments.", HttpStatusCode.BadRequest);

        repo.Delete(semester);
        await _unitOfWork.SaveChangesAsync();

        return Response<bool>.Success(true, "Semester deleted successfully.");
    }
}
