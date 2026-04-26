using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Domain.Entities
{
	public class ArcoRequest
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public Guid SubjectId { get; set; }
		public string RequestType { get; set; } = string.Empty;
		public string Status { get; set; } = "PENDIENTE";
		public string? Description { get; set; }
		public Guid? BiometricAuthId { get; set; }
		public string? ResponseText { get; set; }
		public string? ResponseFilePath { get; set; }
		public string? RejectedReason { get; set; }
		public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
		public DateTimeOffset? DueDate { get; set; }
		public DateTimeOffset? ResolvedAt { get; set; }
		public Guid? ResolvedBy { get; set; }
		public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

		public Subject? Subject { get; set; }
		public ICollection<ArcoAuditLog> AuditLogs { get; set; } = new List<ArcoAuditLog>();
	}
}
