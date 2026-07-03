using SecureHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public interface IDeviceRepository
	{
		Task AddAsync(Device device);
		Task AddCredentialAsync(DeviceCredential credential);
		Task SaveChangesAsync();
		Task UpdateAsync(Device device);
		Task<IEnumerable<Device>> GetBySubjectIdAsync(Guid subjectId, CancellationToken ct = default);
	}	
}
