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

		public RegisterSubjectUseCase(ISubjectRepository subjectRepository, IDeviceRepository deviceRepository, IBiometricAuthRepository biometricRepository, IBiometricProcessor biometricProcessor)
		{
			_subjectRepository = subjectRepository;
			_deviceRepository = deviceRepository;
			_biometricRepository = biometricRepository;
			_biometricProcessor = biometricProcessor;
		}

		public async Task<RegisterSubjectResponse> ExecuteAsync(RegisterSubjectCommand command)
		{
			var existing = await _subjectRepository.GetByIdentificationAsync(command.Identification);
			if (existing != null)
				throw new InvalidOperationException($"Ya existe un sujeto con identificación {command.Identification}");

			var existingEmail = await _subjectRepository.GetByEmailAsync(command.Email);
			if (existingEmail != null)
				throw new InvalidOperationException($"Ya existe un sujeto con email {command.Email}");
			var subject = Subject.Create(command.Identification, command.FullName, command.Email, command.Phone, command.Address,
				command.SubjectType,
				command.ContactPerson
			);
			await _subjectRepository.AddAsync(subject);
			await _subjectRepository.SaveChangesAsync();
			var device = Device.Create(subject.Id, command.DeviceType, command.Brand, command.Model, command.SerialNumber);
			await _deviceRepository.AddAsync(device);

			var credential = DeviceCredential.Create(device.Id, command.EncryptedPassword, command.EncryptionIV, command.SystemUser);
			await _deviceRepository.AddCredentialAsync(credential);
			await _deviceRepository.SaveChangesAsync();

			var biometricVector = _biometricProcessor.ProcessImage(command.BiometricImageBase64);
			var biometric = BiometricAuth.Create(subject.Id, biometricVector, command.ConsentText, command.TemplateType, command.DigitalSignature );
			await _biometricRepository.AddAsync(biometric);
			await _biometricRepository.SaveChangesAsync();

			return new RegisterSubjectResponse
			{
				SubjectId = subject.Id,
				DeviceId = device.Id,
				BiometricAuthId = biometric.Id,
				Message = "Sujeto registrado exitosamente"
			};
		}
	}
}
