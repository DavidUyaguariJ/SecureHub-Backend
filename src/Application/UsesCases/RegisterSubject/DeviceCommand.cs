using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.RegisterSubject
{
	public class DeviceCommand
	{
		public string DeviceType { get; set; } = string.Empty;
		public string? Brand { get; set; }
		public string? Model { get; set; }
		public string? SerialNumber { get; set; }
		public string? SystemUser { get; set; }
		public string Password { get; set; } = string.Empty;
	}
}
