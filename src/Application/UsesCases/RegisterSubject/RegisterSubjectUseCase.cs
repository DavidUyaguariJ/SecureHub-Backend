using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.RegisterSubject.Dtos;
using SecureHub.Domain.Entities;
using SecureHub.Infrastructure.Persistence.Repositories;

namespace SecureHub.Application.UsesCases.RegisterSubject
{
	public class RegisterSubjectUseCase
	{
		private readonly ISubjectRepository _subjectRepo;
		private readonly IDeviceRepository _deviceRepo;
		private readonly IEncryptionService _encryption;
		private readonly IKeycloakService _keycloak;
		private readonly IEmailService _email;
		private readonly IUnitOfWork _unitOfWork;

		public RegisterSubjectUseCase(
			ISubjectRepository subjectRepo,
			IDeviceRepository deviceRepo,
			IEncryptionService encryption,
			IKeycloakService keycloak,
			IEmailService email,
			IUnitOfWork unitOfWork)
		{
			_subjectRepo = subjectRepo;
			_deviceRepo = deviceRepo;
			_encryption = encryption;
			_keycloak = keycloak;
			_email = email;
			_unitOfWork = unitOfWork;
		}

		public async Task<RegisterSubjectResponse> ExecuteAsync(
			RegisterSubjectCommand command,
			CancellationToken ct = default)
		{
			await _unitOfWork.BeginTransactionAsync();

			try
			{
				var subject = Subject.Create(
					identification: _encryption.Encrypt(command.Identification),
					fullName: _encryption.Encrypt(command.FullName),
					email: _encryption.Encrypt(command.Email),
					phone: command.Phone is not null
						? _encryption.Encrypt(command.Phone!)
						: null,
					address: command.Address is not null
						? _encryption.Encrypt(command.Address!)
						: null,
					subjectType: command.SubjectType,
					contactPerson: command.ContactPerson is not null
						? _encryption.Encrypt(command.ContactPerson!)
						: null
				);

				await _subjectRepo.AddAsync(subject);

				// Registrar dispositivos
				var deviceResponses = new List<DeviceResponse>();

				foreach (var d in command.Devices)
				{
					var device = Device.Create(
						subjectId: subject.Id,
						deviceType: d.DeviceType,
						brand: d.Brand,
						model: d.Model,
						serialNumber: _encryption.Encrypt(d.SerialNumber));

					await _deviceRepo.AddAsync(device);

					// Encriptar contraseña + IV
					var (encryptedPassword, iv) =
						_encryption.EncryptPassword(d.Password);

					var credential = DeviceCredential.Create(
						deviceId: device.Id,
						encryptedPassword: encryptedPassword,
						encryptionIV: iv,
						systemUser: d.SystemUser);

					await _deviceRepo.AddCredentialAsync(credential);

					deviceResponses.Add(new DeviceResponse
					{
						DeviceId = device.Id,
						DeviceType = d.DeviceType,
						SerialNumber = d.SerialNumber
					});
				}
				var (username, tempPass) =
					await _keycloak.CreateApplicantUserAsync(
						command.FullName,
						command.Email,
						command.Identification,
						ct);
				await _unitOfWork.CommitAsync();

				try
				{
					await _email.SendCredentialsAsync(
						command.Email,
						command.FullName,
						username,
						subject.Id,
						tempPass,
						ct);
				}
				catch
				{
					
				}

				return new RegisterSubjectResponse
				{
					SubjectId = subject.Id,
					Devices = deviceResponses,
					Message =
						$"Sujeto registrado. Usuario '{username}' creado en Keycloak. Se envió correo con credenciales."
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