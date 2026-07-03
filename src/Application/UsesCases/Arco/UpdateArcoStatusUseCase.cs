using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Arco.Dtos;
using SecureHub.Domain.Entities;
using SecureHub.Infrastructure.Persistence.Repositories;
using System.Text.Json;

namespace SecureHub.Application.UsesCases.Arco
{
	public class UpdateArcoStatusUseCase
	{
		private readonly IArcoRequestRepository _arcoRepo;
		private readonly IArcoAuditLogRepository _auditRepo;
		private readonly IDeviceRepository _deviceRepo;
		private readonly ISubjectRepository _subjectRepo;
		private readonly IEncryptionService _encryptionService;
		private readonly IEmailService _emailService;
		private readonly IArcoResponsePdfService _pdfService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IBlockchainService _blockchain;

		private static string HashString(string? value)
		{
			if (string.IsNullOrEmpty(value)) return "";
			using var sha = System.Security.Cryptography.SHA256.Create();
			var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
			return "sha256:" + Convert.ToHexString(bytes).ToLower();
		}

		public UpdateArcoStatusUseCase(
			IArcoRequestRepository arcoRepo,
			IArcoAuditLogRepository auditRepo,
			ISubjectRepository subjectRepo,
			IEncryptionService encryptionService,
			IEmailService emailService,
			IArcoResponsePdfService pdfService,
			IDeviceRepository deviceRepo,
			IUnitOfWork unitOfWork,
			IBlockchainService blockchain)
		{
			_arcoRepo = arcoRepo;
			_auditRepo = auditRepo;
			_subjectRepo = subjectRepo;
			_encryptionService = encryptionService;
			_emailService = emailService;
			_pdfService = pdfService;
			_unitOfWork = unitOfWork;
			_blockchain = blockchain;
			_deviceRepo = deviceRepo;
		}

		public async Task<ArcoRequestResponseDto> ExecuteAsync(
			Guid requestId,
			UpdateArcoStatusDto dto,
			Guid operatorId,
			string requesterIp,
			CancellationToken ct = default)
		{
			await _unitOfWork.BeginTransactionAsync();
			try
			{
				var request = await _arcoRepo.GetByIdAsync(requestId, ct)
					?? throw new InvalidOperationException("Solicitud ARCO no encontrada.");

				if (request.Status is "COMPLETADO" or "RECHAZADO")
					throw new InvalidOperationException($"La solicitud ya está en estado final: {request.Status}");

				var validStatuses = new[] { "EN_PROCESO", "COMPLETADO", "RECHAZADO" };
				if (!validStatuses.Contains(dto.NewStatus))
					throw new InvalidOperationException($"Estado no válido: {dto.NewStatus}.");

				if (dto.NewStatus == "RECHAZADO" && string.IsNullOrWhiteSpace(dto.RejectedReason))
					throw new InvalidOperationException("Debe indicar el motivo del rechazo.");

				var previousStatus = request.Status;
				request.Status = dto.NewStatus;
				request.ResponseText = dto.ResponseText;
				request.RejectedReason = dto.RejectedReason;

				if (dto.NewStatus is "COMPLETADO" or "RECHAZADO")
				{
					request.ResolvedAt = DateTimeOffset.UtcNow;
					request.ResolvedBy = operatorId;
				}

				var subject = await _subjectRepo.GetByIdAsync(request.SubjectId, ct);

				if (dto.NewStatus == "COMPLETADO" && subject is not null)
				{
					switch (request.RequestType)
					{
						case "CANCELACION":
							subject.MaskPersonalData();
							subject.SoftDelete();
							break;

						case "RECTIFICACION":
							if (!string.IsNullOrWhiteSpace(request.Description))
							{
								try
								{
									var jsonPart = request.Description.Contains('|')
										? request.Description.Split('|', 2)[0]
										: request.Description;

									var updateData = JsonSerializer.Deserialize<UpdateSubjectDataDto>(
										jsonPart,
										new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

									if (updateData is not null)
									{
										subject.UpdatePersonalData(
											fullName: updateData.FullName is not null ? _encryptionService.Encrypt(updateData.FullName) : null,
											email: updateData.Email is not null ? _encryptionService.Encrypt(updateData.Email) : null,
											phone: updateData.Phone is not null ? _encryptionService.Encrypt(updateData.Phone) : null,
											address: updateData.Address is not null ? _encryptionService.Encrypt(updateData.Address) : null
										);
									}
									if (updateData?.Devices is not null && updateData.Devices.Any())
									{
										var devices = await _deviceRepo.GetBySubjectIdAsync(subject.Id, ct);

										foreach (var upd in updateData.Devices.Where(d => d.Id != Guid.Empty))
										{
											var device = devices.FirstOrDefault(d => d.Id == upd.Id);
											if (device is null) continue;
											device.Update(
												brand: upd.Brand,
												model: upd.Model,
												serialNumber: upd.SerialNumber is not null
													? _encryptionService.Encrypt(upd.SerialNumber)
													: null
											);
										}

										await _deviceRepo.SaveChangesAsync();
									}
								}
								catch (JsonException) { }
							}
							break;

						case "ACCESO":
						case "OPOSICION":
						case "PORTABILIDAD":
							break;
					}
				}

				await _auditRepo.AddAsync(new ArcoAuditLog
				{
					ArcoRequestId = request.Id,
					Action = dto.NewStatus,
					PreviousStatus = previousStatus,
					NewStatus = dto.NewStatus,
					PerformedBy = operatorId,
					PerformedByRole = dto.OperatorRole ?? "OPERADOR",
					PerformedByName = dto.OperatorName,
					IpAddress = requesterIp,
					Notes = dto.NewStatus == "RECHAZADO"
						? "Motivo: " + dto.RejectedReason
						: dto.ResponseText,
					CreatedAt = DateTimeOffset.UtcNow
				}, ct);

				await _unitOfWork.CommitAsync();

				await _blockchain.RecordAuditAsync(
					entityId: request.Id,
					entityType: "ARCO_REQUEST",
					action: "STATUS_CHANGED",
					previousState: previousStatus,
					newState: dto.NewStatus,
					operatorRef: operatorId.ToString(),
					ipHash: HashString(requesterIp),
					ct: ct);

				if (dto.NewStatus is "COMPLETADO" or "RECHAZADO")
					await _blockchain.UpdateArcoStatusAsync(
						arcoRequestId: request.Id,
						newStatus: dto.NewStatus,
						resolutionHash: "",
						ct: ct);
				if (subject is not null)
				{
					var subjectEmail = TryDecrypt(subject.Email);
					var subjectName = TryDecrypt(subject.FullName);

					try
					{
						await _emailService.SendArcoStatusChangedAsync(
							subjectEmail, subjectName,
							request.RequestType, dto.NewStatus,
							dto.ResponseText, dto.RejectedReason, ct);
					}
					catch { /* best-effort */ }

					if (dto.NewStatus is "COMPLETADO" or "RECHAZADO")
					{
						try
						{
							var logs = await _auditRepo.GetByRequestIdAsync(request.Id, ct);
							var detailDto = new ArcoRequestDetailDto(
								new ArcoRequestResponseDto(
									request.Id, request.SubjectId, subjectName,
									request.RequestType, request.Status, request.Description,
									request.RequestedAt, request.DueDate, request.ResolvedAt,
									request.ResponseText, request.RejectedReason,
									request.ResponseFilePath is not null),
								logs.Select(l => new ArcoAuditLogDto(
									l.Id, l.Action, l.PreviousStatus, l.NewStatus,
									l.PerformedByRole, l.PerformedByName, l.Notes, l.CreatedAt)));

							var pdfBytes = _pdfService.Generate(detailDto);
							var fileName = "respuesta-arco-" + request.Id.ToString()[..8].ToUpper() + ".pdf";

							await _emailService.SendArcoResolutionWithPdfAsync(
								subjectEmail, subjectName,
								request.RequestType, dto.NewStatus,
								pdfBytes, fileName, ct);
						}
						catch { /* best-effort */ }
					}
				}

				var subjectFinal = await _subjectRepo.GetByIdAsync(request.SubjectId, ct);
				var maskedName = subjectFinal is not null
					? MaskName(TryDecrypt(subjectFinal.FullName))
					: string.Empty;

				return new ArcoRequestResponseDto(
					request.Id, request.SubjectId, maskedName,
					request.RequestType, request.Status, request.Description,
					request.RequestedAt, request.DueDate, request.ResolvedAt,
					request.ResponseText, request.RejectedReason,
					request.ResponseFilePath is not null);
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

		private static string MaskName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return "—";
			var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			return string.Join(" ", parts.Select(p =>
				p.Length <= 1 ? "X" : p[0] + new string('x', p.Length - 1)));
		}
	}
}