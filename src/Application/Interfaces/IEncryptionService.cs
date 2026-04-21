using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IEncryptionService
	{
		(byte[] encryptedPassword, byte[] iv) Encrypt(string plainPassword);
	}
}
