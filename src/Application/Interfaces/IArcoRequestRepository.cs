using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public interface IArcoRequestRepository
	{
		Task AddAsync(ArcoRequest request, CancellationToken ct = default);
		Task<ArcoRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
		Task<IEnumerable<ArcoRequest>> GetBySubjectIdAsync(Guid subjectId, CancellationToken ct = default);
		Task<IEnumerable<ArcoRequest>> GetAllAsync(CancellationToken ct = default);
		Task<IEnumerable<ArcoRequest>> GetByStatusAsync(string status, CancellationToken ct = default);
	}

}
