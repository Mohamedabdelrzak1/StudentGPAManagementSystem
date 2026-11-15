using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Common;
using Shared.Dto;

namespace ServiceAbstraction;

public interface IStudentService
{
    Task<Response<IEnumerable<StudentDto>>> GetAllStudentsAsync();
    Task<Response<StudentDto>> GetStudentByIdAsync(int id);
    Task<Response<StudentDto>> GetStudentByCodeAsync(string code);
    Task<Response<StudentDto>> CreateStudentAsync(CreateStudentDto dto);
    Task<Response<StudentDto>> UpdateStudentAsync(int id, UpdateStudentDto dto);
    Task<Response<bool>> DeleteStudentAsync(int id);
}
