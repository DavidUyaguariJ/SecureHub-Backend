using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Dashboard.Dtos;
using SecureHub.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard
{
	public class GetDashboardSummaryUseCase
	{
		private readonly IDashboardRepository _repo;

		public GetDashboardSummaryUseCase(IDashboardRepository repo) => _repo = repo;

		public async Task<DashboardSummaryDto> ExecuteAsync(CancellationToken ct = default)
		{
			var arco = await _repo.GetArcoStatsAsync(ct);
			var contracts = await _repo.GetContractStatsAsync(ct);
			var subjects = await _repo.GetSubjectStatsAsync(ct);
			double complianceRate = arco.TotalFinalized > 0 ? Math.Round((double)arco.OnTimeFinalized / arco.TotalFinalized * 100, 1): 0;
			return new DashboardSummaryDto(
				arco.Total, arco.Pending, arco.InProcess, arco.Completed, arco.Rejected, arco.Overdue,
				Math.Round(arco.AvgResolutionDays, 1), complianceRate,
				contracts.Active, contracts.Expired, contracts.Revoked,
				subjects.TotalSubjects, subjects.WithBiometrics);
		}
	}
}