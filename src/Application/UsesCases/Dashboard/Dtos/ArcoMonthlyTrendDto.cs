using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard.Dtos
{
	public record ArcoMonthlyTrendDto(string Month, int Pending, int InProcess, int Completed, int Rejected);
}
