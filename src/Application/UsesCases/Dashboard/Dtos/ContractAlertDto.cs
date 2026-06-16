using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard.Dtos
{
	public record ContractAlertDto(
		Guid ContractId,
		string CompanyName,
		DateTimeOffset ValidUntil,
		int DaysUntilExpiry,
		string Status
	);
}
