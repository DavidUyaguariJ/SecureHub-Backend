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
	}
}
