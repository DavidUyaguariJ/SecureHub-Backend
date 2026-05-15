using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Arco.Dtos;
using SecureHub.Infrastructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco
{
	public class GetSubjectPortalDataUseCase
	{
		private readonly ISubjectRepository _subjectRepo;
		private readonly IBiometricAuthRepository _biometricRepo;
		private readonly IEncryptionService _encryption;

		public GetSubjectPortalDataUseCase(
			ISubjectRepository subjectRepo,
			IBiometricAuthRepository biometricRepo,
			IEncryptionService encryption)
		{
			_subjectRepo = subjectRepo;
			_biometricRepo = biometricRepo;
			_encryption = encryption;
		}

		public async Task<SubjectPortalDto?> ExecuteAsync(
			Guid subjectId, CancellationToken ct = default)
		{
			var subject = await _subjectRepo.GetByIdAsync(subjectId, ct);
			if (subject is null) return null;

			var biometric = await _biometricRepo.GetLatestBySubjectIdAsync(subjectId, ct);

			return new SubjectPortalDto(
				subject.Id,
				TryDecrypt(subject.Identification),
				TryDecrypt(subject.FullName),
				TryDecrypt(subject.Email),
				subject.Phone is not null ? TryDecrypt(subject.Phone) : null,
				subject.Address is not null ? TryDecrypt(subject.Address) : null,
				subject.SubjectType,
				subject.ContactPerson is not null ? TryDecrypt(subject.ContactPerson) : null,
				biometric is not null,
				subject.CreatedAt
			);
		}

		private string TryDecrypt(string val)
		{
			try { return _encryption.Decrypt(val); } catch { return val; }
		}
	}

	// ── Registrar biometría por primera vez (titular desde el portal) ────────────

	public class RegisterSubjectBiometricUseCase
	{
		private const float FaceMatchThreshold = 0.40f;

		private readonly ISubjectRepository _subjectRepo;
		private readonly IBiometricAuthRepository _biometricRepo;
		private readonly IBiometricProcessor _biometricProcessor;
		private readonly IEncryptionService _encryption;
		private readonly IUnitOfWork _unitOfWork;

		public RegisterSubjectBiometricUseCase(
			ISubjectRepository subjectRepo,
			IBiometricAuthRepository biometricRepo,
			IBiometricProcessor biometricProcessor,
			IEncryptionService encryption,
			IUnitOfWork unitOfWork)
		{
			_subjectRepo = subjectRepo;
			_biometricRepo = biometricRepo;
			_biometricProcessor = biometricProcessor;
			_encryption = encryption;
			_unitOfWork = unitOfWork;
		}

		public async Task<string> ExecuteAsync(
			Guid subjectId, RegisterBiometricCommand command, CancellationToken ct = default)
		{
			var subject = await _subjectRepo.GetByIdAsync(subjectId, ct)
				?? throw new InvalidOperationException("Titular no encontrado.");

			// Verificar que no tenga ya biometría activa
			var existing = await _biometricRepo.GetLatestBySubjectIdAsync(subjectId, ct);
			if (existing is not null)
				throw new InvalidOperationException("El titular ya tiene datos biométricos registrados.");

			await _unitOfWork.BeginTransactionAsync();
			try
			{
				var embeddingResult = await _biometricProcessor.ExtractEmbeddingAsync(command.ImageBase64);
				var embeddingBytes = _biometricProcessor.SerializeEmbedding(embeddingResult.Embedding);
				var encryptedVector = _encryption.EncryptBytes(embeddingBytes);

				var biometric = Domain.Entities.BiometricAuth.Create(
					subjectId: subjectId,
					biometricVector: encryptedVector,
					consentText: command.ConsentText,
					templateType: "FACIAL",
					embeddingModel: "ArcFace_512",
					embeddingDims: embeddingResult.Dimensions,
					confidenceScore: embeddingResult.DetectionScore
				);

				await _biometricRepo.AddAsync(biometric);
				await _unitOfWork.CommitAsync();

				return biometric.Id.ToString();
			}
			catch
			{
				await _unitOfWork.RollbackAsync();
				throw;
			}
		}
	}

	public class VerifySubjectBiometricUseCase
	{
		private const float FaceMatchThreshold = 0.40f;

		private readonly IBiometricAuthRepository _biometricRepo;
		private readonly IBiometricProcessor _biometricProcessor;
		private readonly IEncryptionService _encryption;

		public VerifySubjectBiometricUseCase(
			IBiometricAuthRepository biometricRepo,
			IBiometricProcessor biometricProcessor,
			IEncryptionService encryption)
		{
			_biometricRepo = biometricRepo;
			_biometricProcessor = biometricProcessor;
			_encryption = encryption;
		}

		public async Task<bool> ExecuteAsync(
			Guid subjectId, VerifyBiometricCommand command, CancellationToken ct = default)
		{
			var stored = await _biometricRepo.GetLatestBySubjectIdAsync(subjectId, ct)
				?? throw new InvalidOperationException("No hay datos biométricos registrados.");

			var decryptedStored = _encryption.DecryptBytes(stored.BiometricVector);
			var embeddingResult = await _biometricProcessor.ExtractEmbeddingAsync(command.ImageBase64);
			var candidateBytes = _biometricProcessor.SerializeEmbedding(embeddingResult.Embedding);

			float score = _biometricProcessor.CompareFaces(decryptedStored, candidateBytes);
			return score >= FaceMatchThreshold;
		}
	}
}