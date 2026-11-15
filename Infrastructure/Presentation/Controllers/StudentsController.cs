using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ServiceAbstraction;
using Shared.Dto;

namespace Presentation.Controllers;

public class StudentsController : BaseApiController
{
    private readonly IServiceManager _serviceManager;

    public StudentsController(IServiceManager serviceManager)
    {
        _serviceManager = serviceManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllStudents()
    {
        var response = await _serviceManager.StudentService.GetAllStudentsAsync();
        return ResponseResult(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetStudentById(int id)
    {
        var response = await _serviceManager.StudentService.GetStudentByIdAsync(id);
        return ResponseResult(response);
    }

    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetStudentByCode(string code)
    {
        var response = await _serviceManager.StudentService.GetStudentByCodeAsync(code);
        return ResponseResult(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto dto)
    {
        var response = await _serviceManager.StudentService.CreateStudentAsync(dto);
        return ResponseResult(response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto)
    {
        var response = await _serviceManager.StudentService.UpdateStudentAsync(id, dto);
        return ResponseResult(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteStudent(int id)
    {
        var response = await _serviceManager.StudentService.DeleteStudentAsync(id);
        return ResponseResult(response);
    }

    [HttpGet("{id:int}/gpa")]
    public async Task<IActionResult> CalculateGpa(int id)
    {
        var response = await _serviceManager.GpaCalculatorService.CalculateGpaAsync(id);
        return ResponseResult(response);
    }

    [HttpGet("{id:int}/transcript")]
    public async Task<IActionResult> GetTranscript(int id)
    {
        var response = await _serviceManager.GpaCalculatorService.GetTranscriptAsync(id);
        return ResponseResult(response);
    }
}
