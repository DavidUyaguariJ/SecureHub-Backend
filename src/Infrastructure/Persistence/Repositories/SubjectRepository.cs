using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public class SubjectRepository : ISubjectRepository
	{
		private readonly SecureHubDbContext _context;

		public SubjectRepository(SecureHubDbContext context)
		{
			_context = context;
		}

		public async Task<Subject?> GetByIdAsync(Guid id)
			=> await _context.Subjects.FindAsync(id);

		public async Task<Subject?> GetByIdentificationAsync(string identification) => await _context.Subjects.FirstOrDefaultAsync(s => s.Identification == identification);

		public async Task<Subject?> GetByEmailAsync(string email) => await _context.Subjects.FirstOrDefaultAsync(s => s.Email == email);

		public async Task AddAsync(Subject subject) => await _context.Subjects.AddAsync(subject);

		public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
	}
}
