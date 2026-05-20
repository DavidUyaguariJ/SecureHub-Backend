using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Arco.Dtos;
using SecureHub.Infrastructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco
{
	public class GetArcoRequestsUseCase
	{
		private readonly IArcoRequestRepository _arcoRepo;
		private readonly IArcoAuditLogRepository _auditRepo;
		private readonly ISubjectRepository _subjectRepo;
		private readonly IEncryptionService _encryptionService;

		public GetArcoRequestsUseCase(
			IArcoRequestRepository arcoRepo,
			IArcoAuditLogRepository auditRepo,
			ISubjectRepository subjectRepo,
			IEncryptionService encryptionService)
		{
			_arcoRepo = arcoRepo;
			_auditRepo = auditRepo;
			_subjectRepo = subjectRepo;
			_encryptionService = encryptionService;
		}

		public async Task<IEnumerable<ArcoRequestResponseDto>> GetAllAsync(
			string? statusFilter, CancellationToken ct = default)
		{
			var requests = statusFilter is not null
				? await _arcoRepo.GetByStatusAsync(statusFilter, ct)
				: await _arcoRepo.GetAllAsync(ct);

			var result = new List<ArcoRequestResponseDto>();
			foreach (var r in requests)
			{
				var subject = await _subjectRepo.GetByIdIncludeDeletedAsync(r.SubjectId, ct);
				var maskedName = DecryptAndMask(subject?.FullName);
				result.Add(MapToDto(r, maskedName));
			}
			return result;
		}

		public async Task<ArcoRequestDetailDto?> GetDetailAsync(
			Guid requestId, CancellationToken ct = default)
		{
			var request = await _arcoRepo.GetByIdAsync(requestId, ct);
			if (request is null) return null;

			var subject = await _subjectRepo.GetByIdIncludeDeletedAsync(request.SubjectId, ct);
			var maskedName = DecryptAndMask(subject?.FullName);
			var logs = await _auditRepo.GetByRequestIdAsync(requestId, ct);

			return new ArcoRequestDetailDto(
				MapToDto(request, maskedName),
				logs.Select(l => new ArcoAuditLogDto(
					l.Id, l.Action, l.PreviousStatus, l.NewStatus,
					l.PerformedByRole, l.Notes, l.CreatedAt)));
		}

		public async Task<IEnumerable<ArcoRequestResponseDto>> GetBySubjectAsync(
			Guid subjectId, CancellationToken ct = default)
		{
			var subject = await _subjectRepo.GetByIdIncludeDeletedAsync(subjectId, ct);
			var maskedName = DecryptAndMask(subject?.FullName);
			var requests = await _arcoRepo.GetBySubjectIdAsync(subjectId, ct);
			return requests.Select(r => MapToDto(r, maskedName));
		}

		private string DecryptAndMask(string? encryptedName)
		{
			if (string.IsNullOrWhiteSpace(encryptedName)) return "—";
			try
			{
				var clear = _encryptionService.Decrypt(encryptedName);
				return MaskName(clear);
			}
			catch { return encryptedName; }
		}

		private static string MaskName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return "—";
			var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			return string.Join(" ", parts.Select(p =>
				p.Length <= 1 ? "X" : p[0] + new string('x', p.Length - 1)));
		}

		private static ArcoRequestResponseDto MapToDto(
			Domain.Entities.ArcoRequest r, string maskedName) =>
			new(r.Id, r.SubjectId, maskedName, r.RequestType, r.Status, r.Description,
				r.RequestedAt, r.DueDate, r.ResolvedAt, r.ResponseText, r.RejectedReason,
				r.ResponseFilePath is not null);
	}
}