using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco.Dtos
{
	public record ArcoRequestResponseDto(
		Guid Id,
		Guid SubjectId,
		string SubjectFullName,
		string RequestType,
		string Status,
		string? Description,
		DateTimeOffset RequestedAt,
		DateTimeOffset? DueDate,
		DateTimeOffset? ResolvedAt,
		string? ResponseText,
		string? RejectedReason,
		bool HasResponseFile
	);
}
