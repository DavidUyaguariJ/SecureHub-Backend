using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Domain.Entities
{
	public class ArcoAuditLog
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public Guid ArcoRequestId { get; set; }
		public string Action { get; set; } = string.Empty;
		public string? PreviousStatus { get; set; }
		public string? NewStatus { get; set; }
		public Guid? PerformedBy { get; set; }
		public string? PerformedByRole { get; set; }
		public string? PerformedByName { get; set; }
		public string? Notes { get; set; }
		public string? IpAddress { get; set; }
		public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

		public ArcoRequest? ArcoRequest { get; set; }
	}
}
