using Microsoft.AspNetCore.Mvc;
using Shared.Common;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult ResponseResult<T>(Response<T> response)
    {
        if (response == null) return NotFound();
        return StatusCode((int)response.StatusCode, response);
    }

    protected IActionResult ResponseResult<T>(PaginationResponse<T> response)
    {
        if (response == null) return NotFound();
        return StatusCode((int)response.StatusCode, response);
    }

    protected IActionResult ResponseResult(object response)
    {
        if (response == null) return NotFound();
        return Ok(response);
    }
}
