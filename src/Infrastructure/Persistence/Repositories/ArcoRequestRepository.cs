using Microsoft.EntityFrameworkCore;
using SecureHub.Application.Interfaces;
using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public class ArcoRequestRepository : IArcoRequestRepository
	{
		private readonly SecureHubDbContext _db;
		public ArcoRequestRepository(SecureHubDbContext db) => _db = db;

		public async Task AddAsync(ArcoRequest request, CancellationToken ct = default)
			=> await _db.ArcoRequests.AddAsync(request, ct);

		public async Task<ArcoRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
			=> await _db.ArcoRequests
				.Include(r => r.Subject)
				.FirstOrDefaultAsync(r => r.Id == id, ct);

		public async Task<IEnumerable<ArcoRequest>> GetBySubjectIdAsync(
			Guid subjectId, CancellationToken ct = default)
			=> await _db.ArcoRequests
				.Where(r => r.SubjectId == subjectId)
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync(ct);

		public async Task<IEnumerable<ArcoRequest>> GetAllAsync(CancellationToken ct = default)
			=> await _db.ArcoRequests
				.Include(r => r.Subject)
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync(ct);

		public async Task<IEnumerable<ArcoRequest>> GetByStatusAsync(
			string status, CancellationToken ct = default)
			=> await _db.ArcoRequests
				.Include(r => r.Subject)
				.Where(r => r.Status == status)
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync(ct);
	}
}