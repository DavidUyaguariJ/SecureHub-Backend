using SecureHub.Application.Interfaces;
using SecureHub.Infrastructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.ThirdPart
{
	public class GetExternalSubjectDataUseCase
	{
		private readonly IPartContractRepository _contractRepo;
		private readonly ISubjectRepository _subjectRepo;
		private readonly IEncryptionService _encryption;
		private readonly IBlockchainService _blockchain;

		public GetExternalSubjectDataUseCase(
			IPartContractRepository contractRepo, ISubjectRepository subjectRepo,
			IEncryptionService encryption, IBlockchainService blockchain)
		{
			_contractRepo = contractRepo;
			_subjectRepo = subjectRepo;
			_encryption = encryption;
			_blockchain = blockchain;
		}

		public async Task<Dictionary<string, object?>> ExecuteAsync(
			Guid contractId, CancellationToken ct = default)
		{
			var contract = await _contractRepo.GetByIdAsync(contractId, ct)
				?? throw new UnauthorizedAccessException("Contrato no encontrado.");

			if (!contract.IsActive())
				throw new UnauthorizedAccessException($"Contrato {contract.Status}. Sin acceso.");

			if (!contract.SubjectId.HasValue)
				throw new InvalidOperationException("Contrato sin titular asociado.");

			var (bcActive, _, _) = await _blockchain.IsThirdPartyActiveAsync(contractId, ct);
			if (!bcActive)
				throw new UnauthorizedAccessException("Contrato revocado en blockchain.");

			var subject = await _subjectRepo.GetByIdWithDevicesAsync(contract.SubjectId.Value, ct)
							?? throw new KeyNotFoundException("Titular no encontrado.");

			var result = new Dictionary<string, object?>();
			foreach (var field in contract.AllowedFields)
			{
				result[field] = field switch
				{
					"full_name" => Dec(subject.FullName),
					"email" => Dec(subject.Email),
					"phone" => Dec(subject.Phone),
					"address" => Dec(subject.Address),
					"identification" => Dec(subject.Identification),
					"subject_type" => subject.SubjectType,
					"contact_person" => Dec(subject.ContactPerson),
					"devices" => subject.Devices
						.Where(d => !d.IsDeleted)
						.Select(d => new
						{
							d.DeviceType,
							d.Brand,
							d.Model,
							SerialNumber = Dec(d.SerialNumber)
						}),
					_ => null
				};
			}

			try
			{
				await _blockchain.RecordAuditAsync(contractId, "PART_CONTRACT",
					"DATA_ACCESS", "", $"subject:{contract.SubjectId.Value}",
					contract.KeycloakUsername ?? contractId.ToString(), "", ct);
			}
			catch { }

			return result;
		}

		private string? Dec(string? val)
		{
			if (val is null) return null;
			try { return _encryption.Decrypt(val); } catch { return val; }
		}
		public async Task<Dictionary<string, object?>> ExecuteBySubjectAsync(Guid subjectId, CancellationToken ct = default)
		{
			var subject = await _subjectRepo.GetByIdWithDevicesAsync(subjectId, ct) ?? throw new KeyNotFoundException("Titular no encontrado.");
			return new Dictionary<string, object?>
			{
				["full_name"] = Dec(subject.FullName),
				["email"] = Dec(subject.Email),
				["phone"] = Dec(subject.Phone),
				["address"] = Dec(subject.Address),
				["identification"] = Dec(subject.Identification),
				["subject_type"] = subject.SubjectType,
				["contact_person"] = Dec(subject.ContactPerson)
			};
		}
	}
}
