using SecureHub.Application.UsesCases.Dashboard.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IDashboardReportService
	{
		byte[] GenerateSummaryPdf(
			DashboardSummaryDto summary,
			IEnumerable<ArcoByTypeDto> byType,
			IEnumerable<ArcoMonthlyTrendDto> trend,
			IEnumerable<ArcoAlertDto> arcoAlerts,
			IEnumerable<ContractAlertDto> contractAlerts);

		byte[] GenerateSummaryExcel(
			DashboardSummaryDto summary,
			IEnumerable<ArcoByTypeDto> byType,
			IEnumerable<ArcoMonthlyTrendDto> trend,
			IEnumerable<ArcoAlertDto> arcoAlerts,
			IEnumerable<ContractAlertDto> contractAlerts);
	}
}
