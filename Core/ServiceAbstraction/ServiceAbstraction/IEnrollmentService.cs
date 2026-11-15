using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Common;
using Shared.Dto;

namespace ServiceAbstraction;

public interface IEnrollmentService
{
    Task<Response<IEnumerable<EnrollmentDto>>> GetAllEnrollmentsAsync(int? studentId = null, int? semesterId = null);
    Task<Response<EnrollmentDto>> GetEnrollmentByIdAsync(int id);
    Task<Response<EnrollmentDto>> EnrollCourseAsync(CreateEnrollmentDto dto);
    Task<Response<EnrollmentDto>> UpdateEnrollmentAsync(int id, UpdateEnrollmentDto dto);
    Task<Response<bool>> DeleteEnrollmentAsync(int id);
}
