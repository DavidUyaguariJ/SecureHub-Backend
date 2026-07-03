using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard.Dtos
{
	public record ContractStatsRaw(int Active, int Expired, int Revoked);
}
