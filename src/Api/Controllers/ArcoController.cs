using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Arco;
using SecureHub.Application.UsesCases.Arco.Dtos;
using System.Security.Claims;

namespace SecureHub.Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class ArcoRequestController : ControllerBase
	{
		private readonly CreateArcoRequestUseCase _createUseCase;
		private readonly UpdateArcoStatusUseCase _updateStatusUseCase;
		private readonly GetArcoRequestsUseCase _getUseCase;
		private readonly LookupSubjectUseCase _lookupUseCase;
		private readonly IArcoResponsePdfService _pdfService;

		public ArcoRequestController(
			CreateArcoRequestUseCase createUseCase,
			UpdateArcoStatusUseCase updateStatusUseCase,
			GetArcoRequestsUseCase getUseCase,
			LookupSubjectUseCase lookupUseCase,
			IArcoResponsePdfService pdfService)
		{
			_createUseCase = createUseCase;
			_updateStatusUseCase = updateStatusUseCase;
			_getUseCase = getUseCase;
			_lookupUseCase = lookupUseCase;
			_pdfService = pdfService;
		}

		[HttpGet("subject/lookup")]
		[Authorize(Roles = "admin_api_role,applicant_api_role")]
		public async Task<IActionResult> LookupSubject([FromQuery] string identification, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(identification))
				return BadRequest(new { message = "La identificación es requerida." });
			var result = await _lookupUseCase.ExecuteAsync(identification, ct);
			if (result is null)
				return NotFound(new { message = "No se encontró ningún titular con esa identificación." });
			return Ok(result);
		}

		[HttpPost]
		[Authorize(Roles = "admin_api_role,applicant_api_role")]
		public async Task<IActionResult> Create([FromBody] CreateArcoRequestDto dto, CancellationToken ct)
		{
			try
			{
				var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
				var result = await _createUseCase.ExecuteAsync(dto, ip, ct);
				return CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
			}
			catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
			catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpGet]
		[Authorize(Roles = "admin_api_role, technician_api_role")]
		public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken ct)
		{
			try { return Ok(await _getUseCase.GetAllAsync(status, ct)); }
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpGet("{id:guid}")]
		[Authorize(Roles = "admin_api_role,technician_api_role")]
		public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
		{
			try
			{
				var result = await _getUseCase.GetDetailAsync(id, ct);
				if (result is null) return NotFound(new { message = "Solicitud no encontrada." });
				return Ok(result);
			}
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpGet("subject/{subjectId:guid}")]
		[Authorize(Roles = "admin_api_role,technician_api_role,applicant_api_role")]
		public async Task<IActionResult> GetBySubject(Guid subjectId, CancellationToken ct)
		{
			try { return Ok(await _getUseCase.GetBySubjectAsync(subjectId, ct)); }
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpPatch("{id:guid}/status")]
		[Authorize(Roles = "admin_api_role,technician_api_role")]
		public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateArcoStatusDto dto, CancellationToken ct)
		{
			try
			{
				var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
				Guid operatorId = Guid.TryParse(subClaim, out var parsed) ? parsed : Guid.Empty;
				var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
				var operatorName = User.FindFirstValue("name")
					?? User.FindFirstValue("preferred_username")
					?? "Operador";
				var enrichedDto = dto with { OperatorName = operatorName };
				return Ok(await _updateStatusUseCase.ExecuteAsync(id, enrichedDto, operatorId, ip, ct));
			}
			catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpGet("{id:guid}/download")]
		[Authorize(Roles = "admin_api_role,technician_api_role")]
		public async Task<IActionResult> DownloadResponse(Guid id, CancellationToken ct)
		{
			try
			{
				var detail = await _getUseCase.GetDetailAsync(id, ct);
				if (detail is null)
					return NotFound(new { message = "Solicitud no encontrada." });

				if (detail.Request.Status is not ("COMPLETADO" or "RECHAZADO"))
					return BadRequest(new { message = "Solo se puede descargar la respuesta de solicitudes finalizadas." });

				var pdfBytes = _pdfService.Generate(detail);
				var fileName = $"respuesta-arco-{id.ToString()[..8].ToUpper()}.pdf";
				return File(pdfBytes, "application/pdf", fileName);
			}
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}
	}
}