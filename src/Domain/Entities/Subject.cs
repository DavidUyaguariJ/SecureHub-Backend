using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Domain.Entities
{
	public class Subject
	{
		public Guid Id { get; private set; }
		public string Identification { get; private set; } = null!;
		public string FullName { get; private set; } = null!;
		public string? Phone { get; private set; }
		public string? Address { get; private set; }
		public string Email { get; private set; } = null!;
		public string? SubjectType { get; private set; }
		public string? ContactPerson { get; private set; }
		public bool IsDeleted { get; private set; }
		public DateTime? DeletedAt { get; private set; }
		public DateTime CreatedAt { get; private set; }

		public ICollection<Device> Devices { get; private set; } = new List<Device>();
		public ICollection<BiometricAuth> BiometricAuths { get; private set; } = new List<BiometricAuth>();

		private Subject() { }

		public static Subject Create(
			string identification,
			string fullName,
			string email,
			string? phone = null,
			string? address = null,
			string? subjectType = null,
			string? contactPerson = null)
		{
			return new Subject
			{
				Id = Guid.NewGuid(),
				Identification = identification,
				FullName = fullName,
				Email = email,
				Phone = phone,
				Address = address,
				SubjectType = subjectType,
				ContactPerson = contactPerson,
				IsDeleted = false,
				CreatedAt = DateTime.UtcNow
			};
		}

		public void SoftDelete()
		{
			IsDeleted = true;
			DeletedAt = DateTime.UtcNow;
		}

		public void UpdatePersonalData(
			string? fullName = null,
			string? email = null,
			string? phone = null,
			string? address = null)
		{
			if (fullName is not null) FullName = fullName;
			if (email is not null) Email = email;
			if (phone is not null) Phone = phone;
			if (address is not null) Address = address;
		}

		public void MaskPersonalData()
		{
			Identification = MaskId(Identification);
			FullName = MaskName(FullName);
			Email = MaskEmail(Email);
			Phone = MaskPhone(Phone);
			Address = "DATO ELIMINADO";
		}
		private static string MaskId(string id)
		{
			if (string.IsNullOrWhiteSpace(id)) return "XXXXXXXXXX";
			return id.Length <= 4
				? new string('X', id.Length)
				: new string('X', id.Length - 4) + id[^4..];
		}

		private static string MaskName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return "XXXXX";
			var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			return string.Join(" ", System.Linq.Enumerable.Select(parts, p =>
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