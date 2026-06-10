using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public interface IPartContractRepository
	{
		Task AddAsync(PartContract contract, CancellationToken ct = default);
		Task UpdateAsync(PartContract contract, CancellationToken ct = default);
		Task<PartContract?> GetByIdAsync(Guid id, CancellationToken ct = default);
		Task<IEnumerable<PartContract>> GetAllAsync(string? statusFilter = null, CancellationToken ct = default);
		Task<PartContract?> GetByKeycloakUserIdAsync(string keycloakUserId, CancellationToken ct = default);
		Task<PartContract?> GetActiveByKeycloakUsernameAsync(string keycloakUsername, Guid subjectId, CancellationToken ct = default);
	}
}
