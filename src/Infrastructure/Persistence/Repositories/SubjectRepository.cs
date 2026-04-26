using Microsoft.EntityFrameworkCore;
using SecureHub.Application.Interfaces;
using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public class SubjectRepository : ISubjectRepository
	{
		private readonly SecureHubDbContext _context;
		private readonly IEncryptionService _encryptionService;

		public SubjectRepository(SecureHubDbContext context, IEncryptionService encryptionService)
		{
			_context = context;
			_encryptionService = encryptionService;
		}
		public async Task<Subject?> GetByIdAsync(Guid id)
			=> await _context.Subjects.FindAsync(id);

		public async Task<Subject?> GetByIdentificationAsync(string identification)
			=> await _context.Subjects.FirstOrDefaultAsync(s => s.Identification == identification);

		public async Task<Subject?> GetByEmailAsync(string email)
			=> await _context.Subjects.FirstOrDefaultAsync(s => s.Email == email);

		public async Task AddAsync(Subject subject)
			=> await _context.Subjects.AddAsync(subject);

		public async Task SaveChangesAsync()
			=> await _context.SaveChangesAsync();
		public async Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct)
			=> await _context.Subjects.FindAsync([id], ct);

		public async Task<Subject?> GetByIdentificationAsync(string identification, CancellationToken ct)
			=> await _context.Subjects.FirstOrDefaultAsync(s => s.Identification == identification, ct);
		public async Task<Subject?> FindByDecryptedIdentificationAsync(
			string identification, CancellationToken ct)
		{
			var candidates = await _context.Subjects
				.AsNoTracking()
				.Select(s => new { s.Id, s.Identification })
				.ToListAsync(ct);

			Guid? matchId = null;
			foreach (var c in candidates)
			{
				try
				{
					var decrypted = _encryptionService.Decrypt(c.Identification);
					if (decrypted == identification)
					{
						matchId = c.Id;
						break;
					}
				}
				catch
				{
				}
			}

			if (matchId is null) return null;
			return await _context.Subjects.FindAsync([matchId.Value], ct);
		}
	}
}
