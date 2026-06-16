using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Dashboard.Dtos;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard
{
	public class GetImmutableAuditLogUseCase
	{
		private readonly IDashboardRepository _repo;
		public GetImmutableAuditLogUseCase(IDashboardRepository repo) => _repo = repo;

		public async Task<ImmutableAuditPageDto> ExecuteAsync(
			int page = 1, int pageSize = 20,
			string? action = null,
			DateTimeOffset? from = null,
			DateTimeOffset? to = null,
			CancellationToken ct = default)
		{
			int total = await _repo.CountAuditLogsAsync(action, from, to, ct);
			var logs = await _repo.GetAuditLogsPagedAsync(action, from, to, page, pageSize, ct);

			var items = logs.Select(l =>
			{
				var raw = $"{l.Id}|{l.ArcoRequestId}|{l.Action}|{l.PreviousStatus}|{l.NewStatus}|{l.PerformedBy}|{l.PerformedByRole}|{l.Notes}|{l.IpAddress}|{l.CreatedAt:O}";
				var hash = ComputeSha256(raw);
				bool anchored = !string.IsNullOrEmpty(l.Notes) && l.Notes.Contains("tx:");

				return new ImmutableAuditEventDto(
					l.Id, l.ArcoRequestId, l.Action, l.PreviousStatus, l.NewStatus,
					l.PerformedByName, l.PerformedByRole, l.Notes, l.IpAddress,
					l.CreatedAt, hash, anchored);
			});

			return new ImmutableAuditPageDto(items, total, page, pageSize);
		}

		private static string ComputeSha256(string input)
		{
			var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
			return "sha256:" + Convert.ToHexString(bytes).ToLower();
		}
	}
}
