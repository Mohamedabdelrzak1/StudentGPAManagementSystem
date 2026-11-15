using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Common;
using Shared.Dto;

namespace ServiceAbstraction;

public interface IGradingScaleService
{
    Task<Response<IEnumerable<GradingScaleDto>>> GetAllGradingScalesAsync();
    Task<GradingScaleDto?> ResolveGradeAsync(decimal score);
}
