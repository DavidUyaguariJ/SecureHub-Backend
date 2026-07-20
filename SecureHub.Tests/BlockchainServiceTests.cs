using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using SecureHub.Infrastructure.Blockchain;
using Xunit;

namespace SecureHub.Tests
{
    // CP-03 (RF-04 Registro Inmutable de Actividades / Blockchain)
    public class BlockchainServiceTests
    {
        private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        [Fact]
        public void Constructor_MissingRpcUrl_ThrowsInvalidOperationException()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "DEPLOYER_PRIVATE_KEY", "0xabc123" },
                { "CONTRACT_ADDRESS", "0xdef456" },
                { "CHAIN_ID", "31337" }
            });

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new BlockchainService(config));

            Assert.Equal("Blockchain:RpcUrl no configurado", ex.Message);
        }

        [Fact]
        public void Constructor_MissingDeployerPrivateKey_ThrowsInvalidOperationException()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "RPC_URL", "http://localhost:8545" },
                { "CONTRACT_ADDRESS", "0xdef456" },
                { "CHAIN_ID", "31337" }
            });

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new BlockchainService(config));

            Assert.Equal("Blockchain:DeployerPrivateKey no configurado", ex.Message);
        }

        [Fact]
        public void Constructor_MissingContractAddress_ThrowsInvalidOperationException()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "RPC_URL", "http://localhost:8545" },
                { "DEPLOYER_PRIVATE_KEY", "0xabc123" },
                { "CHAIN_ID", "31337" }
            });

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new BlockchainService(config));

            Assert.Equal("Blockchain:ContractAddress no configurado", ex.Message);
        }
    }
}