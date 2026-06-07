using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Arco;
using SecureHub.Application.UsesCases.ThirdPart;
using SecureHub.Application.UsesCases.ThirdPart.Dtos;
using System.Security.Claims;

namespace SecureHub.Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "admin_api_role")]
	public class PartContractController : ControllerBase
	{
		private readonly CreatePartContractUseCase _create;
		private readonly RevokePartContractUseCase _revoke;
		private readonly GetPartContractsUseCase _get;
		private readonly IPartContractPdfService _pdf;
		private readonly LookupSubjectUseCase _lookup;
		private readonly RenewPartContractUseCase _renew;

		public PartContractController(
			CreatePartContractUseCase create,
			RevokePartContractUseCase revoke,
			GetPartContractsUseCase get,
			IPartContractPdfService pdf,
			LookupSubjectUseCase lookup,
			RenewPartContractUseCase renew
			)
		{
			_create = create;
			_revoke = revoke;
			_get = get;
			_pdf = pdf;
			_lookup = lookup;
			_renew = renew;
		}

		[HttpGet]
		public async Task<IActionResult> GetAll(
			[FromQuery] string? status, CancellationToken ct)
			=> Ok(await _get.GetAllAsync(status, ct));

		[HttpGet("{id:guid}")]
		public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
		{
			var dto = await _get.GetByIdAsync(id, ct);
			return dto is null ? NotFound() : Ok(dto);
		}

		[HttpGet("{id:guid}/blockchain-status")]
		public async Task<IActionResult> BlockchainStatus(Guid id, CancellationToken ct)
		{
			var (active, validUntil, status) = await _get.GetBlockchainStatusAsync(id, ct);
			return Ok(new { active, validUntil, status });
		}

		[HttpPost]
		public async Task<IActionResult> Create(
			[FromBody] CreatePartContractCommand cmd, CancellationToken ct)
		{
			var operatorId = GetOperatorId();
			var result = await _create.ExecuteAsync(cmd, operatorId, ct);
			return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
		}

		[HttpPost("{id:guid}/revoke")]
		public async Task<IActionResult> Revoke(
			Guid id, [FromBody] RevokePartContractCommand cmd, CancellationToken ct)
		{
			var result = await _revoke.ExecuteAsync(id, GetOperatorId(), cmd.Reason, ct);
			return Ok(result);
		}

		[HttpGet("{id:guid}/pdf")]
		public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
		{
			var dto = await _get.GetByIdAsync(id, ct);
			if (dto is null) return NotFound();
			var (active, validUntil, bcStatus) = await _get.GetBlockchainStatusAsync(id, ct);
			var bytes = await _pdf.GenerateAsync(dto, active, validUntil, bcStatus, ct);
			return File(bytes, "application/pdf",
				$"contrato-{dto.CompanyName.Replace(" ", "_")}-{dto.Id}.pdf");
		}

		[HttpGet("available-fields")]
		public IActionResult AvailableFields() => Ok(AllowedFieldOptions.Fields);

		private Guid GetOperatorId()
		{
			var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
				   ?? User.FindFirstValue("sub")
				   ?? throw new UnauthorizedAccessException("Sin sub claim");
			return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
		}

		[HttpGet("subject-lookup")]
		public async Task<IActionResult> LookupSubject([FromQuery] string identification, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(identification))
				return BadRequest("Identificación requerida.");

			var result = await _lookup.ExecuteAsync(identification, ct);
			return result is null ? NotFound("Titular no encontrado.") : Ok(result);
		}

		[HttpPost("{id:guid}/renew")]
		public async Task<IActionResult> Renew(Guid id, [FromBody] RenewPartContractCommand cmd, CancellationToken ct)
		{
			var result = await _renew.ExecuteAsync(id, cmd.ValidFrom, cmd.ValidUntil, GetOperatorId(), ct);
			return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
		}
	}

}