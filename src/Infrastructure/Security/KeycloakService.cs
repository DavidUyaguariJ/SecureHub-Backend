using Microsoft.Extensions.Configuration;
using SecureHub.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SecureHub.Infrastructure.Security
{
	public class KeycloakService : IKeycloakService
	{
		private readonly string _adminUrl;
		private readonly string _realm;
		private readonly string _adminClientId;
		private readonly string _adminSecret;
		private readonly string _applicantGroupId;
		private readonly IHttpClientFactory _httpClientFactory;

		public KeycloakService(IConfiguration config, IHttpClientFactory httpClientFactory)
		{
			_adminUrl = config["Keycloak:AdminUrl"]
				?? throw new InvalidOperationException("Keycloak:AdminUrl no configurado");
			_realm = config["Keycloak:Realm"]
				?? throw new InvalidOperationException("Keycloak:Realm no configurado. Debe definirse por ambiente (develop/staging/production)");
			_adminClientId = config["Keycloak:AdminClientId"]
				?? throw new InvalidOperationException("Keycloak:AdminClientId no configurado");
			_adminSecret = config["Keycloak:AdminSecret"]
				?? throw new InvalidOperationException("Keycloak:AdminSecret no configurado");
			_applicantGroupId = config["Keycloak:ApplicantGroupId"]
				?? throw new InvalidOperationException("Keycloak:ApplicantGroupId no configurado");
			_httpClientFactory = httpClientFactory;
		}

		public async Task<(string Username, string TemporaryPassword)> CreateApplicantUserAsync(
			string fullName, string email, string identification, CancellationToken ct = default)
		{
			var token = await GetAdminTokenAsync(ct);
			var username = GenerateUsername(identification);
			var tempPass = GenerateTemporaryPassword();
			var userId = await CreateUserAsync(token, username, fullName, email, tempPass, ct);
			await AddToGroupAsync(token, userId, ct);
			return (username, tempPass);
		}

		private async Task<string> GetAdminTokenAsync(CancellationToken ct)
		{
			var client = _httpClientFactory.CreateClient("KeycloakAdmin");
			var url = $"{_adminUrl}/realms/master/protocol/openid-connect/token";

			var body = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["grant_type"] = "client_credentials",
				["client_id"] = _adminClientId,
				["client_secret"] = _adminSecret
			});

			var resp = await client.PostAsync(url, body, ct);
			resp.EnsureSuccessStatusCode();

			var json = await resp.Content.ReadAsStringAsync(ct);
			var doc = JsonDocument.Parse(json);
			return doc.RootElement.GetProperty("access_token").GetString()!;
		}

		private async Task<string> CreateUserAsync(
			string token, string username, string fullName,
			string email, string tempPass, CancellationToken ct)
		{
			var client = _httpClientFactory.CreateClient("KeycloakAdmin");
			var url = $"{_adminUrl}/admin/realms/{_realm}/users";

			var nameParts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
			var firstName = nameParts[0];
			var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

			var payload = new
			{
				username = username,
				email = email,
				firstName = firstName,
				lastName = lastName,
				enabled = true,
				emailVerified = true,
				credentials = new[]
				{
					new { type = "password", value = tempPass, temporary = true }
				}
			};

			var req = new HttpRequestMessage(HttpMethod.Post, url)
			{
				Content = new StringContent(
					JsonSerializer.Serialize(payload),
					Encoding.UTF8, "application/json")
			};
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var resp = await client.SendAsync(req, ct);

			if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
				throw new InvalidOperationException($"El usuario '{username}' ya existe en Keycloak (realm: {_realm}).");

			resp.EnsureSuccessStatusCode();

			var location = resp.Headers.Location?.ToString()
				?? throw new InvalidOperationException("Keycloak no devolvió Location header con el userId.");

			return location.Split('/').Last();
		}

		private async Task AddToGroupAsync(string token, string userId, CancellationToken ct)
		{
			var client = _httpClientFactory.CreateClient("KeycloakAdmin");
			var url = $"{_adminUrl}/admin/realms/{_realm}/users/{userId}/groups/{_applicantGroupId}";

			var req = new HttpRequestMessage(HttpMethod.Put, url);
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var resp = await client.SendAsync(req, ct);
			resp.EnsureSuccessStatusCode();
		}

		private static string GenerateUsername(string identification)
			=> identification.Replace("-", "").Replace(".", "").ToLowerInvariant();

		private static string GenerateTemporaryPassword()
		{
			const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
			const string symbols = "!@#$%";
			var rng = new Random();
			var pass = new string(Enumerable.Range(0, 9)
				.Select(_ => chars[rng.Next(chars.Length)]).ToArray());
			return pass + symbols[rng.Next(symbols.Length)];
		}
	}
}