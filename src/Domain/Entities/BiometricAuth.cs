using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Domain.Entities
{
	public class BiometricAuth
	{
		public Guid Id { get; private set; }
		public Guid SubjectId { get; private set; }
		public string? TemplateType { get; private set; }
		public byte[] BiometricVector { get; private set; }
		public string ConsentText { get; private set; }
		public string? DigitalSignature { get; private set; }
		public DateTime CreatedAt { get; private set; }

		public Subject Subject { get; private set; } = null!;

		private BiometricAuth() { }

		public static BiometricAuth Create(
			Guid subjectId,
			byte[] biometricVector,
			string consentText,
			string? templateType = null,
			string? digitalSignature = null)
		{
			return new BiometricAuth
			{
				Id = Guid.NewGuid(),
				SubjectId = subjectId,
				BiometricVector = biometricVector,
				ConsentText = consentText,
				TemplateType = templateType,
				DigitalSignature = digitalSignature,
				CreatedAt = DateTime.UtcNow
			};
		}
	}
}
