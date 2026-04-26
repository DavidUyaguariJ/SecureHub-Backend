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
	public class UpdateArcoStatusUseCase
	{
		private readonly IArcoRequestRepository _arcoRepo;
		private readonly IArcoAuditLogRepository _auditRepo;
		private readonly ISubjectRepository _subjectRepo;
		private readonly IEncryptionService _encryptionService;
		private readonly IUnitOfWork _unitOfWork;

		public UpdateArcoStatusUseCase(IArcoRequestRepository arcoRepo,IArcoAuditLogRepository auditRepo,ISubjectRepository subjectRepo,IEncryptionService encryptionService,IUnitOfWork unitOfWork)
		{
			_arcoRepo = arcoRepo;
			_auditRepo = auditRepo;
			_subjectRepo = subjectRepo;
			_encryptionService = encryptionService;
			_unitOfWork = unitOfWork;
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
					throw new InvalidOperationException(
						$"La solicitud ya está en estado final: {request.Status}");

				var validStatuses = new[] { "EN_PROCESO", "COMPLETADO", "RECHAZADO" };
				if (!validStatuses.Contains(dto.NewStatus))
					throw new InvalidOperationException(
						$"Estado no válido: {dto.NewStatus}. Valores permitidos: {string.Join(", ", validStatuses)}");

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
				if (dto.NewStatus == "COMPLETADO")
				{
					var subject = await _subjectRepo.GetByIdAsync(request.SubjectId, ct);
					if (subject is not null)
					{
						switch (request.RequestType)
						{
							case "CANCELACION":
								subject.MaskPersonalData();
								subject.SoftDelete();
								break;

							case "RECTIFICACION":
								if (!string.IsNullOrWhiteSpace(dto.ResponseText))
								{
									try
									{
										var updateData = JsonSerializer.Deserialize<UpdateSubjectDataDto>(
											dto.ResponseText,
											new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

										if (updateData is not null)
											subject.UpdatePersonalData(
												fullName: updateData.FullName is not null ? _encryptionService.Encrypt(updateData.FullName) : null,
												email: updateData.Email is not null ? _encryptionService.Encrypt(updateData.Email) : null,
												phone: updateData.Phone is not null ? _encryptionService.Encrypt(updateData.Phone) : null,
												address: updateData.Address is not null ? _encryptionService.Encrypt(updateData.Address) : null
											);
									}
									catch (JsonException)
									{
									}
								}
								break;
						}
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
					IpAddress = requesterIp,
					Notes = dto.NewStatus == "RECHAZADO"
						? $"Motivo: {dto.RejectedReason}"
						: dto.ResponseText,
					CreatedAt = DateTimeOffset.UtcNow
				}, ct);

				await _unitOfWork.CommitAsync();
				var subjectFinal = await _subjectRepo.GetByIdAsync(request.SubjectId, ct);
				return new ArcoRequestResponseDto(
					request.Id,
					request.SubjectId,
					subjectFinal?.FullName ?? string.Empty,
					request.RequestType,
					request.Status,
					request.Description,
					request.RequestedAt,
					request.DueDate,
					request.ResolvedAt,
					request.ResponseText,
					request.RejectedReason,
					request.ResponseFilePath is not null);
			}
			catch
			{
				await _unitOfWork.RollbackAsync();
				throw;
			}
		}
	}
}
