using SecureHub.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SecureHub.Infrastructure.Security
{
	public class AesEncryptionService : IEncryptionService
	{
		public (byte[] encryptedPassword, byte[] iv) Encrypt(string plainPassword)
		{
			using var aes = Aes.Create();
			aes.GenerateKey();
			aes.GenerateIV();
			using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
			using var ms = new MemoryStream();
			using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
			using var sw = new StreamWriter(cs);
			sw.Write(plainPassword);
			sw.Flush();
			cs.FlushFinalBlock();
			var encrypted = ms.ToArray();
			var combined = new byte[aes.Key.Length + encrypted.Length];
			Buffer.BlockCopy(aes.Key, 0, combined, 0, aes.Key.Length);
			Buffer.BlockCopy(encrypted, 0, combined, aes.Key.Length, encrypted.Length);
			return (combined, aes.IV);
		}
	}
}
