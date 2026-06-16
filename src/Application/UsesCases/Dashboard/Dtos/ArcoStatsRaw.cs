using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard.Dtos
{
	public record ArcoStatsRaw(int Total, int Pending, int InProcess, int Completed, int Rejected, int Overdue,
		double AvgResolutionDays, int OnTimeFinalized, int TotalFinalized);
}
