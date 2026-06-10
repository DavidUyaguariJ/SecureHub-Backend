using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IBlockchainService
	{
		Task<string> RecordAuditAsync(
			Guid entityId, string entityType, string action,
			string previousState, string newState,
			string operatorRef, string ipHash,
			CancellationToken ct = default);

		Task<string> RecordArcoRequestAsync(
			Guid arcoRequestId, Guid subjectId,
			string requestType, DateTimeOffset requestedAt,
			CancellationToken ct = default);

		Task<string> UpdateArcoStatusAsync(
			Guid arcoRequestId, string newStatus,
			string resolutionHash, CancellationToken ct = default);

		Task<string> RegisterThirdPartyAsync(
			Guid thirdPartyId, string name, string contractHash,
			string dataCategories, string purpose, DateTimeOffset validUntil,
			CancellationToken ct = default);

		Task<(bool Active, DateTimeOffset ValidUntil, int Status)> IsThirdPartyActiveAsync(
			Guid thirdPartyId, CancellationToken ct = default);
		Task<string> UpdateThirdPartyStatusAsync(Guid thirdPartyId, int status,CancellationToken ct = default);
	}
}
