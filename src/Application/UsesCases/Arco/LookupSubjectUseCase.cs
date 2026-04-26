using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Arco.Dtos;
using SecureHub.Infrastructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco
{
	public class LookupSubjectUseCase
	{
		private readonly ISubjectRepository _subjectRepo;
		private readonly IBiometricAuthRepository _biometricRepo;
		private readonly IEncryptionService _encryptionService;

		public LookupSubjectUseCase(
			ISubjectRepository subjectRepo,
			IBiometricAuthRepository biometricRepo,
			IEncryptionService encryptionService)
		{
			_subjectRepo = subjectRepo;
			_biometricRepo = biometricRepo;
			_encryptionService = encryptionService;
		}

		public async Task<SubjectLookupDto?> ExecuteAsync(
			string identification, CancellationToken ct = default)
		{
			var subject = await _subjectRepo.FindByDecryptedIdentificationAsync(identification, ct);
			if (subject is null) return null;

			var biometric = await _biometricRepo.GetLatestBySubjectIdAsync(subject.Id, ct);
			var clearFullName = TryDecrypt(subject.FullName, _encryptionService);
			var clearEmail = TryDecrypt(subject.Email, _encryptionService);
			var clearPhone = subject.Phone is not null
				? TryDecrypt(subject.Phone, _encryptionService)
				: null;

			return new SubjectLookupDto(
				subject.Id,
				identification,
				MaskName(clearFullName),
				MaskEmail(clearEmail),
				MaskPhone(clearPhone),
				biometric is not null);
		}

		private static string TryDecrypt(string value, IEncryptionService enc)
		{
			try { return enc.Decrypt(value); }
			catch { return value; }
		}

		private static string MaskName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return "XXXXX";
			var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			return string.Join(" ", parts.Select(p =>
				p.Length <= 1 ? "X" : p[0] + new string('x', p.Length - 1)));
		}

		private static string MaskEmail(string email)
		{
			if (string.IsNullOrWhiteSpace(email)) return "***@***.***";
			var idx = email.IndexOf('@');
			if (idx <= 0) return "***@***.***";
			return email[0] + new string('*', Math.Max(idx - 1, 1)) + email[idx..];
		}

		private static string? MaskPhone(string? phone)
		{
			if (string.IsNullOrWhiteSpace(phone)) return null;
			return phone.Length <= 4
				? new string('*', phone.Length)
				: new string('*', phone.Length - 4) + phone[^4..];
		}
	}
}
