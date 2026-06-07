using SecureHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Persistence.Repositories
{
	public class DeviceRepository : IDeviceRepository
	{
		private readonly SecureHubDbContext _context;

		public DeviceRepository(SecureHubDbContext context)
		{
			_context = context;
		}

		public async Task AddAsync(Device device) => await _context.Devices.AddAsync(device);

		public async Task AddCredentialAsync(DeviceCredential credential) => await _context.DeviceCredentials.AddAsync(credential);

		public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
		public async Task UpdateAsync(Device device)=>_context.Devices.Update(device);
		public async Task<IEnumerable<Device>> GetBySubjectIdAsync(Guid subjectId, CancellationToken ct= default) => await _context.Devices.Where(d => d.SubjectId == subjectId).ToListAsync(ct);
	}
}
