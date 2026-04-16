using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Domain.Entities
{
	public class DeviceCredential
	{
		public Guid Id { get; private set; }
		public Guid DeviceId { get; private set; }
		public string? SystemUser { get; private set; }
		public byte[] EncryptedPassword { get; private set; }
		public byte[] EncryptionIV { get; private set; }
		public DateTime UpdatedAt { get; private set; }

		public Device Device { get; private set; } = null!;

		private DeviceCredential() { }

		public static DeviceCredential Create(
			Guid deviceId,
			byte[] encryptedPassword,
			byte[] encryptionIV,
			string? systemUser = null)
		{
			return new DeviceCredential
			{
				Id = Guid.NewGuid(),
				DeviceId = deviceId,
				SystemUser = systemUser,
				EncryptedPassword = encryptedPassword,
				EncryptionIV = encryptionIV,
				UpdatedAt = DateTime.UtcNow
			};
		}
	}
}
