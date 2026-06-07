using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Domain.Entities
{
	public class PartContract
	{
		public Guid Id { get; private set; }
		public string CompanyName { get; private set; } = null!;
		public string ContactEmail { get; private set; } = null!;
		public string? ContactPerson { get; private set; }
		public string PurposeDescription { get; private set; } = null!;
		public string[] AllowedFields { get; private set; } = [];
		public string? ContractFilePath { get; private set; }
		public string? ContractHash { get; private set; }
		public DateTimeOffset ValidFrom { get; private set; }
		public DateTimeOffset ValidUntil { get; private set; }
		public string Status { get; private set; } = "ACTIVO";
		public string? KeycloakUserId { get; private set; }
		public string? KeycloakUsername { get; private set; }
		public string? BlockchainTxHash { get; private set; }
		public Guid? CreatedBy { get; private set; }
		public Guid? RevokedBy { get; private set; }
		public DateTimeOffset? RevokedAt { get; private set; }
		public string? RevokedReason { get; private set; }
		public bool IsDeleted { get; private set; }
		public DateTime CreatedAt { get; private set; }
		public DateTime DeletedAt { get; private set; }
		public Guid? SubjectId { get; private set; }

		private PartContract() { }

		public static PartContract Create(
			string companyName, string contactEmail, string? contactPerson, Guid subjectId,
			string purpose, string[] allowedFields,
			DateTimeOffset validFrom, DateTimeOffset validUntil,
			Guid? createdBy = null)
		{
			if (validUntil <= validFrom)
				throw new ArgumentException("ValidUntil debe ser posterior a ValidFrom.");

			return new PartContract
			{
				Id = Guid.NewGuid(),
				CompanyName = companyName,
				ContactEmail = contactEmail,
				ContactPerson = contactPerson,
				PurposeDescription = purpose,
				AllowedFields = allowedFields,
				ValidFrom = validFrom,
				ValidUntil = validUntil,
				Status = "ACTIVO",
				CreatedBy = createdBy,
				CreatedAt = DateTime.UtcNow,
				SubjectId = subjectId
			};
		}

		public void SetKeycloakUser(string userId, string username)
		{
			KeycloakUserId = userId;
			KeycloakUsername = username;
		}

		public void SetBlockchainTx(string txHash) => BlockchainTxHash = txHash;

		public void Revoke(Guid revokedBy, string reason)
		{
			if (Status == "REVOCADO")
				throw new InvalidOperationException("Contrato ya revocado.");
			Status = "REVOCADO";
			RevokedBy = revokedBy;
			RevokedAt = DateTimeOffset.UtcNow;
			RevokedReason = reason;
		}

		public void Suspend()
		{
			if (Status == "REVOCADO") throw new InvalidOperationException("Contrato ya revocado.");
			Status = "SUSPENDIDO";
		}

		public void Reactivate()
		{
			if (Status == "REVOCADO") throw new InvalidOperationException("No se puede reactivar un contrato revocado.");
			Status = "ACTIVO";
		}

		public bool IsActive() =>
			Status == "ACTIVO" && ValidUntil >= DateTimeOffset.UtcNow;
	}
}