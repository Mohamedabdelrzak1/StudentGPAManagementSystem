using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Common;
using Shared.Dto;

namespace ServiceAbstraction;

public interface ICourseService
{
    Task<Response<IEnumerable<CourseDto>>> GetAllCoursesAsync();
    Task<Response<CourseDto>> GetCourseByIdAsync(int id);
    Task<Response<CourseDto>> GetCourseByCodeAsync(string code);
    Task<Response<CourseDto>> CreateCourseAsync(CreateCourseDto dto);
    Task<Response<CourseDto>> UpdateCourseAsync(int id, UpdateCourseDto dto);
    Task<Response<bool>> DeleteCourseAsync(int id);
}
