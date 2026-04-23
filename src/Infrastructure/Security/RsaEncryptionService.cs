using SecureHub.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SecureHub.Infrastructure.Security
{
	public class RsaEncryptionService : IEncryptionService
	{
		private readonly RSA _rsaPublic;
		private readonly RSA _rsaPrivate;

		public RsaEncryptionService(string publicKey, string privateKey)
		{
			_rsaPublic = RSA.Create();
			_rsaPrivate = RSA.Create();

			ImportPublicKey(_rsaPublic, publicKey);
			ImportPrivateKey(_rsaPrivate, privateKey);
		}

		private static void ImportPublicKey(RSA rsa, string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Public key is null or empty");

			if (key.Contains("BEGIN PUBLIC KEY"))
			{
				rsa.ImportFromPem(key.ToCharArray());
			}
			else
			{
				var bytes = Convert.FromBase64String(key);
				rsa.ImportSubjectPublicKeyInfo(bytes, out _);
			}
		}

		private static void ImportPrivateKey(RSA rsa, string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Private key is null or empty");

			if (key.Contains("BEGIN"))
			{
				rsa.ImportFromPem(key.ToCharArray());
			}
			else
			{
				var bytes = Convert.FromBase64String(key);
				rsa.ImportPkcs8PrivateKey(bytes, out _);
			}
		}

		public string Encrypt(string plainText)
		{
			if (string.IsNullOrEmpty(plainText))
				return plainText;

			var bytes = Encoding.UTF8.GetBytes(plainText);
			var encrypted = _rsaPublic.Encrypt(bytes, RSAEncryptionPadding.OaepSHA256);
			return Convert.ToBase64String(encrypted);
		}

		public string Decrypt(string cipherText)
		{
			if (string.IsNullOrEmpty(cipherText))
				return cipherText;

			var bytes = Convert.FromBase64String(cipherText);
			var decrypted = _rsaPrivate.Decrypt(bytes, RSAEncryptionPadding.OaepSHA256);
			return Encoding.UTF8.GetString(decrypted);
		}

		public byte[] EncryptBytes(byte[] data)
		{
			using var aes = Aes.Create();
			aes.KeySize = 256;
			aes.GenerateKey();
			aes.GenerateIV();

			var encryptedAesKey = _rsaPublic.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);

			using var encryptor = aes.CreateEncryptor();
			var encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

			var result = new byte[4 + encryptedAesKey.Length + 16 + encryptedData.Length];

			BitConverter.GetBytes(encryptedAesKey.Length).CopyTo(result, 0);
			encryptedAesKey.CopyTo(result, 4);
			aes.IV.CopyTo(result, 4 + encryptedAesKey.Length);
			encryptedData.CopyTo(result, 4 + encryptedAesKey.Length + 16);

			return result;
		}

		public byte[] DecryptBytes(byte[] encryptedData)
		{
			var keyLength = BitConverter.ToInt32(encryptedData, 0);

			var encryptedAesKey = encryptedData[4..(4 + keyLength)];
			var iv = encryptedData[(4 + keyLength)..(4 + keyLength + 16)];
			var data = encryptedData[(4 + keyLength + 16)..];

			var aesKey = _rsaPrivate.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);

			using var aes = Aes.Create();
			aes.Key = aesKey;
			aes.IV = iv;

			using var decryptor = aes.CreateDecryptor();
			return decryptor.TransformFinalBlock(data, 0, data.Length);
		}

		public (byte[] encryptedPassword, byte[] iv) EncryptPassword(string password)
		{
			using var aes = Aes.Create();
			aes.KeySize = 256;
			aes.GenerateKey();
			aes.GenerateIV();

			using var encryptor = aes.CreateEncryptor();
			var encryptedBytes = encryptor.TransformFinalBlock(
				Encoding.UTF8.GetBytes(password), 0, password.Length);

			return (encryptedBytes, aes.IV);
		}

		public string DecryptPassword(byte[] encryptedPassword, byte[] iv)
		{
			throw new NotImplementedException("Usar Decrypt() para DeviceCredential migrado a RSA");
		}
	}
}