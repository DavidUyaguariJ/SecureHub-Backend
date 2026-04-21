using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public class BiometricAuthRepository : IBiometricAuthRepository
	{
		private readonly SecureHubDbContext _context;

		public BiometricAuthRepository(SecureHubDbContext context)
		{
			_context = context;
		}

		public async Task AddAsync(BiometricAuth biometric) => await _context.BiometricAuths.AddAsync(biometric);

		public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
	}
}
