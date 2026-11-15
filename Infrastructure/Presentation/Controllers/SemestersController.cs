using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ServiceAbstraction;
using Shared.Dto;

namespace Presentation.Controllers;

public class SemestersController : BaseApiController
{
    private readonly IServiceManager _serviceManager;

    public SemestersController(IServiceManager serviceManager)
    {
        _serviceManager = serviceManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSemesters()
    {
        var response = await _serviceManager.SemesterService.GetAllSemestersAsync();
        return ResponseResult(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSemesterById(int id)
    {
        var response = await _serviceManager.SemesterService.GetSemesterByIdAsync(id);
        return ResponseResult(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSemester([FromBody] CreateSemesterDto dto)
    {
        var response = await _serviceManager.SemesterService.CreateSemesterAsync(dto);
        return ResponseResult(response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSemester(int id, [FromBody] UpdateSemesterDto dto)
    {
        var response = await _serviceManager.SemesterService.UpdateSemesterAsync(id, dto);
        return ResponseResult(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSemester(int id)
    {
        var response = await _serviceManager.SemesterService.DeleteSemesterAsync(id);
        return ResponseResult(response);
    }
}
