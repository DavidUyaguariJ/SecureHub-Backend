using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IArcoAuditLogRepository
	{
		Task AddAsync(ArcoAuditLog log, CancellationToken ct = default);
		Task<IEnumerable<ArcoAuditLog>> GetByRequestIdAsync(Guid requestId, CancellationToken ct = default);
	}
}
