using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public interface IBiometricAuthRepository
	{
		Task AddAsync(BiometricAuth biometric);
		Task SaveChangesAsync();
	}
}
