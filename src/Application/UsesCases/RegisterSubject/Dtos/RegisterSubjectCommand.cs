using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.RegisterSubject.Dtos
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
		public List<DeviceCommand> Devices { get; set; } = new();
	}

}
