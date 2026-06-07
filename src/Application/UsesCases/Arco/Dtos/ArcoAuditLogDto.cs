using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco.Dtos
{
	public record ArcoAuditLogDto(
		Guid Id,
		string Action,
		string? PreviousStatus,
		string? NewStatus,
		string? PerformedByRole,
		string? PerformedByName,
		string? Notes,
		DateTimeOffset CreatedAt
	);
}
