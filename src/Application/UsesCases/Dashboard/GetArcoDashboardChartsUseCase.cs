using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Dashboard.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard
{
	public class GetArcoDashboardChartsUseCase
	{
		private readonly IDashboardRepository _repo;
		public GetArcoDashboardChartsUseCase(IDashboardRepository repo) => _repo = repo;

		public Task<IEnumerable<ArcoByTypeDto>> GetByTypeAsync(CancellationToken ct = default)
			=> _repo.GetArcoByTypeAsync(ct);

		public async Task<IEnumerable<ArcoMonthlyTrendDto>> GetMonthlyTrendAsync(
			int months = 6, CancellationToken ct = default)
		{
			var since = DateTimeOffset.UtcNow.AddMonths(-months + 1);
			var raw = await _repo.GetArcoMonthlyTrendRawAsync(since, ct);

			return raw.Select(g => new ArcoMonthlyTrendDto(
				$"{g.Year}-{g.Month:D2}",
				g.Pending, g.InProcess, g.Completed, g.Rejected));
		}
	}
}
