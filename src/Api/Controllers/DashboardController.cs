using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureHub.Application.Interfaces;
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
		private readonly IDashboardReportService _dashboardReport;
		private readonly IAuditLogReportService _auditLogReport;

		public DashboardController(
			GetDashboardSummaryUseCase summary,
			GetArcoDashboardChartsUseCase charts,
			GetDashboardAlertsUseCase alerts,
			GetImmutableAuditLogUseCase auditLog,
			IDashboardReportService dashboardReport,
			IAuditLogReportService auditLogReport)
		{
			_summary = summary;
			_charts = charts;
			_alerts = alerts;
			_auditLog = auditLog;
			_dashboardReport = dashboardReport;
			_auditLogReport = auditLogReport;
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

		[HttpGet("summary/report/pdf")]
		public async Task<IActionResult> DownloadSummaryPdf(CancellationToken ct)
		{
			try
			{
				var (summary, byType, trend, arcoAlerts, contractAlerts) = await LoadSummaryDataAsync(ct);
				var bytes = _dashboardReport.GenerateSummaryPdf(summary, byType, trend, arcoAlerts, contractAlerts);
				return File(bytes, "application/pdf",
					$"reporte-cumplimiento-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
			}
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpGet("summary/report/excel")]
		public async Task<IActionResult> DownloadSummaryExcel(CancellationToken ct)
		{
			try
			{
				var (summary, byType, trend, arcoAlerts, contractAlerts) = await LoadSummaryDataAsync(ct);
				var bytes = _dashboardReport.GenerateSummaryExcel(summary, byType, trend, arcoAlerts, contractAlerts);
				return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
					$"reporte-cumplimiento-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		private async Task<(
			SecureHub.Application.UsesCases.Dashboard.Dtos.DashboardSummaryDto Summary,
			IEnumerable<SecureHub.Application.UsesCases.Dashboard.Dtos.ArcoByTypeDto> ByType,
			IEnumerable<SecureHub.Application.UsesCases.Dashboard.Dtos.ArcoMonthlyTrendDto> Trend,
			IEnumerable<SecureHub.Application.UsesCases.Dashboard.Dtos.ArcoAlertDto> ArcoAlerts,
			IEnumerable<SecureHub.Application.UsesCases.Dashboard.Dtos.ContractAlertDto> ContractAlerts
		)> LoadSummaryDataAsync(CancellationToken ct)
		{
			var summary = await _summary.ExecuteAsync(ct);
			var byType = await _charts.GetByTypeAsync(ct);
			var trend = await _charts.GetMonthlyTrendAsync(6, ct);
			var arcoAlerts = await _alerts.GetArcoAlertsAsync(ct);
			var contractAlerts = await _alerts.GetContractAlertsAsync(ct);
			return (summary, byType, trend, arcoAlerts, contractAlerts);
		}

		[HttpGet("audit-log/report/pdf")]
		public async Task<IActionResult> DownloadAuditLogPdf(
			[FromQuery] string? action = null,
			[FromQuery] DateTimeOffset? from = null,
			[FromQuery] DateTimeOffset? to = null,
			CancellationToken ct = default)
		{
			try
			{
				var page = await _auditLog.ExecuteAsync(1, 5000, action, from, to, ct);
				var bytes = _auditLogReport.GeneratePdf(page.Items);
				return File(bytes, "application/pdf",
					$"bitacora-auditoria-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
			}
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}

		[HttpGet("audit-log/report/excel")]
		public async Task<IActionResult> DownloadAuditLogExcel(
			[FromQuery] string? action = null,
			[FromQuery] DateTimeOffset? from = null,
			[FromQuery] DateTimeOffset? to = null,
			CancellationToken ct = default)
		{
			try
			{
				var page = await _auditLog.ExecuteAsync(1, 5000, action, from, to, ct);
				var bytes = _auditLogReport.GenerateExcel(page.Items);
				return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
					$"bitacora-auditoria-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
		}
	}
}