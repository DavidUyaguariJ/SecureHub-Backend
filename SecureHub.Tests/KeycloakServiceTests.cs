using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using SecureHub.Infrastructure.Security;
using Xunit;

namespace SecureHub.Tests
{
    // CP-01 (RNF-01 Seguridad): el servicio no debe iniciar si falta configuración crítica
    public class KeycloakServiceTests
    {
        private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        [Fact]
        public void Constructor_MissingAdminUrl_ThrowsInvalidOperationException()
        {
            // Arrange: config sin "Keycloak:AdminUrl" a propósito
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Keycloak:Realm", "newbie" },
                { "Keycloak:AdminClientId", "admin-cli" },
                { "Keycloak:AdminSecret", "secret" },
                { "Keycloak:ApplicantGroupId", "grp-1" },
                { "Keycloak:ExternalGroupId", "grp-2" }
            });
            var httpClientFactory = new Mock<IHttpClientFactory>().Object;

            // Act + Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new KeycloakService(config, httpClientFactory));

            Assert.Equal("Keycloak:AdminUrl no configurado", ex.Message);
        }

        [Fact]
        public void Constructor_MissingAdminSecret_ThrowsInvalidOperationException()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Keycloak:AdminUrl", "https://keycloak.local/admin" },
                { "Keycloak:Realm", "newbie" },
                { "Keycloak:AdminClientId", "admin-cli" },
                { "Keycloak:ApplicantGroupId", "grp-1" },
                { "Keycloak:ExternalGroupId", "grp-2" }
            });
            var httpClientFactory = new Mock<IHttpClientFactory>().Object;

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new KeycloakService(config, httpClientFactory));

            Assert.Equal("Keycloak:AdminSecret no configurado", ex.Message);
        }

        [Fact]
        public void Constructor_AllConfigValuesPresent_DoesNotThrow()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Keycloak:AdminUrl", "https://keycloak.local/admin" },
                { "Keycloak:Realm", "newbie" },
                { "Keycloak:AdminClientId", "admin-cli" },
                { "Keycloak:AdminSecret", "secret" },
                { "Keycloak:ApplicantGroupId", "grp-1" },
                { "Keycloak:ExternalGroupId", "grp-2" }
            });
            var httpClientFactory = new Mock<IHttpClientFactory>().Object;

            var exception = Record.Exception(() => new KeycloakService(config, httpClientFactory));

            Assert.Null(exception);
        }
    }
}