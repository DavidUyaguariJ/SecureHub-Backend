using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco.Dtos
{
	public record ArcoRequestDetailDto(
		ArcoRequestResponseDto Request,
		IEnumerable<ArcoAuditLogDto> AuditLogs
	);
}
