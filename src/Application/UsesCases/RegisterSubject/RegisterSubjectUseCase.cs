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

		public RegisterSubjectUseCase(ISubjectRepository subjectRepository, IDeviceRepository deviceRepository, IBiometricAuthRepository biometricRepository, IBiometricProcessor biometricProcessor, IEncryptionService encryptionService, IUnitOfWork unitOfWork)
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
					command.Identification,
					command.FullName,
					command.Email,
					command.Phone,
					command.Address,
					command.SubjectType,
					command.ContactPerson
				);

				await _subjectRepository.AddAsync(subject);

				var deviceResponses = new List<DeviceResponse>();

				foreach (var deviceCmd in command.Devices)
				{
					var device = Device.Create(
						subject.Id,
						deviceCmd.DeviceType,
						deviceCmd.Brand,
						deviceCmd.Model,
						deviceCmd.SerialNumber
					);

					await _deviceRepository.AddAsync(device);

					var (encryptedPassword, iv) = _encryptionService.Encrypt(deviceCmd.Password);

					var credential = DeviceCredential.Create(
						device.Id,
						encryptedPassword,
						iv,
						deviceCmd.SystemUser
					);

					await _deviceRepository.AddCredentialAsync(credential);

					deviceResponses.Add(new DeviceResponse
					{
						DeviceId = device.Id,
						DeviceType = device.DeviceType,
						SerialNumber = device.SerialNumber
					});
				}

				var biometricVector = _biometricProcessor.ProcessImage(command.BiometricImageBase64);

				var biometric = BiometricAuth.Create(
					subject.Id,
					biometricVector,
					command.ConsentText,
					command.TemplateType,
					command.DigitalSignature
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
