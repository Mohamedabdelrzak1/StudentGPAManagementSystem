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

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public StudentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Response<IEnumerable<StudentDto>>> GetAllStudentsAsync()
    {
        var repo = _unitOfWork.GetRepository<Student, int>();
        var students = await repo.GetAllQueryable()
            .OrderBy(s => s.Code)
            .ToListAsync();

        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        var enrollments = await enrollmentRepo.GetAllQueryable()
            .Include(e => e.GradingScale)
            .ToListAsync();

        var enrollmentsByStudent = enrollments
            .GroupBy(e => e.StudentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dtos = new List<StudentDto>();
        foreach (var student in students)
        {
            var dto = _mapper.Map<StudentDto>(student);
            enrollmentsByStudent.TryGetValue(student.Id, out var studentEnrollments);
            PopulateStudentAcademicStanding(dto, studentEnrollments);
            dtos.Add(dto);
        }

        return Response<IEnumerable<StudentDto>>.Success(dtos);
    }

    public async Task<Response<StudentDto>> GetStudentByIdAsync(int id)
    {
        var repo = _unitOfWork.GetRepository<Student, int>();
        var student = await repo.GetByIdAsync(id);

        if (student == null)
            return Response<StudentDto>.Failure($"Student with ID '{id}' was not found.", HttpStatusCode.NotFound);

        var dto = _mapper.Map<StudentDto>(student);

        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        var studentEnrollments = await enrollmentRepo.GetAllQueryable()
            .Where(e => e.StudentId == id)
            .Include(e => e.GradingScale)
            .ToListAsync();

        PopulateStudentAcademicStanding(dto, studentEnrollments);
        return Response<StudentDto>.Success(dto);
    }

    public async Task<Response<StudentDto>> GetStudentByCodeAsync(string code)
    {
        var repo = _unitOfWork.GetRepository<Student, int>();
        var student = await repo.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Code.ToUpper() == code.Trim().ToUpper());

        if (student == null)
            return Response<StudentDto>.Failure($"Student with Code '{code}' was not found.", HttpStatusCode.NotFound);

        var dto = _mapper.Map<StudentDto>(student);

        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        var studentEnrollments = await enrollmentRepo.GetAllQueryable()
            .Where(e => e.StudentId == student.Id)
            .Include(e => e.GradingScale)
            .ToListAsync();

        PopulateStudentAcademicStanding(dto, studentEnrollments);
        return Response<StudentDto>.Success(dto);
    }

    public async Task<Response<StudentDto>> CreateStudentAsync(CreateStudentDto dto)
    {
        if (dto == null)
            return Response<StudentDto>.Failure("Invalid student data.", HttpStatusCode.BadRequest);

        var code = dto.Code.Trim();
        var repo = _unitOfWork.GetRepository<Student, int>();
        var exists = await repo.GetAllQueryable()
            .AnyAsync(s => s.Code.ToUpper() == code.ToUpper());

        if (exists)
            return Response<StudentDto>.Failure($"Student with Code '{code}' already exists.", HttpStatusCode.Conflict);

        var student = _mapper.Map<Student>(dto);
        student.Code = code;
        student.Name = dto.Name.Trim();

        await repo.AddAsync(student);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<StudentDto>(student);
        return Response<StudentDto>.Success(resultDto, "Student created successfully.", HttpStatusCode.Created);
    }

    public async Task<Response<StudentDto>> UpdateStudentAsync(int id, UpdateStudentDto dto)
    {
        if (dto == null)
            return Response<StudentDto>.Failure("Invalid student data.", HttpStatusCode.BadRequest);

        var repo = _unitOfWork.GetRepository<Student, int>();
        var student = await repo.GetByIdAsync(id);

        if (student == null)
            return Response<StudentDto>.Failure($"Student with ID '{id}' was not found.", HttpStatusCode.NotFound);

        _mapper.Map(dto, student);
        student.Name = dto.Name.Trim();

        repo.Update(student);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<StudentDto>(student);
        return Response<StudentDto>.Success(resultDto, "Student updated successfully.");
    }

    public async Task<Response<bool>> DeleteStudentAsync(int id)
    {
        var repo = _unitOfWork.GetRepository<Student, int>();
        var student = await repo.GetByIdAsync(id);

        if (student == null)
            return Response<bool>.Failure($"Student with ID '{id}' was not found.", HttpStatusCode.NotFound);

        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment, int>();
        var hasEnrollments = await enrollmentRepo.GetAllQueryable()
            .AnyAsync(e => e.StudentId == id);

        if (hasEnrollments)
            return Response<bool>.Failure("Cannot delete student because they have registered enrollments.", HttpStatusCode.BadRequest);

        repo.Delete(student);
        await _unitOfWork.SaveChangesAsync();

        return Response<bool>.Success(true, "Student deleted successfully.");
    }

    private static void PopulateStudentAcademicStanding(StudentDto dto, IEnumerable<Enrollment>? studentEnrollments)
    {
        dto.Major = "علوم الحاسب";

        if (studentEnrollments != null && studentEnrollments.Any())
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
                if (calc.CumulativeGpa.Value >= 3.50m)
                    dto.Status = "متفوق";
                else if (calc.CumulativeGpa.Value < 2.00m)
                    dto.Status = "إنذار أكاديمي";
                else
                    dto.Status = "منتظم";
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
    }
}
