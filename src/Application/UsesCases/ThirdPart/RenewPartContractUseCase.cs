using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.ThirdPart.Dtos;
using SecureHub.Domain.Entities;
using SecureHub.Infrastructure.Persistence.Repositories;

namespace SecureHub.Application.UsesCases.ThirdPart
{
	public class RenewPartContractUseCase
	{
		private readonly IPartContractRepository _repo;
		private readonly IKeycloakService _keycloak;
		private readonly IBlockchainService _blockchain;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IEncryptionService _encryption;
		private readonly IEmailService _email;
		private readonly ISubjectRepository _subjectRepo;

		public RenewPartContractUseCase(
			IPartContractRepository repo, IKeycloakService keycloak,
			IBlockchainService blockchain, IUnitOfWork unitOfWork,
			IEncryptionService encryption, IEmailService email,
			ISubjectRepository subjectRepository)
		{
			_repo = repo;
			_keycloak = keycloak;
			_blockchain = blockchain;
			_unitOfWork = unitOfWork;
			_encryption = encryption;
			_email = email;
			_subjectRepo = subjectRepository;
		}

		public async Task<PartContractDto> ExecuteAsync(
			Guid originalContractId, DateTimeOffset newValidFrom,
			DateTimeOffset newValidUntil, Guid operatorId,
			CancellationToken ct = default)
		{
			var original = await _repo.GetByIdAsync(originalContractId, ct)
				?? throw new InvalidOperationException("Contrato original no encontrado.");

			if (!string.IsNullOrWhiteSpace(original.KeycloakUsername))
			{
				try
				{
					var kcUuid = await _keycloak.GetUserIdByUsernameAsync(original.KeycloakUsername, ct);
					if (kcUuid is not null)
						await _keycloak.DeleteUserAsync(kcUuid, ct);
				}
				catch (Exception ex) { Console.WriteLine($"[RENEW KEYCLOAK DELETE ERROR] {ex.Message}"); }
			}

			var validUntilEnd = new DateTimeOffset(
				newValidUntil.Year, newValidUntil.Month, newValidUntil.Day,
				23, 59, 59, newValidUntil.Offset);

			var companyName = TryDecrypt(original.CompanyName, _encryption);
			var contactEmail = TryDecrypt(original.ContactEmail, _encryption);
			var purpose = TryDecrypt(original.PurposeDescription, _encryption);

			var newContract = PartContract.Create(
				original.CompanyName, original.ContactEmail, original.ContactPerson,
				original.SubjectId ?? Guid.Empty,
				original.PurposeDescription, original.AllowedFields,
				newValidFrom, validUntilEnd, operatorId);

			string username, tempPass;
			await _unitOfWork.BeginTransactionAsync();
			try
			{
				(username, tempPass) = await _keycloak.CreateExternalUserAsync(
					companyName, contactEmail, newContract.Id, validUntilEnd, ct);
				newContract.SetKeycloakUser(username, username);
				await _repo.AddAsync(newContract, ct);
				await _unitOfWork.CommitAsync();
			}
			catch { await _unitOfWork.RollbackAsync(); throw; }
			try
			{
				var tx = await _blockchain.RegisterThirdPartyAsync(
					newContract.Id, companyName, "",
					string.Join(",", newContract.AllowedFields),
					purpose, validUntilEnd, ct);
				newContract.SetBlockchainTx(tx);
				await _repo.UpdateAsync(newContract, ct);
				await _unitOfWork.SaveAsync(ct);
			}
			catch (Exception ex) { Console.WriteLine($"[RENEW BLOCKCHAIN ERROR] {ex.Message}"); }
			try
			{
				await _email.SendExternalCredentialsAsync(
					contactEmail, companyName,
					username, tempPass, original.SubjectId ?? Guid.Empty,
					validUntilEnd, ct);
			}
			catch (Exception ex) { Console.WriteLine($"[RENEW EMAIL ERROR] {ex.Message}"); }
			if (original.SubjectId.HasValue && original.SubjectId != Guid.Empty)
			{
				try
				{
					var subject = await _subjectRepo.GetByIdAsync(original.SubjectId.Value, ct);
					if (subject != null)
					{
						var subjectEmail = _encryption.Decrypt(subject.Email);
						var subjectName = _encryption.Decrypt(subject.FullName);
						await _email.SendSubjectThirdPartyNotificationAsync(
							subjectEmail, subjectName,
							companyName, purpose,
							newContract.AllowedFields, validUntilEnd, ct);
					}
				}
				catch (Exception ex) { Console.WriteLine($"[RENEW EMAIL SUBJECT NOTIFY ERROR] {ex.Message}"); }
			}

			return CreatePartContractUseCase.ToDto(newContract);
		}

		private static string TryDecrypt(string val, IEncryptionService enc)
		{
			try { return enc.Decrypt(val); } catch { return val; }
		}
	}
}