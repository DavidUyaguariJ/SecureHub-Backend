using Microsoft.EntityFrameworkCore;
using SecureHub.Application.Interfaces;
using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public class PartContractRepository : IPartContractRepository
	{
		private readonly SecureHubDbContext _db;
		public PartContractRepository(SecureHubDbContext db) => _db = db;

		public async Task AddAsync(PartContract contract, CancellationToken ct = default)
			=> await _db.PartContracts.AddAsync(contract, ct);

		public async Task UpdateAsync(PartContract contract, CancellationToken ct = default)
		{
			_db.PartContracts.Update(contract);
			await Task.CompletedTask;
		}

		public async Task<PartContract?> GetByIdAsync(Guid id, CancellationToken ct = default)
			=> await _db.PartContracts.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

		public async Task<IEnumerable<PartContract>> GetAllAsync(
			string? statusFilter = null, CancellationToken ct = default)
		{
			var query = _db.PartContracts
				.Where(c => !c.IsDeleted);

			if (!string.IsNullOrEmpty(statusFilter))
				query = query.Where(c => c.Status == statusFilter);

			return await query.OrderBy(c => c.Status == "VENCIDO" ? 0 : c.Status == "REVOCADO" ? 1 : c.Status == "SUSPENDIDO" ? 2 : 3)
				.ThenByDescending(c => c.CreatedAt).ToListAsync(ct);
		}
		public async Task<PartContract?> GetActiveByKeycloakUsernameAsync(string keycloakUsername, Guid subjectId, CancellationToken ct = default)
			=> await _db.PartContracts.FirstOrDefaultAsync(p =>
					!p.IsDeleted &&
					p.KeycloakUsername == keycloakUsername &&
					p.SubjectId == subjectId &&
					p.Status == "ACTIVO" &&
					p.ValidUntil >= DateTimeOffset.UtcNow, ct);
		public async Task<PartContract?> GetByKeycloakUserIdAsync(
			string keycloakUserId, CancellationToken ct = default)
			=> await _db.PartContracts
				.FirstOrDefaultAsync(c => c.KeycloakUserId == keycloakUserId && !c.IsDeleted, ct);
	}
}
