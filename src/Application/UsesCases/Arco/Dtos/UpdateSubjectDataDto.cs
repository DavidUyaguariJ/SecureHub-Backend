using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco.Dtos
{
	public record UpdateSubjectDataDto(
		string? FullName,
		string? Phone,
		string? Address,
		string? Email,
		IEnumerable<UpdateDeviceDto>? Devices
	);
}
