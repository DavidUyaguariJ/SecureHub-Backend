using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco.Dtos
{
	public record SubjectLookupDto(
		Guid Id,
		string Identification,
		string MaskedFullName,
		string MaskedEmail,
		string? MaskedPhone,
		bool HasBiometrics
	);
}
