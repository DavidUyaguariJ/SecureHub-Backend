using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco.Dtos
{
	public record ResolveArcoRequestDto(
		Guid RequestId,
		string ResponseText,
		Guid ResolvedBy
	);
}
