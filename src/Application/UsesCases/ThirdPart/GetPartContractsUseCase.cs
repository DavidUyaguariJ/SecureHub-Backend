using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.ThirdPart.Dtos;
using SecureHub.Infrastructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.ThirdPart
{
	/// <summary>
	/// Expone los datos de un Subject SOLO los campos que el contrato permite.
	/// Usado por el external_api_role para acceder a datos.
	/// </summary>
	public class GetPartContractsUseCase
	{
		private readonly IPartContractRepository _repo;
		private readonly IBlockchainService _blockchain;

		public GetPartContractsUseCase(IPartContractRepository repo, IBlockchainService blockchain)
		{ _repo = repo; _blockchain = blockchain; }

		public async Task<IEnumerable<PartContractDto>> GetAllAsync(
			string? statusFilter, CancellationToken ct = default)
			=> (await _repo.GetAllAsync(statusFilter, ct)).Select(CreatePartContractUseCase.ToDto);

		public async Task<PartContractDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
		{
			var c = await _repo.GetByIdAsync(id, ct);
			return c is null ? null : CreatePartContractUseCase.ToDto(c);
		}

		public async Task<(bool Active, DateTimeOffset ValidUntil, int Status)>
			GetBlockchainStatusAsync(Guid id, CancellationToken ct = default)
			=> await _blockchain.IsThirdPartyActiveAsync(id, ct);
	}
}
