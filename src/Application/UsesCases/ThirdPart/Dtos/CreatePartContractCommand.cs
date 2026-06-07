using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.ThirdPart.Dtos
{
	public record CreatePartContractCommand(
		string CompanyName,
		string ContactEmail,
		string? ContactPerson,
		string PurposeDescription,
		string[] AllowedFields,
		DateTimeOffset ValidFrom,
		DateTimeOffset ValidUntil,
		Guid SubjectId
	);
}
