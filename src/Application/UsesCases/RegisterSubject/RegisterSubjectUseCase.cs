using System;
using System.Collections.Generic;
using System.Text;
using SecureHub.Application.Interfaces;
using SecureHub.Domain.Entities;
using SecureHub.Infrastructure.Persistence.Repositories;

namespace SecureHub.Application.UsesCases.RegisterSubject
{
	public class RegisterSubjectUseCase
	{
		private readonly ISubjectRepository _subjectRepository;
		private readonly IDeviceRepository _deviceRepository;
		private readonly IBiometricAuthRepository _biometricRepository;
		private readonly IBiometricProcessor _biometricProcessor;
		private readonly IEncryptionService _encryptionService;
		private readonly IUnitOfWork _unitOfWork;

		public RegisterSubjectUseCase(ISubjectRepository subjectRepository, IDeviceRepository deviceRepository, IBiometricAuthRepository biometricRepository, IBiometricProcessor biometricProcessor,
			IEncryptionService encryptionService,IUnitOfWork unitOfWork)
		{
			_subjectRepository = subjectRepository;
			_deviceRepository = deviceRepository;
			_biometricRepository = biometricRepository;
			_biometricProcessor = biometricProcessor;
			_encryptionService = encryptionService;
			_unitOfWork = unitOfWork;
		}

		public async Task<RegisterSubjectResponse> ExecuteAsync(RegisterSubjectCommand command)
		{
			await _unitOfWork.BeginTransactionAsync();

			try
			{
				var existing = await _subjectRepository.GetByIdentificationAsync(command.Identification);
				if (existing != null)
					throw new InvalidOperationException("Ya existe un sujeto con esa identificación");

				var existingEmail = await _subjectRepository.GetByEmailAsync(command.Email);
				if (existingEmail != null)
					throw new InvalidOperationException("Ya existe un sujeto con ese email");
				var subject = Subject.Create(
					_encryptionService.Encrypt(command.Identification),
					_encryptionService.Encrypt(command.FullName),
					_encryptionService.Encrypt(command.Email),
					command.Phone != null ? _encryptionService.Encrypt(command.Phone) : null,
					command.Address != null ? _encryptionService.Encrypt(command.Address) : null,
					command.SubjectType,
					command.ContactPerson != null ? _encryptionService.Encrypt(command.ContactPerson) : null
				);

				await _subjectRepository.AddAsync(subject);

				var deviceResponses = new List<DeviceResponse>();

				foreach (var deviceCmd in command.Devices)
				{
					var device = Device.Create(
						subject.Id,
						deviceCmd.DeviceType,
						deviceCmd.Brand != null ? _encryptionService.Encrypt(deviceCmd.Brand) : null,
						deviceCmd.Model != null ? _encryptionService.Encrypt(deviceCmd.Model) : null,
						deviceCmd.SerialNumber != null ? _encryptionService.Encrypt(deviceCmd.SerialNumber) : null
					);

					await _deviceRepository.AddAsync(device);

					var credential = DeviceCredential.CreateRsa(
						device.Id,
						_encryptionService.Encrypt(deviceCmd.Password),
						_encryptionService.Encrypt(deviceCmd.SystemUser)
					);

					await _deviceRepository.AddCredentialAsync(credential);
					deviceResponses.Add(new DeviceResponse
					{
						DeviceId = device.Id,
						DeviceType = device.DeviceType,
						SerialNumber = device.SerialNumber
					});
				}
				var embeddingResult = await _biometricProcessor.ExtractEmbeddingAsync(
					command.BiometricImageBase64);

				if (!embeddingResult.FaceDetected)
					throw new InvalidOperationException(
						"No se detectó un rostro válido en la imagen biométrica");
				var embeddingBytes = _biometricProcessor.SerializeEmbedding(embeddingResult.Embedding);
				var encryptedBiometricVector = _encryptionService.EncryptBytes(embeddingBytes);
				var encryptedConsentText = _encryptionService.Encrypt(command.ConsentText);
				var encryptedDigitalSig = command.DigitalSignature != null
					? _encryptionService.Encrypt(command.DigitalSignature)
					: null;

				var biometric = BiometricAuth.Create(
					subjectId: subject.Id,
					biometricVector: encryptedBiometricVector,
					consentText: encryptedConsentText,
					templateType: command.TemplateType ?? "LBPH_256",
					digitalSignature: encryptedDigitalSig,
					embeddingModel: embeddingResult.ModelUsed,
					embeddingDims: embeddingResult.Dimensions,
					confidenceScore: embeddingResult.DetectionScore
				);

				await _biometricRepository.AddAsync(biometric);
				await _unitOfWork.CommitAsync();

				return new RegisterSubjectResponse
				{
					SubjectId = subject.Id,
					Devices = deviceResponses,
					BiometricAuthId = biometric.Id,
					Message = $"Titular registrado con {deviceResponses.Count} dispositivo(s) exitosamente"
				};
			}
			catch
			{
				await _unitOfWork.RollbackAsync();
				throw;
			}
		}
	}
}