using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard.Dtos
{
	public record ArcoAlertRaw(
		Guid Id, string? SubjectFullName, string RequestType,
		string Status, DateTimeOffset DueDate);
}
