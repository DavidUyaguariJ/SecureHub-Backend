using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureHub.Application.UsesCases.Dashboard;

namespace SecureHub.Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "admin_api_role,technician_api_role")]
	public class DashboardController : ControllerBase
	{
		private readonly GetDashboardSummaryUseCase _summary;
		private readonly GetArcoDashboardChartsUseCase _charts;
		private readonly GetDashboardAlertsUseCase _alerts;
		private readonly GetImmutableAuditLogUseCase _auditLog;

		public DashboardController(
			GetDashboardSummaryUseCase summary,
			GetArcoDashboardChartsUseCase charts,
			GetDashboardAlertsUseCase alerts,
			GetImmutableAuditLogUseCase auditLog)
		{
			_summary = summary;
			_charts = charts;
			_alerts = alerts;
			_auditLog = auditLog;
		}

		[HttpGet("summary")]
		public async Task<IActionResult> GetSummary(CancellationToken ct)
		{
			try { return Ok(await _summary.ExecuteAsync(ct)); }
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpGet("arco/by-type")]
		public async Task<IActionResult> GetArcoByType(CancellationToken ct)
		{
			try { return Ok(await _charts.GetByTypeAsync(ct)); }
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpGet("arco/trend")]
		public async Task<IActionResult> GetArcoTrend([FromQuery] int months = 6, CancellationToken ct = default)
		{
			try { return Ok(await _charts.GetMonthlyTrendAsync(months, ct)); }
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpGet("alerts/arco")]
		public async Task<IActionResult> GetArcoAlerts(CancellationToken ct)
		{
			try { return Ok(await _alerts.GetArcoAlertsAsync(ct)); }
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpGet("alerts/contracts")]
		public async Task<IActionResult> GetContractAlerts(CancellationToken ct)
		{
			try { return Ok(await _alerts.GetContractAlertsAsync(ct)); }
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpGet("audit-log")]
		public async Task<IActionResult> GetAuditLog(
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 20,
			[FromQuery] string? action = null,
			[FromQuery] DateTimeOffset? from = null,
			[FromQuery] DateTimeOffset? to = null,
			CancellationToken ct = default)
		{
			try { return Ok(await _auditLog.ExecuteAsync(page, pageSize, action, from, to, ct)); }
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}
	}
}