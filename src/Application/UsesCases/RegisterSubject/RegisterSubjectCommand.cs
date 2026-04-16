using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.RegisterSubject
{
	public class RegisterSubjectCommand
	{
		public string Identification { get; set; } = string.Empty;
		public string FullName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string? Phone { get; set; }
		public string? Address { get; set; }
		public string? SubjectType { get; set; }
		public string? ContactPerson { get; set; }
		public string DeviceType { get; set; } = string.Empty;
		public string? Brand { get; set; }
		public string? Model { get; set; }
		public string? SerialNumber { get; set; }
		public string? SystemUser { get; set; }
		public byte[] EncryptedPassword { get; set; } = Array.Empty<byte>();
		public byte[] EncryptionIV { get; set; } = Array.Empty<byte>();
		public string BiometricImageBase64 { get; set; } = string.Empty;
		public string ConsentText { get; set; } = string.Empty;
		public string? TemplateType { get; set; }
		public string? DigitalSignature { get; set; }
	}
}
