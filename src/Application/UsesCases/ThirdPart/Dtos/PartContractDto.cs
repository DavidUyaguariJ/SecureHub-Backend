using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.ThirdPart.Dtos
{
	public record PartContractDto(
		Guid Id,
		string CompanyName,
		string ContactEmail,
		string? ContactPerson,
		string PurposeDescription,
		string[] AllowedFields,
		DateTimeOffset ValidFrom,
		DateTimeOffset ValidUntil,
		string Status,
		string? KeycloakUsername,
		string? BlockchainTxHash,
		bool IsActive,
		DateTimeOffset? RevokedAt,
		string? RevokedReason,
		DateTime CreatedAt
	);
}
