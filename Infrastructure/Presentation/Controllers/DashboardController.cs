using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ServiceAbstraction;
using Shared.Dto;

namespace Presentation.Controllers;

public class DashboardController : BaseApiController
{
    private readonly IServiceManager _serviceManager;

    public DashboardController(IServiceManager serviceManager)
    {
        _serviceManager = serviceManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var response = await _serviceManager.DashboardService.GetDashboardSummaryAsync();
        return ResponseResult(response);
    }
}
