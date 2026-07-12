using System;
using System.Security.Cryptography;
using System.Text;
using SecureHub.Infrastructure.Security;
using Xunit;

namespace SecureHub.Tests
{
    // CP-02 (RNF-01 Seguridad: cifrado de datos sensibles)
    public class RsaEncryptionServiceTests
    {
        private static (string publicKeyPem, string privateKeyPem) GenerateTestKeyPair()
        {
            using var rsa = RSA.Create(2048);
            var publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
            var privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
            return (publicKeyPem, privateKeyPem);
        }

        [Fact]
        public void Encrypt_Then_Decrypt_ReturnsOriginalPlainText()
        {
            var (publicKey, privateKey) = GenerateTestKeyPair();
            var service = new RsaEncryptionService(publicKey, privateKey);
            var original = "1726588302"; // dato de ejemplo: número de identificación

            var encrypted = service.Encrypt(original);
            var decrypted = service.Decrypt(encrypted);

            Assert.NotEqual(original, encrypted);
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void Encrypt_EmptyString_ReturnsEmptyStringWithoutThrowing()
        {
            var (publicKey, privateKey) = GenerateTestKeyPair();
            var service = new RsaEncryptionService(publicKey, privateKey);

            var result = service.Encrypt(string.Empty);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void EncryptBytes_Then_DecryptBytes_ReturnsOriginalData()
        {
            var (publicKey, privateKey) = GenerateTestKeyPair();
            var service = new RsaEncryptionService(publicKey, privateKey);
            var originalData = Encoding.UTF8.GetBytes("plantilla-biometrica-de-prueba");

            var encrypted = service.EncryptBytes(originalData);
            var decrypted = service.DecryptBytes(encrypted);

            Assert.Equal(originalData, decrypted);
        }

        [Fact]
        public void Constructor_InvalidPublicKeyFormat_ThrowsFormatException()
        {
            var (_, privateKey) = GenerateTestKeyPair();

            Assert.Throws<FormatException>(() =>
                new RsaEncryptionService("clave-publica-invalida", privateKey));
        }
    }
}