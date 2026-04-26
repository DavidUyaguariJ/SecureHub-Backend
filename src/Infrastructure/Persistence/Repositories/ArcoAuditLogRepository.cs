using Microsoft.EntityFrameworkCore;
using SecureHub.Application.Interfaces;
using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public class ArcoAuditLogRepository : IArcoAuditLogRepository
	{
		private readonly SecureHubDbContext _db;
		public ArcoAuditLogRepository(SecureHubDbContext db) => _db = db;

		public async Task AddAsync(ArcoAuditLog log, CancellationToken ct = default)
			=> await _db.ArcoAuditLogs.AddAsync(log, ct);

		public async Task<IEnumerable<ArcoAuditLog>> GetByRequestIdAsync(
			Guid requestId, CancellationToken ct = default)
			=> await _db.ArcoAuditLogs
				.Where(l => l.ArcoRequestId == requestId)
				.OrderBy(l => l.CreatedAt)
				.ToListAsync(ct);
	}
}
