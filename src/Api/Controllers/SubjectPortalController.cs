using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureHub.Application.UsesCases.Arco;
using SecureHub.Application.UsesCases.Arco.Dtos;

namespace SecureHub.Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "applicant_role,admin_api_role")]
	public class SubjectPortalController : ControllerBase
	{
		private readonly GetSubjectPortalDataUseCase _getDataUseCase;
		private readonly RegisterSubjectBiometricUseCase _registerBiometricUseCase;
		private readonly VerifySubjectBiometricUseCase _verifyBiometricUseCase;

		public SubjectPortalController(
			GetSubjectPortalDataUseCase getDataUseCase,
			RegisterSubjectBiometricUseCase registerBiometricUseCase,
			VerifySubjectBiometricUseCase verifyBiometricUseCase)
		{
			_getDataUseCase = getDataUseCase;
			_registerBiometricUseCase = registerBiometricUseCase;
			_verifyBiometricUseCase = verifyBiometricUseCase;
		}

		[HttpGet("{subjectId:guid}")]
		public async Task<IActionResult> GetMyData(Guid subjectId, CancellationToken ct)
		{
			try
			{
				var result = await _getDataUseCase.ExecuteAsync(subjectId, ct);
				if (result is null) return NotFound(new { message = "Titular no encontrado." });
				return Ok(result);
			}
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpPost("{subjectId:guid}/biometric")]
		public async Task<IActionResult> RegisterBiometric(
			Guid subjectId,
			[FromBody] RegisterBiometricCommand command,
			CancellationToken ct)
		{
			try
			{
				var biometricId = await _registerBiometricUseCase.ExecuteAsync(subjectId, command, ct);
				return Ok(new { biometricId, message = "Datos biométricos registrados exitosamente." });
			}
			catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpPost("{subjectId:guid}/biometric/verify")]
		public async Task<IActionResult> VerifyBiometric(
			Guid subjectId,
			[FromBody] VerifyBiometricCommand command,
			CancellationToken ct)
		{
			try
			{
				var verified = await _verifyBiometricUseCase.ExecuteAsync(subjectId, command, ct);
				if (!verified)
					return Unauthorized(new { message = "Verificación biométrica fallida. Intente nuevamente." });
				return Ok(new { verified = true, message = "Identidad verificada correctamente." });
			}
			catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}
	}
}