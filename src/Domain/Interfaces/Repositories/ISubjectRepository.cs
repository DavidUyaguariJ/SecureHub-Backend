
using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public interface ISubjectRepository
	{
		Task<Subject?> GetByIdAsync(Guid id);
		Task<Subject?> GetByIdentificationAsync(string identification);
		Task<Subject?> GetByEmailAsync(string email);
		Task AddAsync(Subject subject);
		Task SaveChangesAsync();

		Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct);
		Task<Subject?> GetByIdentificationAsync(string identification, CancellationToken ct);
		Task<Subject?> FindByDecryptedIdentificationAsync(string identification, CancellationToken ct);
	}
}
