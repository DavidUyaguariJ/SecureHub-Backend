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

		public GetArcoRequestsUseCase(
			IArcoRequestRepository arcoRepo,
			IArcoAuditLogRepository auditRepo,
			ISubjectRepository subjectRepo)
		{
			_arcoRepo = arcoRepo;
			_auditRepo = auditRepo;
			_subjectRepo = subjectRepo;
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
				var subject = await _subjectRepo.GetByIdAsync(r.SubjectId, ct);
				result.Add(MapToDto(r, subject?.FullName ?? string.Empty));
			}
			return result;
		}

		public async Task<ArcoRequestDetailDto?> GetDetailAsync(
			Guid requestId, CancellationToken ct = default)
		{
			var request = await _arcoRepo.GetByIdAsync(requestId, ct);
			if (request is null) return null;

			var subject = await _subjectRepo.GetByIdAsync(request.SubjectId, ct);
			var logs = await _auditRepo.GetByRequestIdAsync(requestId, ct);

			return new ArcoRequestDetailDto(
				MapToDto(request, subject?.FullName ?? string.Empty),
				logs.Select(l => new ArcoAuditLogDto(
					l.Id, l.Action, l.PreviousStatus, l.NewStatus,
					l.PerformedByRole, l.Notes, l.CreatedAt)));
		}

		public async Task<IEnumerable<ArcoRequestResponseDto>> GetBySubjectAsync(
			Guid subjectId, CancellationToken ct = default)
		{
			var subject = await _subjectRepo.GetByIdAsync(subjectId, ct);
			var requests = await _arcoRepo.GetBySubjectIdAsync(subjectId, ct);
			return requests.Select(r => MapToDto(r, subject?.FullName ?? string.Empty));
		}

		private static ArcoRequestResponseDto MapToDto(
			Domain.Entities.ArcoRequest r, string fullName) =>
			new(r.Id, r.SubjectId, fullName, r.RequestType, r.Status, r.Description,
				r.RequestedAt, r.DueDate, r.ResolvedAt, r.ResponseText, r.RejectedReason,
				r.ResponseFilePath is not null);
	}
}
