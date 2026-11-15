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
            .OrderBy(s => s.StartDate)
            .ToListAsync();

        var dtos = _mapper.Map<IEnumerable<SemesterDto>>(semesters);
        return Response<IEnumerable<SemesterDto>>.Success(dtos);
    }

    public async Task<Response<SemesterDto>> GetSemesterByIdAsync(int id)
    {
        var repo = _unitOfWork.GetRepository<Semester, int>();
        var semester = await repo.GetByIdAsync(id);

        if (semester == null)
            return Response<SemesterDto>.Failure($"Semester with ID '{id}' was not found.", HttpStatusCode.NotFound);

        var dto = _mapper.Map<SemesterDto>(semester);
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

        var semester = _mapper.Map<Semester>(dto);
        semester.Name = name;
        semester.StartDate = dto.StartDate;
        semester.EndDate = dto.EndDate;

        await repo.AddAsync(semester);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<SemesterDto>(semester);
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

        _mapper.Map(dto, semester);
        semester.Name = name;
        semester.StartDate = dto.StartDate;
        semester.EndDate = dto.EndDate;

        repo.Update(semester);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<SemesterDto>(semester);
        return Response<SemesterDto>.Success(resultDto, "Semester updated successfully.");
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
