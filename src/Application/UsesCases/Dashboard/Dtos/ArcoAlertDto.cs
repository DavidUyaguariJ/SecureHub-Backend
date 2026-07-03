using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard.Dtos
{
	public record ArcoAlertDto(
		Guid RequestId,
		string SubjectName,
		string RequestType,
		string Status,
		DateTimeOffset DueDate,
		int DaysOverdue,
		bool IsOverdue
	);
}
