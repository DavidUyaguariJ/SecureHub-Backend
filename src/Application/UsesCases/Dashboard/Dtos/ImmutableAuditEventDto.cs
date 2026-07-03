using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard.Dtos
{
	public record ImmutableAuditEventDto(
		Guid AuditLogId,
		Guid ArcoRequestId,
		string Action,
		string? PreviousStatus,
		string? NewStatus,
		string? PerformedByName,
		string? PerformedByRole,
		string? Notes,
		string? IpAddress,
		DateTimeOffset CreatedAt,
		string IntegrityHash,
		bool BlockchainAnchored
	);
}
