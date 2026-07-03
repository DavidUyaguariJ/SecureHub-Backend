using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.ThirdPart.Dtos
{
	public record RenewPartContractCommand(DateTimeOffset ValidFrom, DateTimeOffset ValidUntil);
}
