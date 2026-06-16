using Microsoft.EntityFrameworkCore;
using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Dashboard.Dtos;
using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public class DashboardRepository : IDashboardRepository
	{
		private readonly SecureHubDbContext _db;

		public DashboardRepository(SecureHubDbContext db) => _db = db;

		public async Task<ArcoStatsRaw> GetArcoStatsAsync(CancellationToken ct = default)
		{
			var now = DateTimeOffset.UtcNow;

			// Counts por status: usa idx_arco_requests_status
			var total = await _db.ArcoRequests.CountAsync(ct);
			var pending = await _db.ArcoRequests.CountAsync(r => r.Status == "PENDIENTE", ct);
			var inProcess = await _db.ArcoRequests.CountAsync(r => r.Status == "EN_PROCESO", ct);
			var completed = await _db.ArcoRequests.CountAsync(r => r.Status == "COMPLETADO", ct);
			var rejected = await _db.ArcoRequests.CountAsync(r => r.Status == "RECHAZADO", ct);

			var overdue = await _db.ArcoRequests.CountAsync(r =>
				(r.Status == "PENDIENTE" || r.Status == "EN_PROCESO") &&
				r.DueDate.HasValue && r.DueDate.Value < now, ct);

			// Promedio de días de resolución calculado en SQL (EF traduce el .Average sobre TotalDays)
			var avgDays = await _db.ArcoRequests
				.Where(r => r.ResolvedAt.HasValue)
				.Select(r => (r.ResolvedAt!.Value - r.RequestedAt).TotalDays)
				.DefaultIfEmpty(0)
				.AverageAsync(ct);

			var totalFinalized = await _db.ArcoRequests
				.CountAsync(r => r.Status == "COMPLETADO" || r.Status == "RECHAZADO", ct);

			var onTimeFinalized = await _db.ArcoRequests.CountAsync(r =>
				(r.Status == "COMPLETADO" || r.Status == "RECHAZADO") &&
				(!r.DueDate.HasValue || r.ResolvedAt == null || r.ResolvedAt.Value <= r.DueDate.Value), ct);

			return new ArcoStatsRaw(
				total, pending, inProcess, completed, rejected, overdue,
				avgDays, onTimeFinalized, totalFinalized);
		}

		public async Task<ContractStatsRaw> GetContractStatsAsync(CancellationToken ct = default)
		{
			var active = await _db.PartContracts.CountAsync(c => c.Status == "ACTIVO", ct);
			var expired = await _db.PartContracts.CountAsync(c => c.Status == "VENCIDO", ct);
			var revoked = await _db.PartContracts.CountAsync(c => c.Status == "REVOCADO", ct);
			return new ContractStatsRaw(active, expired, revoked);
		}

		public async Task<SubjectStatsRaw> GetSubjectStatsAsync(CancellationToken ct = default)
		{
			var totalSubjects = await _db.Subjects.CountAsync(ct);
			var withBiometrics = await _db.BiometricAuths
				.Select(b => b.SubjectId)
				.Distinct()
				.CountAsync(ct);
			return new SubjectStatsRaw(totalSubjects, withBiometrics);
		}

		public async Task<IEnumerable<ArcoByTypeDto>> GetArcoByTypeAsync(CancellationToken ct = default)
		{
			return await _db.ArcoRequests
				.GroupBy(r => r.RequestType)
				.Select(g => new ArcoByTypeDto(g.Key, g.Count()))
				.ToListAsync(ct);
		}

		public async Task<IEnumerable<ArcoMonthlyTrendRaw>> GetArcoMonthlyTrendRawAsync(
			DateTimeOffset since, CancellationToken ct = default)
		{
			return await _db.ArcoRequests
				.Where(r => r.RequestedAt >= since)
				.GroupBy(r => new { r.RequestedAt.Year, r.RequestedAt.Month })
				.OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
				.Select(g => new ArcoMonthlyTrendRaw(
					g.Key.Year, g.Key.Month,
					g.Count(r => r.Status == "PENDIENTE"),
					g.Count(r => r.Status == "EN_PROCESO"),
					g.Count(r => r.Status == "COMPLETADO"),
					g.Count(r => r.Status == "RECHAZADO")))
				.ToListAsync(ct);
		}

		public async Task<IEnumerable<ArcoAlertRaw>> GetArcoAlertsAsync(
			DateTimeOffset threshold, CancellationToken ct = default)
		{
			return await _db.ArcoRequests
				.Include(r => r.Subject)
				.Where(r =>
					(r.Status == "PENDIENTE" || r.Status == "EN_PROCESO") &&
					r.DueDate.HasValue && r.DueDate.Value <= threshold)
				.OrderBy(r => r.DueDate)
				.Select(r => new ArcoAlertRaw(
					r.Id, r.Subject != null ? r.Subject.FullName : null,
					r.RequestType, r.Status, r.DueDate!.Value))
				.ToListAsync(ct);
		}

		public async Task<IEnumerable<ContractAlertDto>> GetContractAlertsAsync(
			DateTimeOffset threshold, CancellationToken ct = default)
		{
			var now = DateTimeOffset.UtcNow;
			var contracts = await _db.PartContracts
				.Where(c => c.Status == "ACTIVO" && c.ValidUntil <= threshold)
				.OrderBy(c => c.ValidUntil)
				.ToListAsync(ct);

			return contracts.Select(c => new ContractAlertDto(
				c.Id, c.CompanyName, c.ValidUntil,
				(int)(c.ValidUntil - now).TotalDays, c.Status));
		}

		public async Task<int> CountAuditLogsAsync(
			string? action, DateTimeOffset? from, DateTimeOffset? to,
			CancellationToken ct = default)
		{
			return await BuildAuditQuery(action, from, to).CountAsync(ct);
		}

		public async Task<IEnumerable<ArcoAuditLog>> GetAuditLogsPagedAsync(
			string? action, DateTimeOffset? from, DateTimeOffset? to,
			int page, int pageSize, CancellationToken ct = default)
		{
			return await BuildAuditQuery(action, from, to)
				.OrderByDescending(l => l.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync(ct);
		}

		private IQueryable<ArcoAuditLog> BuildAuditQuery(
			string? action, DateTimeOffset? from, DateTimeOffset? to)
		{
			var query = _db.ArcoAuditLogs.AsQueryable();
			if (!string.IsNullOrWhiteSpace(action))
				query = query.Where(l => l.Action == action);
			if (from.HasValue)
				query = query.Where(l => l.CreatedAt >= from.Value);
			if (to.HasValue)
				query = query.Where(l => l.CreatedAt <= to.Value);
			return query;
		}
	}
}
