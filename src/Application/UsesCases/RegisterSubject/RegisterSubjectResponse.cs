using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.RegisterSubject
{
	public class RegisterSubjectResponse
	{
		public Guid SubjectId { get; set; }
		public List<DeviceResponse> Devices { get; set; } = new();
		public Guid BiometricAuthId { get; set; }
		public string Message { get; set; } = string.Empty;
	}
}
