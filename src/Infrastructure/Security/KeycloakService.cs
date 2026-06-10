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
		private readonly string _externalGroupId;
		private readonly IHttpClientFactory _httpClientFactory;

		public KeycloakService(IConfiguration config, IHttpClientFactory httpClientFactory)
		{
			_adminUrl = config["Keycloak:AdminUrl"]
				?? throw new InvalidOperationException("Keycloak:AdminUrl no configurado");
			_realm = config["Keycloak:Realm"]
				?? throw new InvalidOperationException("Keycloak:Realm no configurado");
			_adminClientId = config["Keycloak:AdminClientId"]
				?? throw new InvalidOperationException("Keycloak:AdminClientId no configurado");
			_adminSecret = config["Keycloak:AdminSecret"]
				?? throw new InvalidOperationException("Keycloak:AdminSecret no configurado");
			_applicantGroupId = config["Keycloak:ApplicantGroupId"]
				?? throw new InvalidOperationException("Keycloak:ApplicantGroupId no configurado");
			_externalGroupId = config["Keycloak:ExternalGroupId"]
				?? throw new InvalidOperationException("Keycloak:ExternalGroupId no configurado");
			_httpClientFactory = httpClientFactory;
		}

		public async Task<(string Username, string TemporaryPassword)> CreateApplicantUserAsync(
			string fullName, string email, string identification, CancellationToken ct = default)
		{
			var token = await GetAdminTokenAsync(ct);
			var username = GenerateUsername(identification);
			var tempPass = GenerateTemporaryPassword();
			var existingId = await FindUserByUsernameOrEmailAsync(token, username, email, ct);
			if (existingId is not null)
				return (username, tempPass);

			var userId = await CreateUserAsync(token, username, fullName, email, tempPass, ct);
			await AddToGroupAsync(token, userId, ct);
			return (username, tempPass);
		}

		public async Task<(string Username, string TemporaryPassword)> CreateExternalUserAsync(
			string companyName, string contactEmail,
			Guid contractId, DateTimeOffset validUntil,
			CancellationToken ct = default)
		{
			var token = await GetAdminTokenAsync(ct);
			var slug = new string(companyName.Where(char.IsLetterOrDigit).Take(8).ToArray()).ToLower();
			var username = $"ext_{slug}_{contractId.ToString()[..8]}";
			var tempPass = GenerateTemporaryPassword();
			await EnsureEmailNotTakenAsync(token, contactEmail, null, ct);
			var userId = await CreateUserCoreAsync(token, username, companyName, contactEmail, tempPass,
				new Dictionary<string, string[]>
				{
					["contract_id"] = [contractId.ToString()],
					["valid_until"] = [validUntil.ToString("O")],
					["user_type"] = ["external"]
				}, ct);

			await AddToExternalGroupAsync(token, userId, ct);
			return (username, tempPass);
		}

		public async Task DisableUserAsync(string keycloakUserId, CancellationToken ct = default)
		{
			var token = await GetAdminTokenAsync(ct);
			var client = _httpClientFactory.CreateClient("KeycloakAdmin");
			var url = $"{_adminUrl}/admin/realms/{_realm}/users/{keycloakUserId}";
			var req = new HttpRequestMessage(HttpMethod.Put, url)
			{
				Content = new StringContent(
					JsonSerializer.Serialize(new { enabled = false }),
					Encoding.UTF8, "application/json")
			};
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			var resp = await client.SendAsync(req, ct);
			resp.EnsureSuccessStatusCode();
		}

		public async Task DeleteUserAsync(string keycloakUserId, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(keycloakUserId))
			{
				Console.WriteLine("[DELETE] keycloakUserId vacío — omitiendo eliminación");
				return;
			}
			var token = await GetAdminTokenAsync(ct);
			var client = _httpClientFactory.CreateClient("KeycloakAdmin");
			try
			{
				var disableUrl = $"{_adminUrl}/admin/realms/{_realm}/users/{keycloakUserId}";
				var disableReq = new HttpRequestMessage(HttpMethod.Put, disableUrl)
				{
					Content = new StringContent(
						JsonSerializer.Serialize(new { enabled = false }),
						Encoding.UTF8, "application/json")
				};
				disableReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
				await client.SendAsync(disableReq, ct);
			}
			catch { /* si falla el disable igualmente intentamos borrar */ }

			var url = $"{_adminUrl}/admin/realms/{_realm}/users/{keycloakUserId}";
			var req = new HttpRequestMessage(HttpMethod.Delete, url);
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			var resp = await client.SendAsync(req, ct);

			if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				Console.WriteLine($"[DELETE] Usuario {keycloakUserId} no encontrado en Keycloak — ya fue eliminado.");
				return;
			}

			resp.EnsureSuccessStatusCode();
			Console.WriteLine($"[DELETE] Usuario {keycloakUserId} eliminado de Keycloak.");
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

		/// <summary>
		/// Busca usuario por username exacto O por email exacto.
		/// Retorna el Keycloak userId si lo encuentra, null si no existe.
		/// </summary>
		private async Task<string?> FindUserByUsernameOrEmailAsync(
			string token, string username, string email, CancellationToken ct)
		{
			var client = _httpClientFactory.CreateClient("KeycloakAdmin");

			// Buscar por username exacto
			var urlByUsername = $"{_adminUrl}/admin/realms/{_realm}/users?username={Uri.EscapeDataString(username)}&exact=true";
			var reqU = new HttpRequestMessage(HttpMethod.Get, urlByUsername);
			reqU.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			var respU = await client.SendAsync(reqU, ct);
			if (respU.IsSuccessStatusCode)
			{
				var jsonU = await respU.Content.ReadAsStringAsync(ct);
				var usersU = JsonDocument.Parse(jsonU).RootElement;
				if (usersU.GetArrayLength() > 0)
					return usersU[0].GetProperty("id").GetString();
			}

			// Buscar por email exacto
			var urlByEmail = $"{_adminUrl}/admin/realms/{_realm}/users?email={Uri.EscapeDataString(email)}&exact=true";
			var reqE = new HttpRequestMessage(HttpMethod.Get, urlByEmail);
			reqE.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			var respE = await client.SendAsync(reqE, ct);
			if (respE.IsSuccessStatusCode)
			{
				var jsonE = await respE.Content.ReadAsStringAsync(ct);
				var usersE = JsonDocument.Parse(jsonE).RootElement;
				if (usersE.GetArrayLength() > 0)
					return usersE[0].GetProperty("id").GetString();
			}

			return null;
		}

		/// <summary>
		/// Para encargados externos: lanza excepción si el email ya está en uso
		/// (a menos que pertenezca al userId excluido, útil en renovación).
		/// </summary>
		private async Task EnsureEmailNotTakenAsync(
			string token, string email, string? excludeUserId, CancellationToken ct)
		{
			var client = _httpClientFactory.CreateClient("KeycloakAdmin");
			var url = $"{_adminUrl}/admin/realms/{_realm}/users?email={Uri.EscapeDataString(email)}&exact=true";
			var req = new HttpRequestMessage(HttpMethod.Get, url);
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			var resp = await client.SendAsync(req, ct);
			if (!resp.IsSuccessStatusCode) return;

			var json = await resp.Content.ReadAsStringAsync(ct);
			var users = JsonDocument.Parse(json).RootElement;
			if (users.GetArrayLength() == 0) return;

			var foundId = users[0].GetProperty("id").GetString();
			if (excludeUserId is not null && foundId == excludeUserId) return;

			throw new InvalidOperationException(
				$"El correo '{email}' ya está registrado en Keycloak para otro usuario. " +
				"Utilice un correo de contacto diferente para este encargado.");
		}

		private async Task<string> CreateUserCoreAsync(
			string token, string username, string fullName, string email, string tempPass,
			Dictionary<string, string[]>? attributes, CancellationToken ct)
		{
			var client = _httpClientFactory.CreateClient("KeycloakAdmin");
			var url = $"{_adminUrl}/admin/realms/{_realm}/users";

			var nameParts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
			var payload = new
			{
				username = username,
				email = email,
				firstName = nameParts[0],
				lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
				enabled = true,
				emailVerified = true,
				attributes = attributes,
				credentials = new[] { new { type = "password", value = tempPass, temporary = true } }
			};

			var req = new HttpRequestMessage(HttpMethod.Post, url)
			{
				Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
			};
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			var resp = await client.SendAsync(req, ct);

			if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
				throw new InvalidOperationException(
					$"El usuario '{username}' ya existe en Keycloak. " +
					"Verifique que el contrato no haya sido registrado previamente.");

			resp.EnsureSuccessStatusCode();
			var location = resp.Headers.Location?.ToString()
				?? throw new InvalidOperationException("Keycloak no devolvió Location header.");
			return location.Split('/').Last();
		}

		private async Task<string> CreateUserAsync(
			string token, string username, string fullName,
			string email, string tempPass, CancellationToken ct)
		{
			var client = _httpClientFactory.CreateClient("KeycloakAdmin");
			var url = $"{_adminUrl}/admin/realms/{_realm}/users";

			var nameParts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

			var payload = new
			{
				username = username,
				email = email,
				firstName = nameParts[0],
				lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
				enabled = true,
				emailVerified = true,
				credentials = new[] { new { type = "password", value = tempPass, temporary = true } }
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
				throw new InvalidOperationException(
					$"El usuario '{username}' ya existe en Keycloak (realm: {_realm}).");

			resp.EnsureSuccessStatusCode();
			var location = resp.Headers.Location?.ToString()
				?? throw new InvalidOperationException("Keycloak no devolvió Location header.");
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

		private async Task AddToExternalGroupAsync(string token, string userId, CancellationToken ct)
		{
			var client = _httpClientFactory.CreateClient("KeycloakAdmin");
			var url = $"{_adminUrl}/admin/realms/{_realm}/users/{userId}/groups/{_externalGroupId}";
			var req = new HttpRequestMessage(HttpMethod.Put, url);
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			var resp = await client.SendAsync(req, ct);
			resp.EnsureSuccessStatusCode();
		}
		public async Task<string?> GetUserIdByUsernameAsync(string username, CancellationToken ct = default)
		{
			var token = await GetAdminTokenAsync(ct);
			var client = _httpClientFactory.CreateClient("KeycloakAdmin");
			var url = $"{_adminUrl}/admin/realms/{_realm}/users?username={Uri.EscapeDataString(username)}&exact=true";
			var req = new HttpRequestMessage(HttpMethod.Get, url);
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			var resp = await client.SendAsync(req, ct);
			if (!resp.IsSuccessStatusCode) return null;
			var json = await resp.Content.ReadAsStringAsync(ct);
			var users = JsonDocument.Parse(json).RootElement;
			if (users.GetArrayLength() == 0) return null;
			return users[0].GetProperty("id").GetString();
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