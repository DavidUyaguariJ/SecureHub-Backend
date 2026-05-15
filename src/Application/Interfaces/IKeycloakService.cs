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
	}
}
