using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Arco;
using SecureHub.Application.UsesCases.Dashboard;
using SecureHub.Application.UsesCases.RegisterSubject;
using SecureHub.Application.UsesCases.ThirdPart;
using SecureHub.Infrastructure.Biometrics;
using SecureHub.Infrastructure.Blockchain;
using SecureHub.Infrastructure.Documents;
using SecureHub.Infrastructure.Email;
using SecureHub.Infrastructure.Persistence;
using SecureHub.Infrastructure.Persistence.Repositories;
using SecureHub.Infrastructure.Security;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
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
builder.Services.AddHttpClient("KeycloakAdmin")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    });
 
builder.Services.AddScoped<IEmailService, MailKitEmailService>();
builder.Services.AddScoped<IKeycloakService, KeycloakService>();
builder.Services.AddScoped<GetSubjectPortalDataUseCase>();
builder.Services.AddScoped<RegisterSubjectBiometricUseCase>();
builder.Services.AddScoped<VerifySubjectBiometricUseCase>();
builder.Services.AddScoped<CreatePartContractUseCase>();
builder.Services.AddScoped<RevokePartContractUseCase>();
builder.Services.AddScoped<GetPartContractsUseCase>();
builder.Services.AddScoped<GetExternalSubjectDataUseCase>();

builder.Services.AddScoped<RegisterSubjectUseCase>();
builder.Services.AddScoped<CreateArcoRequestUseCase>();
builder.Services.AddScoped<UpdateArcoStatusUseCase>();
builder.Services.AddScoped<GetArcoRequestsUseCase>();
builder.Services.AddScoped<LookupSubjectUseCase>();
builder.Services.AddScoped<RenewPartContractUseCase>();
builder.Services.AddScoped<GetDashboardSummaryUseCase>();
builder.Services.AddScoped<GetDashboardAlertsUseCase>();
builder.Services.AddScoped<GetArcoDashboardChartsUseCase>();
builder.Services.AddScoped<GetImmutableAuditLogUseCase>();


// ── Cifrado RSA
var rsaPublicKey = Environment.GetEnvironmentVariable("RSA_PUBLIC_KEY")
	?? throw new InvalidOperationException("RSA_PUBLIC_KEY no configurada");
var rsaPrivateKey = Environment.GetEnvironmentVariable("RSA_PRIVATE_KEY")
	?? throw new InvalidOperationException("RSA_PRIVATE_KEY no configurada");

builder.Services.AddSingleton<IEncryptionService>(
	new RsaEncryptionService(rsaPublicKey, rsaPrivateKey));

// ── Base de datos
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
var dbName = builder.Configuration["Database:Name"] ?? "securehub_des";
var dbPort = builder.Configuration["Database:Port"] ?? "5432";

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
builder.Services.AddDbContext<SecureHubDbContext>(options =>
	options.UseNpgsql(connectionString));

// ── Repositorios
builder.Services.AddScoped<IArcoResponsePdfService,ArcoResponsePdfService>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IBiometricAuthRepository, BiometricAuthRepository>();
builder.Services.AddScoped<IArcoRequestRepository, ArcoRequestRepository>();
builder.Services.AddScoped<IArcoAuditLogRepository, ArcoAuditLogRepository>();
builder.Services.AddScoped<IPartContractRepository, PartContractRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();

// ── Infraestructura
builder.Services.AddSingleton<IBiometricProcessor, ArcFaceBiometricProcessor>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddSingleton<IBlockchainService, BlockchainService>();
builder.Services.AddScoped<IPartContractPdfService, PartContractPdfService>();
builder.Services.AddScoped<IDashboardReportService, DashboardReportService>();
builder.Services.AddScoped<IAuditLogReportService, AuditLogReportService>();


// ── Autenticación Keycloak / JWT
var keycloakConfig = builder.Configuration.GetSection("Keycloak");
var authority = keycloakConfig["Authority"];
var audience = keycloakConfig["Audience"];

JsonWebKeySet jwks;
var handler = new HttpClientHandler
{
	ServerCertificateCustomValidationCallback = (_, _, _, _) => true
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
					context.Token = authHeader["Bearer ".Length..];
				return Task.CompletedTask;
			}
		};
	});

builder.Services.AddAuthorization();

// ── CORS
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
		policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
var app = builder.Build();

if (app.Environment.IsDevelopment())
	app.MapOpenApi();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// ── Middleware: extrae roles de resource_access del JWT Keycloak
app.Use(async (context, next) =>
{
	var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
	if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
	{
		var token = authHeader["Bearer ".Length..];
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
			if (resourceAccessClaim is not null)
			{
				var resourceAccess = JsonDocument.Parse(resourceAccessClaim);
				if (resourceAccess.RootElement.TryGetProperty("SecureHub-Api", out var client))
				{
					if (client.TryGetProperty("roles", out var roles))
					{
						foreach (var role in roles.EnumerateArray())
						{
							identity!.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
						}
					}
				}
			}

			context.User = principal;
		}
		catch (Exception ex)
		{
			throw new Exception("Token no válido", ex);
		}
	}

	await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();