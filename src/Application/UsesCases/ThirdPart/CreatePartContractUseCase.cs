using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.ThirdPart.Dtos;
using SecureHub.Domain.Entities;
using SecureHub.Infrastructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.ThirdPart
{
	public class CreatePartContractUseCase
	{
		private readonly IPartContractRepository _repo;
		private readonly IKeycloakService _keycloak;
		private readonly IBlockchainService _blockchain;
		private readonly IUnitOfWork _unitOfWork;
		private readonly ISubjectRepository _subjectRepo;
		private readonly IEncryptionService _encryptionService;
		private readonly IEmailService _email;

		public CreatePartContractUseCase(
			IPartContractRepository repo, IKeycloakService keycloak,
			IBlockchainService blockchain, IUnitOfWork unitOfWork,
			IEmailService email, IEncryptionService encryptionService,
			ISubjectRepository subjectRepository)
		{
			_repo = repo;
			_keycloak = keycloak;
			_blockchain = blockchain;
			_unitOfWork = unitOfWork;
			_email = email;
			_encryptionService = encryptionService;
			_subjectRepo = subjectRepository;
		}

		public async Task<PartContractDto> ExecuteAsync(
			CreatePartContractCommand cmd, Guid operatorId, CancellationToken ct = default)
		{
			var invalid = cmd.AllowedFields.Except(AllowedFieldOptions.Fields.Keys).ToList();
			if (invalid.Any())
				throw new ArgumentException($"Campos no permitidos: {string.Join(", ", invalid)}");

			await _unitOfWork.BeginTransactionAsync();
			try
			{
				var validUntilEndOfDay = new DateTimeOffset(
					cmd.ValidUntil.Year, cmd.ValidUntil.Month, cmd.ValidUntil.Day,
					23, 59, 59, cmd.ValidUntil.Offset);

				var contract = PartContract.Create(cmd.CompanyName, cmd.ContactEmail, cmd.ContactPerson, cmd.SubjectId, cmd.PurposeDescription, cmd.AllowedFields,
					cmd.ValidFrom, validUntilEndOfDay, operatorId);

				var (username, tempPass) = await _keycloak.CreateExternalUserAsync(
					cmd.CompanyName, cmd.ContactEmail, contract.Id, validUntilEndOfDay, ct);
				contract.SetKeycloakUser(username, username);
				await _repo.AddAsync(contract, ct);
				await _unitOfWork.CommitAsync();
				try
				{
					var tx = await _blockchain.RegisterThirdPartyAsync(
						contract.Id, cmd.CompanyName, "",
						string.Join(",", cmd.AllowedFields),
						cmd.PurposeDescription, validUntilEndOfDay, ct);
					contract.SetBlockchainTx(tx);
					await _repo.UpdateAsync(contract, ct);
					await _unitOfWork.SaveAsync(ct);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[BLOCKCHAIN ERROR] {ex.Message}");
				}
				try
				{
					await _email.SendExternalCredentialsAsync(
						cmd.ContactEmail, cmd.CompanyName,
						username, tempPass, cmd.SubjectId,
						validUntilEndOfDay, ct);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
				}
				if (cmd.SubjectId != Guid.Empty)
				{
					try
					{
						var subject = await _subjectRepo.GetByIdAsync(cmd.SubjectId, ct);
						if (subject != null)
						{
							var subjectEmail = _encryptionService.Decrypt(subject.Email);
							var subjectName = _encryptionService.Decrypt(subject.FullName);
							await _email.SendSubjectThirdPartyNotificationAsync(
								subjectEmail, subjectName,
								cmd.CompanyName, cmd.PurposeDescription,
								cmd.AllowedFields, validUntilEndOfDay, ct);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"[EMAIL SUBJECT NOTIFY ERROR] {ex.Message}");
					}
				}

				return ToDto(contract);
			}
			catch
			{
				await _unitOfWork.RollbackAsync();
				throw;
			}
		}

		public static PartContractDto ToDto(PartContract c) => new(
			c.Id, c.CompanyName, c.ContactEmail, c.ContactPerson,
			c.PurposeDescription, c.AllowedFields, c.ValidFrom, c.ValidUntil,
			c.Status, c.KeycloakUsername, c.BlockchainTxHash,
			c.IsActive(), c.RevokedAt, c.RevokedReason, c.CreatedAt);
	}
}