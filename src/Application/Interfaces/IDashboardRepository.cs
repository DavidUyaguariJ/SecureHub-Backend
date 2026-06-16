using SecureHub.Application.UsesCases.Dashboard.Dtos;
using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IDashboardRepository
	{
		Task<ArcoStatsRaw> GetArcoStatsAsync(CancellationToken ct = default);
		Task<ContractStatsRaw> GetContractStatsAsync(CancellationToken ct = default);
		Task<SubjectStatsRaw> GetSubjectStatsAsync(CancellationToken ct = default);

		Task<IEnumerable<ArcoByTypeDto>> GetArcoByTypeAsync(CancellationToken ct = default);
		Task<IEnumerable<ArcoMonthlyTrendRaw>> GetArcoMonthlyTrendRawAsync(
			DateTimeOffset since, CancellationToken ct = default);

		Task<IEnumerable<ArcoAlertRaw>> GetArcoAlertsAsync(
			DateTimeOffset threshold, CancellationToken ct = default);
		Task<IEnumerable<ContractAlertDto>> GetContractAlertsAsync(
			DateTimeOffset threshold, CancellationToken ct = default);

		Task<int> CountAuditLogsAsync(
			string? action, DateTimeOffset? from, DateTimeOffset? to,
			CancellationToken ct = default);
		Task<IEnumerable<ArcoAuditLog>> GetAuditLogsPagedAsync(
			string? action, DateTimeOffset? from, DateTimeOffset? to,
			int page, int pageSize, CancellationToken ct = default);
	}
}
