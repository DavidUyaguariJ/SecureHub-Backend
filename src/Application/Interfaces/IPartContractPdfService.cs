using SecureHub.Application.UsesCases.ThirdPart.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IPartContractPdfService
	{
		Task<byte[]> GenerateAsync(
			PartContractDto dto,
			bool blockchainActive,
			DateTimeOffset blockchainValidUntil,
			int blockchainStatus,
			CancellationToken ct = default);
	}
}
