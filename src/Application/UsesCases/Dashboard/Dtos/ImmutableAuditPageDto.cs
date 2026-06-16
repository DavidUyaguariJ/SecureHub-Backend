using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Dashboard.Dtos
{
	public record ImmutableAuditPageDto(
		IEnumerable<ImmutableAuditEventDto> Items,
		int TotalCount,
		int Page,
		int PageSize
	);
}
