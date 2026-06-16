using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Dashboard.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard
{
	public class GetDashboardAlertsUseCase
	{
		private readonly IDashboardRepository _repo;
		public GetDashboardAlertsUseCase(IDashboardRepository repo) => _repo = repo;

		public async Task<IEnumerable<ArcoAlertDto>> GetArcoAlertsAsync(CancellationToken ct = default)
		{
			var now = DateTimeOffset.UtcNow;
			var threshold = now.AddDays(3);
			var raw = await _repo.GetArcoAlertsAsync(threshold, ct);

			return raw.Select(r =>
			{
				var daysOverdue = r.DueDate < now? (int)(now - r.DueDate).TotalDays: -(int)(r.DueDate - now).TotalDays;

				return new ArcoAlertDto(
					r.Id, r.SubjectFullName ?? "—", r.RequestType,
					r.Status, r.DueDate, daysOverdue, r.DueDate < now);
			});
		}

		public Task<IEnumerable<ContractAlertDto>> GetContractAlertsAsync(CancellationToken ct = default)
			=> _repo.GetContractAlertsAsync(DateTimeOffset.UtcNow.AddDays(30), ct);
	}
}
