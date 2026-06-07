namespace SecureHub.Application.Interfaces
{
	public interface IEmailService
	{
		Task SendCredentialsAsync(
			string toEmail, string fullName,
			string username, Guid subjectId, string temporaryPassword,
			CancellationToken ct = default);

		Task SendArcoCreatedAsync(
			string toEmail, string fullName,
			string requestType, Guid requestId,
			DateTimeOffset? dueDate,
			CancellationToken ct = default);

		Task SendArcoStatusChangedAsync(
			string toEmail, string fullName,
			string requestType, string newStatus,
			string? responseText, string? rejectedReason,
			CancellationToken ct = default);

		Task SendArcoResolutionWithPdfAsync(
			string toEmail, string fullName,
			string requestType, string resolution,
			byte[] pdfBytes, string pdfFileName,
			CancellationToken ct = default);

		Task SendExternalCredentialsAsync(
			string toEmail, string companyName,
			string username, string temporaryPassword,
			Guid subjectId,
			DateTimeOffset validUntil,
			CancellationToken ct = default);
		Task SendSubjectThirdPartyNotificationAsync(
			string subjectEmail, string subjectName,
			string companyName, string purposeDescription,
			string[] allowedFields, DateTimeOffset validUntil,
			CancellationToken ct = default);
	}
}