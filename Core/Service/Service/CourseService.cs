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

public class CourseService : ICourseService
{
    private readonly IUnitOfWork _unitOfWork;

    public CourseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Response<IEnumerable<CourseDto>>> GetAllCoursesAsync()
    {
        var repo = _unitOfWork.GetRepository<Course, int>();
        var courses = await repo.GetAllQueryable()
            .OrderBy(c => c.Code)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                CreditHours = c.CreditHours
            })
            .ToListAsync();

        return Response<IEnumerable<CourseDto>>.Success(courses);
    }

    public async Task<Response<CourseDto>> GetCourseByIdAsync(int id)
    {
        var repo = _unitOfWork.GetRepository<Course, int>();
        var course = await repo.GetByIdAsync(id);

        if (course == null)
            return Response<CourseDto>.Failure($"Course with ID '{id}' was not found.", HttpStatusCode.NotFound);

        var dto = new CourseDto
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            CreditHours = course.CreditHours
        };

        return Response<CourseDto>.Success(dto);
    }

    public async Task<Response<CourseDto>> GetCourseByCodeAsync(string code)
    {
        var repo = _unitOfWork.GetRepository<Course, int>();
        var course = await repo.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.Trim().ToUpper());

        if (course == null)
            return Response<CourseDto>.Failure($"Course with Code '{code}' was not found.", HttpStatusCode.NotFound);

        var dto = new CourseDto
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            CreditHours = course.CreditHours
        };

        return Response<CourseDto>.Success(dto);
    }

    public async Task<Response<CourseDto>> CreateCourseAsync(CreateCourseDto dto)
    {
        if (dto == null)
            return Response<CourseDto>.Failure("Invalid course data.", HttpStatusCode.BadRequest);

        if (dto.CreditHours < 1 || dto.CreditHours > 3)
            return Response<CourseDto>.Failure("Credit hours must be between 1 and 3.", HttpStatusCode.BadRequest);

        var code = dto.Code.Trim();
        var name = dto.Name.Trim();

        var repo = _unitOfWork.GetRepository<Course, int>();
        var exists = await repo.GetAllQueryable()
            .AnyAsync(c => c.Code.ToUpper() == code.ToUpper());

        if (exists)
            return Response<CourseDto>.Failure($"Course with Code '{code}' already exists.", HttpStatusCode.Conflict);

        var course = new Course
        {
            Code = code,
            Name = name,
            CreditHours = dto.CreditHours
        };

        await repo.AddAsync(course);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = new CourseDto
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            CreditHours = course.CreditHours
        };

        return Response<CourseDto>.Success(resultDto, "Course created successfully.", HttpStatusCode.Created);
    }

    public async Task<Response<CourseDto>> UpdateCourseAsync(int id, UpdateCourseDto dto)
    {
        if (dto == null)
            return Response<CourseDto>.Failure("Invalid course data.", HttpStatusCode.BadRequest);

        if (dto.CreditHours < 1 || dto.CreditHours > 3)
            return Response<CourseDto>.Failure("Credit hours must be between 1 and 3.", HttpStatusCode.BadRequest);

        var repo = _unitOfWork.GetRepository<Course, int>();
        var course = await repo.GetByIdAsync(id);

        if (course == null)
            return Response<CourseDto>.Failure($"Course with ID '{id}' was not found.", HttpStatusCode.NotFound);

        course.Name = dto.Name.Trim();
        course.CreditHours = dto.CreditHours;

        repo.Update(course);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = new CourseDto
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            CreditHours = course.CreditHours
        };

        return Response<CourseDto>.Success(resultDto, "Course updated successfully.");
    }

    public async Task<Response<bool>> DeleteCourseAsync(int id)
    {
        var repo = _unitOfWork.GetRepository<Course, int>();
        var course = await repo.GetByIdAsync(id);

        if (course == null)
            return Response<bool>.Failure($"Course with ID '{id}' was not found.", HttpStatusCode.NotFound);

        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        var hasEnrollments = await enrollmentRepo.GetAllQueryable()
            .AnyAsync(e => e.CourseId == id);

        if (hasEnrollments)
            return Response<bool>.Failure("Cannot delete course because it has existing student enrollments.", HttpStatusCode.BadRequest);

        repo.Delete(course);
        await _unitOfWork.SaveChangesAsync();

        return Response<bool>.Success(true, "Course deleted successfully.");
    }
}
