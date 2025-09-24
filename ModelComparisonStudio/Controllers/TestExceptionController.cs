using Microsoft.AspNetCore.Mvc;
using ModelComparisonStudio.Core.Exceptions;

namespace ModelComparisonStudio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestExceptionController : ControllerBase
    {
        [HttpGet("validation")]
        public IActionResult ThrowValidationException()
        {
            throw new ValidationException("This is a validation error");
        }

        [HttpGet("authentication")]
        public IActionResult ThrowAuthenticationException()
        {
            throw new AuthenticationException("This is an authentication error");
        }

        [HttpGet("notfound")]
        public IActionResult ThrowNotFoundException()
        {
            throw new NotFoundException("This is a not found error");
        }

        [HttpGet("business")]
        public IActionResult ThrowBusinessException()
        {
            throw new BusinessException("This is a business error");
        }

        [HttpGet("general")]
        public IActionResult ThrowGeneralException()
        {
            throw new Exception("This is a general exception");
        }

        [HttpGet("ok")]
        public IActionResult OkResponse()
        {
            return Ok(new { message = "Success", timestamp = DateTime.UtcNow });
        }
    }
}