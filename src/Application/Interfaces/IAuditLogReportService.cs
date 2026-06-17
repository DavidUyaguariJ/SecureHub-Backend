using SecureHub.Application.UsesCases.Dashboard.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IAuditLogReportService
	{
		byte[] GeneratePdf(IEnumerable<ImmutableAuditEventDto> items);
		byte[] GenerateExcel(IEnumerable<ImmutableAuditEventDto> items);
	}
}
