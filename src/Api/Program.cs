using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureHub.Infrastructure.Persistence;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var envFile = builder.Environment.EnvironmentName switch
{
	"Development" => "Develop",
	"Staging" => "Stage",
	_ => builder.Environment.EnvironmentName
};
builder.Configuration
	.SetBasePath(Directory.GetCurrentDirectory())
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddJsonFile($"appsettings.{envFile}.json", optional: true)
	.AddEnvironmentVariables();

var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "devuser";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "devpass";
var dbName = builder.Configuration["Database:Name"] ?? "securehubdb";
var dbPort = builder.Configuration["Database:Port"] ?? "5432";

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
builder.Services.AddDbContext<SecureHubDbContext>(options =>
	options.UseNpgsql(connectionString)
);

var keycloakConfig = builder.Configuration.GetSection("Keycloak");
var authority = keycloakConfig["Authority"];
var audience = keycloakConfig["Audience"];
JsonWebKeySet jwks = null;
var handler = new HttpClientHandler
{
	ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
};
using var httpClient = new HttpClient(handler);
var jwksUrl = $"{authority}/protocol/openid-connect/certs";
var jwksResponse = await httpClient.GetStringAsync(jwksUrl);
jwks = JsonWebKeySet.Create(jwksResponse);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.RequireHttpsMetadata = false;
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = authority,
			ValidateAudience = true,
			ValidAudience = audience,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			IssuerSigningKeys = jwks.Keys,
			NameClaimType = "preferred_username",
			RoleClaimType = ClaimTypes.Role,
			ClockSkew = TimeSpan.FromMinutes(5)
		};
		options.Events = new JwtBearerEvents
		{
			OnMessageReceived = context =>
			{
				var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
				if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
				{
					context.Token = authHeader.Substring("Bearer ".Length);
				}
				return Task.CompletedTask;
			}
		};
	});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseHttpsRedirection();
// Reemplaza el app.UseAuthentication() con este middleware manual
app.Use(async (context, next) =>
{
	var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

	if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
	{
		var token = authHeader.Substring("Bearer ".Length);
		var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
		var validationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = authority,
			ValidateAudience = true,
			ValidAudience = audience,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			IssuerSigningKeys = jwks.Keys,
			NameClaimType = "preferred_username",
			RoleClaimType = ClaimTypes.Role
		};

		try
		{
			var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
			var identity = principal.Identity as ClaimsIdentity;
			var resourceAccessClaim = principal.FindFirst("resource_access")?.Value;
			if (resourceAccessClaim != null)
			{
				var resourceAccess = JsonDocument.Parse(resourceAccessClaim);
				if (resourceAccess.RootElement.TryGetProperty("SecureHub-Api", out var client))
				{
					if (client.TryGetProperty("roles", out var roles))
					{
						foreach (var role in roles.EnumerateArray())
						{
							identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()));
						}
					}
				}
			}

			context.User = principal;
		}
		catch (Exception ex)
		{
			throw new Exception("Token no valido", ex);
		}
	}

	await next();
});

app.UseAuthorization();
app.UseAuthorization();

app.MapControllers();

app.Run();