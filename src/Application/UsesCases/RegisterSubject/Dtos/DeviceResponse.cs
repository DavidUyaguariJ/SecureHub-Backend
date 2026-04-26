using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.RegisterSubject.Dtos
{
	public class DeviceResponse
	{
		public Guid DeviceId { get; set; }
		public string DeviceType { get; set; } = string.Empty;
		public string? SerialNumber { get; set; }
	}
}
