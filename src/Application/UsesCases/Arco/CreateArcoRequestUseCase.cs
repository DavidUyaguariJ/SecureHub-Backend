using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Arco.Dtos;
using SecureHub.Domain.Entities;
using SecureHub.Infrastructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SecureHub.Application.UsesCases.Arco
{
	public class CreateArcoRequestUseCase
	{
		private const float FaceMatchThreshold = 0.40f;

		private readonly IArcoRequestRepository _arcoRepo;
		private readonly IArcoAuditLogRepository _auditRepo;
		private readonly IBiometricAuthRepository _biometricRepo;
		private readonly IBiometricProcessor _biometricProcessor;
		private readonly ISubjectRepository _subjectRepo;
		private readonly IEncryptionService _encryptionService;
		private readonly IEmailService _emailService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IBlockchainService _blockchain;

		public CreateArcoRequestUseCase(
			IArcoRequestRepository arcoRepo,
			IArcoAuditLogRepository auditRepo,
			IBiometricAuthRepository biometricRepo,
			IBiometricProcessor biometricProcessor,
			ISubjectRepository subjectRepo,
			IEncryptionService encryptionService,
			IEmailService emailService,
			IUnitOfWork unitOfWork,
			IBlockchainService blockchain)
		{
			_arcoRepo = arcoRepo;
			_auditRepo = auditRepo;
			_biometricRepo = biometricRepo;
			_biometricProcessor = biometricProcessor;
			_subjectRepo = subjectRepo;
			_encryptionService = encryptionService;
			_emailService = emailService;
			_unitOfWork = unitOfWork;
			_blockchain = blockchain;
		}
		private static string HashString(string? value)
		{
			if (string.IsNullOrEmpty(value)) return "";
			using var sha = System.Security.Cryptography.SHA256.Create();
			var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
			return "sha256:" + Convert.ToHexString(bytes).ToLower();
		}
		public async Task<ArcoRequestResponseDto> ExecuteAsync(
			CreateArcoRequestDto dto, string requesterIp, CancellationToken ct = default)
		{
			await _unitOfWork.BeginTransactionAsync();
			try
			{
				var subject = await _subjectRepo.GetByIdAsync(dto.SubjectId, ct)
					?? throw new InvalidOperationException("Titular no encontrado.");

				var stored = await _biometricRepo.GetLatestBySubjectIdAsync(dto.SubjectId, ct)
					?? throw new InvalidOperationException("El titular no tiene datos biométricos registrados.");

				var decryptedStoredVector = _encryptionService.DecryptBytes(stored.BiometricVector);
				var embeddingResult = await _biometricProcessor.ExtractEmbeddingAsync(dto.ImageBase64);
				var candidateBytes = _biometricProcessor.SerializeEmbedding(embeddingResult.Embedding);
				float score = _biometricProcessor.CompareFaces(decryptedStoredVector, candidateBytes);

				if (score < FaceMatchThreshold)
					throw new UnauthorizedAccessException(
						$"Verificación biométrica fallida. Score: {score:F3}, requerido: {FaceMatchThreshold}");

				string? description = dto.Description;
				if (dto.RequestType == "RECTIFICACION" && dto.UpdatedData is not null)
				{
					var updatedJson = JsonSerializer.Serialize(dto.UpdatedData,
						new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
					description = string.IsNullOrWhiteSpace(dto.Description)
						? updatedJson
						: $"{updatedJson}|{dto.Description}";
				}

				var dueDate = CalculateBusinessDays(DateTimeOffset.UtcNow, 15);
				var request = new ArcoRequest
				{
					SubjectId = dto.SubjectId,
					RequestType = dto.RequestType,
					Status = "PENDIENTE",
					Description = description,
					RequestedAt = DateTimeOffset.UtcNow,
					DueDate = dueDate,
					CreatedAt = DateTimeOffset.UtcNow
				};

				await _arcoRepo.AddAsync(request, ct);
				await _auditRepo.AddAsync(new ArcoAuditLog
				{
					ArcoRequestId = request.Id,
					Action = "CREADO",
					NewStatus = "PENDIENTE",
					PerformedByRole = "TITULAR",
					IpAddress = requesterIp,
					Notes = $"Tipo: {dto.RequestType} | Score biométrico: {score:F3}",
					CreatedAt = DateTimeOffset.UtcNow
				}, ct);

				await _unitOfWork.CommitAsync();

				await _blockchain.RecordArcoRequestAsync(
					arcoRequestId: request.Id,
					subjectId: request.SubjectId,
					requestType: request.RequestType,
					requestedAt: request.RequestedAt,
					ct: ct);

				await _blockchain.RecordAuditAsync(
					entityId: request.Id,
					entityType: "ARCO_REQUEST",
					action: "CREATED",
					previousState: "",
					newState: "PENDIENTE",
					operatorRef: "TITULAR",
					ipHash: HashString(requesterIp),
					ct: ct);
				try
				{
					var subjectEmail = TryDecrypt(subject.Email);
					var subjectName = TryDecrypt(subject.FullName);
					await _emailService.SendArcoCreatedAsync(
						subjectEmail, subjectName,
						dto.RequestType, request.Id, request.DueDate, ct);
				}
				catch { /* no bloquea la solicitud si el correo falla */ }

				return new ArcoRequestResponseDto(
					request.Id, request.SubjectId, subject.FullName,
					request.RequestType, request.Status, request.Description,
					request.RequestedAt, request.DueDate, null, null, null, false);
			}
			catch
			{
				await _unitOfWork.RollbackAsync();
				throw;
			}
		}

		private string TryDecrypt(string val)
		{
			try { return _encryptionService.Decrypt(val); } catch { return val; }
		}

		private static DateTimeOffset CalculateBusinessDays(DateTimeOffset start, int days)
		{
			var date = start;
			int added = 0;
			while (added < days)
			{
				date = date.AddDays(1);
				if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
					added++;
			}
			return date;
		}
	}

}