using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IEmailService
	{
		Task SendCredentialsAsync(
			string toEmail,
			string fullName,
			string username,
			string temporaryPassword,
			CancellationToken ct = default);

		Task SendArcoStatusChangedAsync(
			string toEmail,
			string fullName,
			string requestType,
			string newStatus,
			string? responseText,
			string? rejectedReason,
			CancellationToken ct = default);

		Task SendArcoResolutionWithPdfAsync(
			string toEmail,
			string fullName,
			string requestType,
			string resolution,
			byte[] pdfBytes,
			string pdfFileName,
			CancellationToken ct = default);
	}
}
