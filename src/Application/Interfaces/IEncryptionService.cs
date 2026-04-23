using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IEncryptionService
	{
		string Encrypt(string plainText);
		string Decrypt(string cipherText);
		byte[] EncryptBytes(byte[] data);
		byte[] DecryptBytes(byte[] encryptedData);
		(byte[] encryptedPassword, byte[] iv) EncryptPassword(string password);
		string DecryptPassword(byte[] encryptedPassword, byte[] iv);
	}
}

