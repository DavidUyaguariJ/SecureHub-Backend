using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco.Dtos
{
	public record UpdateArcoStatusDto(
		string NewStatus,
		string? ResponseText,
		string? RejectedReason,
		string? OperatorRole
	);
}
