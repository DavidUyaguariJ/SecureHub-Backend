using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco.Dtos
{
	public record CreateArcoRequestDto(
		Guid SubjectId,
		string RequestType,
		string? Description,
		string ImageBase64,
		UpdateSubjectDataDto? UpdatedData 
	);
}
