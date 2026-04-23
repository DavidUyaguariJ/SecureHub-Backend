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
	}
}