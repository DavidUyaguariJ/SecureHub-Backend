using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureHub.Application.UsesCases.RegisterSubject;

namespace SecureHub.Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(Roles = "admin_api_role")]
	public class SubjectController : ControllerBase
	{
		private readonly RegisterSubjectUseCase _registerSubjectUseCase;

		public SubjectController(RegisterSubjectUseCase registerSubjectUseCase)
		{
			_registerSubjectUseCase = registerSubjectUseCase;
		}

		[HttpPost("/register")]
		public async Task<IActionResult> Register([FromBody] RegisterSubjectCommand command)
		{
			try
			{
				var result = await _registerSubjectUseCase.ExecuteAsync(command);
				return Ok(result);
			}
			catch (InvalidOperationException ex)
			{
				return Conflict(new { message = ex.Message });
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}
	}
}
