using System;

namespace SecureHub.Domain.Entities
{
	public class DeviceCredential
	{
		public Guid Id { get; private set; }
		public Guid DeviceId { get; private set; }
		public string? SystemUser { get; private set; }
		public string EncryptedPassword { get; private set; } = null!;
		public string? EncryptionIV { get; private set; }
		public DateTime UpdatedAt { get; private set; }

		public Device Device { get; private set; } = null!;

		private DeviceCredential() { }

		public static DeviceCredential CreateRsa(
			Guid deviceId,
			string encryptedPassword,
			string? systemUser = null)
		{
			return new DeviceCredential
			{
				Id = Guid.NewGuid(),
				DeviceId = deviceId,
				EncryptedPassword = encryptedPassword,
				SystemUser = systemUser,
				EncryptionIV = null,
				UpdatedAt = DateTime.UtcNow
			};
		}
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
				EncryptedPassword = Convert.ToBase64String(encryptedPassword),
				EncryptionIV = Convert.ToBase64String(encryptionIV),
				SystemUser = systemUser,
				UpdatedAt = DateTime.UtcNow
			};
		}
	}
}