using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureHub.Application.UsesCases.ThirdPart;
using SecureHub.Infrastructure.Persistence.Repositories;
using System.Security.Claims;

namespace SecureHub.Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "external_api_role,admin_api_role")]
	public class ExternalSubjectController : ControllerBase
	{
		private readonly GetExternalSubjectDataUseCase _useCase;
		private readonly IPartContractRepository _contractRepo;

		public ExternalSubjectController(
			GetExternalSubjectDataUseCase useCase,
			IPartContractRepository contractRepo)
		{
			_useCase = useCase;
			_contractRepo = contractRepo;
		}

		[HttpGet("{subjectId:guid}")]
		public async Task<IActionResult> GetSubjectData(Guid subjectId, CancellationToken ct)
		{
			var isAdmin = User.IsInRole("admin_api_role");

			if (isAdmin)
			{
				var adminData = await _useCase.ExecuteBySubjectAsync(subjectId, ct);
				return Ok(adminData);
			}
			var username = User.FindFirstValue("preferred_username")
				?? throw new UnauthorizedAccessException("Token sin preferred_username.");
			var contract = await _contractRepo.GetActiveByKeycloakUsernameAsync(username, subjectId, ct);

			if (contract is null)
				return Forbid();

			var data = await _useCase.ExecuteAsync(contract.Id, ct);
			return Ok(data);
		}
	}
}
