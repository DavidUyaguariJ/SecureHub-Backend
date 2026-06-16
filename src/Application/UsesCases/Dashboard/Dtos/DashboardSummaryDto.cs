using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard.Dtos
{
	public record DashboardSummaryDto(
		int TotalArcoRequests,
		int PendingArcoRequests,
		int InProcessArcoRequests,
		int CompletedArcoRequests,
		int RejectedArcoRequests,
		int OverdueArcoRequests,
		double AverageResolutionDays,
		double ComplianceRate,
		int ActivePartContracts,
		int ExpiredPartContracts,
		int RevokedPartContracts,
		int TotalSubjects,
		int SubjectsWithBiometrics
	);
}
