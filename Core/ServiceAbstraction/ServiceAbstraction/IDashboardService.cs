using System.Threading.Tasks;
using Shared.Common;
using Shared.Dto;

namespace ServiceAbstraction;

public interface IDashboardService
{
    Task<Response<DashboardDto>> GetDashboardSummaryAsync();
}
