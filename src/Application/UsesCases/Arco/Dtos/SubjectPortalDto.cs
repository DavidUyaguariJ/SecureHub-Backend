using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco.Dtos
{
	public record SubjectPortalDto(
		Guid Id,
		string Identification,
		string FullName,
		string Email,
		string? Phone,
		string? Address,
		string? SubjectType,
		string? ContactPerson,
		bool HasBiometrics,
		DateTime CreatedAt
	);
}
