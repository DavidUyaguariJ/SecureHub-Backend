using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.ThirdPart.Dtos;
using SecureHub.Domain.Entities;
using SecureHub.Infrastructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.ThirdPart
{
	public class RevokePartContractUseCase
	{
		private readonly IPartContractRepository _repo;
		private readonly IKeycloakService _keycloak;
		private readonly IBlockchainService _blockchain;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IEncryptionService _encryption;

		public RevokePartContractUseCase(
			IPartContractRepository repo, IKeycloakService keycloak,
			IBlockchainService blockchain, IUnitOfWork unitOfWork,
			IEncryptionService encryption)
		{
			_repo = repo;
			_keycloak = keycloak;
			_blockchain = blockchain;
			_unitOfWork = unitOfWork;
			_encryption = encryption;
		}

		public async Task<PartContractDto> ExecuteAsync(
			Guid contractId, Guid operatorId, string reason, CancellationToken ct = default)
		{
			var contract = await _repo.GetByIdAsync(contractId, ct)
				?? throw new InvalidOperationException("Contrato no encontrado.");

			await _unitOfWork.BeginTransactionAsync();
			try
			{
				contract.Revoke(operatorId, reason);
				await _repo.UpdateAsync(contract, ct);
				await _unitOfWork.CommitAsync();
			}
			catch { await _unitOfWork.RollbackAsync(); throw; }
			if (!string.IsNullOrWhiteSpace(contract.KeycloakUsername))
			{
				try
				{
					var kcUuid = await _keycloak.GetUserIdByUsernameAsync(contract.KeycloakUsername, ct);
					if (kcUuid is not null)
						await _keycloak.DeleteUserAsync(kcUuid, ct);
				}
				catch (Exception ex) { Console.WriteLine($"[REVOKE KEYCLOAK ERROR] {ex.Message}"); }
			}
			try
			{
				await _blockchain.UpdateThirdPartyStatusAsync(contractId, 2, ct);
				await _blockchain.RecordAuditAsync(contractId, "PART_CONTRACT",
					"REVOCADO", "ACTIVO", "REVOCADO", operatorId.ToString(), "", ct);
			}
			catch (Exception ex) { Console.WriteLine($"[REVOKE BLOCKCHAIN ERROR] {ex.Message}"); }

			return CreatePartContractUseCase.ToDto(contract);
		}
	}

}