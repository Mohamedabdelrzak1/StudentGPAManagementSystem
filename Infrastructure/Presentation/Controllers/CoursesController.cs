using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ServiceAbstraction;
using Shared.Dto;

namespace Presentation.Controllers;

public class CoursesController : BaseApiController
{
    private readonly IServiceManager _serviceManager;

    public CoursesController(IServiceManager serviceManager)
    {
        _serviceManager = serviceManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCourses()
    {
        var response = await _serviceManager.CourseService.GetAllCoursesAsync();
        return ResponseResult(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCourseById(int id)
    {
        var response = await _serviceManager.CourseService.GetCourseByIdAsync(id);
        return ResponseResult(response);
    }

    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetCourseByCode(string code)
    {
        var response = await _serviceManager.CourseService.GetCourseByCodeAsync(code);
        return ResponseResult(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
    {
        var response = await _serviceManager.CourseService.CreateCourseAsync(dto);
        return ResponseResult(response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto)
    {
        var response = await _serviceManager.CourseService.UpdateCourseAsync(id, dto);
        return ResponseResult(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var response = await _serviceManager.CourseService.DeleteCourseAsync(id);
        return ResponseResult(response);
    }
}
