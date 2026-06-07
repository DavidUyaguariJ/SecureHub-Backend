using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco.Dtos
{
	public record DevicePortalDto(
		Guid Id,
		string DeviceType,
		string? Brand,
		string? Model,
		string? SerialNumber
	);
}
