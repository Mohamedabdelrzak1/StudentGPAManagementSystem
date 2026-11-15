using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Common;
using Shared.Dto;

namespace ServiceAbstraction;

public interface ISemesterService
{
    Task<Response<IEnumerable<SemesterDto>>> GetAllSemestersAsync();
    Task<Response<SemesterDto>> GetSemesterByIdAsync(int id);
    Task<Response<SemesterDto>> CreateSemesterAsync(CreateSemesterDto dto);
    Task<Response<SemesterDto>> UpdateSemesterAsync(int id, UpdateSemesterDto dto);
    Task<Response<bool>> DeleteSemesterAsync(int id);
}
