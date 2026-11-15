using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ServiceAbstraction;
using Shared.Dto;

namespace Presentation.Controllers;

public class EnrollmentsController : BaseApiController
{
    private readonly IServiceManager _serviceManager;

    public EnrollmentsController(IServiceManager serviceManager)
    {
        _serviceManager = serviceManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllEnrollments([FromQuery] int? studentId, [FromQuery] int? semesterId)
    {
        var response = await _serviceManager.EnrollmentService.GetAllEnrollmentsAsync(studentId, semesterId);
        return ResponseResult(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEnrollmentById(int id)
    {
        var response = await _serviceManager.EnrollmentService.GetEnrollmentByIdAsync(id);
        return ResponseResult(response);
    }

    [HttpPost]
    public async Task<IActionResult> EnrollCourse([FromBody] CreateEnrollmentDto dto)
    {
        var response = await _serviceManager.EnrollmentService.EnrollCourseAsync(dto);
        return ResponseResult(response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEnrollment(int id, [FromBody] UpdateEnrollmentDto dto)
    {
        var response = await _serviceManager.EnrollmentService.UpdateEnrollmentAsync(id, dto);
        return ResponseResult(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEnrollment(int id)
    {
        var response = await _serviceManager.EnrollmentService.DeleteEnrollmentAsync(id);
        return ResponseResult(response);
    }
}
