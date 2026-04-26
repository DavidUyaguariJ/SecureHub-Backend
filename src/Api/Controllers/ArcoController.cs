using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

		public ArcoRequestController(
			CreateArcoRequestUseCase createUseCase,
			UpdateArcoStatusUseCase updateStatusUseCase,
			GetArcoRequestsUseCase getUseCase,
			LookupSubjectUseCase lookupUseCase)
		{
			_createUseCase = createUseCase;
			_updateStatusUseCase = updateStatusUseCase;
			_getUseCase = getUseCase;
			_lookupUseCase = lookupUseCase;
		}

		[HttpGet("subject/lookup")]
		[Authorize(Roles = "admin_api_role,operador_arco")]
		[ProducesResponseType(typeof(SubjectLookupDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> LookupSubject(
			[FromQuery] string identification,
			CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(identification))
				return BadRequest(new { message = "La identificación es requerida." });

			var result = await _lookupUseCase.ExecuteAsync(identification, ct);
			if (result is null)
				return NotFound(new { message = "No se encontró ningún titular con esa identificación." });

			return Ok(result);
		}

		[HttpPost]
		[Authorize(Roles = "admin_api_role,operador_arco")]
		[ProducesResponseType(typeof(ArcoRequestResponseDto), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public async Task<IActionResult> Create(
			[FromBody] CreateArcoRequestDto dto,
			CancellationToken ct)
		{
			try
			{
				var requesterIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
				var result = await _createUseCase.ExecuteAsync(dto, requesterIp, ct);
				return CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
			}
			catch (UnauthorizedAccessException ex)
			{
				return Unauthorized(new { message = ex.Message });
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
			}
		}

		[HttpGet]
		[Authorize(Roles = "admin_api_role")]
		[ProducesResponseType(typeof(IEnumerable<ArcoRequestResponseDto>), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetAll(
			[FromQuery] string? status,
			CancellationToken ct)
		{
			try
			{
				var result = await _getUseCase.GetAllAsync(status, ct);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
			}
		}

		[HttpGet("{id:guid}")]
		[Authorize(Roles = "admin_api_role,operador_arco")]
		[ProducesResponseType(typeof(ArcoRequestDetailDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
		{
			try
			{
				var result = await _getUseCase.GetDetailAsync(id, ct);
				if (result is null) return NotFound(new { message = "Solicitud no encontrada." });
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
			}
		}

		[HttpGet("subject/{subjectId:guid}")]
		[Authorize(Roles = "admin_api_role,operador_arco")]
		[ProducesResponseType(typeof(IEnumerable<ArcoRequestResponseDto>), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetBySubject(Guid subjectId, CancellationToken ct)
		{
			try
			{
				var result = await _getUseCase.GetBySubjectAsync(subjectId, ct);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
			}
		}

		[HttpPatch("{id:guid}/status")]
		[Authorize(Roles = "admin_api_role")]
		[ProducesResponseType(typeof(ArcoRequestResponseDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> UpdateStatus(
			Guid id,
			[FromBody] UpdateArcoStatusDto dto,
			CancellationToken ct)
		{
			try
			{
				var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
					?? User.FindFirstValue("sub");
				Guid operatorId = Guid.TryParse(subClaim, out var parsed)
					? parsed
					: Guid.Empty;

				var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
				var result = await _updateStatusUseCase.ExecuteAsync(id, dto, operatorId, ip, ct);
				return Ok(result);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
			}
		}
	}
}
