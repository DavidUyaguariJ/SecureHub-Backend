using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Domain.Entities
{
	public class Device
	{
		public Guid Id { get; private set; }
		public Guid SubjectId { get; private set; }
		public string DeviceType { get; private set; }
		public string? Brand { get; private set; }
		public string? Model { get; private set; }
		public string? SerialNumber { get; private set; }
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
			if (subjectId == Guid.Empty)
				throw new ArgumentException("SubjectId es requerido");
			if (string.IsNullOrWhiteSpace(deviceType))
				throw new ArgumentException("DeviceType es requerido");

			return new Device
			{
				Id = Guid.NewGuid(),
				SubjectId = subjectId,
				DeviceType = deviceType,
				Brand = brand,
				Model = model,
				SerialNumber = serialNumber,
				CreatedAt = DateTime.UtcNow
			};
		}
	}
}
