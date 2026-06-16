using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard.Dtos
{
	public record ArcoMonthlyTrendRaw(
		int Year, int Month, int Pending, int InProcess, int Completed, int Rejected);
}
