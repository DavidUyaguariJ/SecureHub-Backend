using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IKeycloakService
	{
		Task<(string Username, string TemporaryPassword)> CreateApplicantUserAsync(
			string fullName,
			string email,
			string identification,
			CancellationToken ct = default);

		Task<(string Username, string TemporaryPassword)> CreateExternalUserAsync(
			string companyName,
			string contactEmail,
			Guid contractId,
			DateTimeOffset validUntil,
			CancellationToken ct = default);
		Task DisableUserAsync(string keycloakUserId, CancellationToken ct = default);
		Task DeleteUserAsync(string keycloakUserId, CancellationToken ct = default);
		Task<string?> GetUserIdByUsernameAsync(string username, CancellationToken ct = default);
	}
}
