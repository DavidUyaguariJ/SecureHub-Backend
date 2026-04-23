using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Domain.Entities
{
	public class Device
	{
		public Guid Id { get; private set; }
		public Guid SubjectId { get; private set; }
		public string DeviceType { get; private set; } = null!;
		public string? Brand { get; private set; }
		public string? Model { get; private set; }
		public string? SerialNumber { get; private set; }
		public bool IsDeleted { get; private set; }
		public DateTime? DeletedAt { get; private set; }
		public DateTime CreatedAt { get; private set; }
		public Subject Subject { get; private set; } = null!;
		public DeviceCredential? Credential { get; private set; }

		private Device() { }

		public static Device Create(
			Guid subjectId,
			string deviceType,
			string? brand = null,
			string? model = null,
			string? serialNumber = null)
		{
			return new Device
			{
				Id = Guid.NewGuid(),
				SubjectId = subjectId,
				DeviceType = deviceType,
				Brand = brand,
				Model = model,
				SerialNumber = serialNumber,
				IsDeleted = false,
				CreatedAt = DateTime.UtcNow
			};
		}

		public void SoftDelete()
		{
			IsDeleted = true;
			DeletedAt = DateTime.UtcNow;
		}
	}
}