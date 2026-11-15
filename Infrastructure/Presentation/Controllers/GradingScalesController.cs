using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ServiceAbstraction;
using Shared.Common;
using Shared.Dto;

namespace Presentation.Controllers;

public class GradingScalesController : BaseApiController
{
    private readonly IServiceManager _serviceManager;

    public GradingScalesController(IServiceManager serviceManager)
    {
        _serviceManager = serviceManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllGradingScales()
    {
        var response = await _serviceManager.GradingScaleService.GetAllGradingScalesAsync();
        return ResponseResult(response);
    }

    [HttpGet("resolve")]
    public async Task<IActionResult> ResolveScore([FromQuery] decimal score)
    {
        var scale = await _serviceManager.GradingScaleService.ResolveGradeAsync(score);
        if (scale == null)
            return ResponseResult(Response<GradingScaleDto>.Failure($"Score '{score}' could not be resolved or is outside the valid 0-100 range."));

        return ResponseResult(Response<GradingScaleDto>.Success(scale, $"Score '{score}' maps to grade '{scale.Grade}' with {scale.Points} points."));
    }
}
