using Microsoft.Extensions.Configuration;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using SecureHub.Application.Interfaces;
using System.Numerics;

namespace SecureHub.Infrastructure.Blockchain
{
	public class BlockchainService : IBlockchainService
	{
		private readonly Web3 _web3;
		private readonly string _contractAddress;
		private readonly string _abi;
		private static readonly HexBigInteger DefaultGas = new HexBigInteger(1_000_000);
		private static readonly SemaphoreSlim _txLock = new(1, 1);

		public BlockchainService(IConfiguration config)
		{
			var rpcUrl = config["RPC_URL"]
				?? throw new InvalidOperationException("Blockchain:RpcUrl no configurado");

			var privateKey = config["DEPLOYER_PRIVATE_KEY"]
				?? throw new InvalidOperationException("Blockchain:DeployerPrivateKey no configurado");
			_contractAddress = config["CONTRACT_ADDRESS"]
				?? throw new InvalidOperationException("Blockchain:ContractAddress no configurado");
			var chainId = long.Parse(config["CHAIN_ID"] ?? "31337");
			var account = new Account(privateKey, chainId);

			_web3 = new Web3(account, rpcUrl);
			_web3.TransactionManager.UseLegacyAsDefault = true;

			_abi = File.ReadAllText(
				Path.Combine(AppContext.BaseDirectory, "Blockchain", "abi.json"));
		}

		private Nethereum.Contracts.Contract GetContract()
		{
			Console.WriteLine($"Obteniendo contrato {_contractAddress}");
			return _web3.Eth.GetContract(_abi, _contractAddress);
		}

		private static byte[] GuidToBytes32(Guid guid)
		{
			var bytes = new byte[32];
			var guidBytes = guid.ToByteArray();
			Array.Copy(guidBytes, 0, bytes, 0, 16);
			return bytes;
		}

		public async Task<string> RecordAuditAsync(
			Guid entityId, string entityType, string action,
			string previousState, string newState,
			string operatorRef, string ipHash, CancellationToken ct = default)
		{
			await _txLock.WaitAsync(ct);
			try
			{
				var fn = GetContract().GetFunction("recordAudit");
				return await fn.SendTransactionAsync(
					_web3.TransactionManager.Account.Address,
					DefaultGas,
					null,
					GuidToBytes32(entityId),
					entityType, action, previousState, newState,
					operatorRef, ipHash);
			}
			finally { _txLock.Release(); }
		}

		public async Task<string> RecordArcoRequestAsync(
			Guid arcoRequestId, Guid subjectId, string requestType,
			DateTimeOffset requestedAt, CancellationToken ct = default)
		{
			await _txLock.WaitAsync(ct);
			try
			{
				var fn = GetContract().GetFunction("recordArcoRequest");
				return await fn.SendTransactionAsync(
					_web3.TransactionManager.Account.Address,
					DefaultGas,
					null,
					GuidToBytes32(arcoRequestId),
					GuidToBytes32(subjectId),
					requestType,
					new BigInteger(requestedAt.ToUnixTimeSeconds()));
			}
			finally { _txLock.Release(); }
		}

		public async Task<string> UpdateArcoStatusAsync(
			Guid arcoRequestId, string newStatus, string resolutionHash,
			CancellationToken ct = default)
		{
			await _txLock.WaitAsync(ct);
			try
			{
				var fn = GetContract().GetFunction("updateArcoStatus");
				return await fn.SendTransactionAsync(
					_web3.TransactionManager.Account.Address,
					DefaultGas,
					null,
					GuidToBytes32(arcoRequestId),
					newStatus, resolutionHash);
			}
			finally { _txLock.Release(); }
		}

		public async Task<string> RegisterThirdPartyAsync(
			Guid thirdPartyId, string name, string contractHash,
			string dataCategories, string purpose,
			DateTimeOffset validUntil, CancellationToken ct = default)
		{
			await _txLock.WaitAsync(ct);
			try
			{
				var fn = GetContract().GetFunction("registerThirdParty");
				return await fn.SendTransactionAsync(
					_web3.TransactionManager.Account.Address,
					DefaultGas,
					null,
					GuidToBytes32(thirdPartyId),
					name, contractHash, dataCategories, purpose,
					new BigInteger(validUntil.ToUnixTimeSeconds()));
			}
			finally { _txLock.Release(); }
		}

		public async Task<string> UpdateThirdPartyStatusAsync(
			Guid thirdPartyId, int status, CancellationToken ct = default)
		{
			await _txLock.WaitAsync(ct);
			try
			{
				var fn = GetContract().GetFunction("updateThirdPartyStatus");
				return await fn.SendTransactionAsync(
					_web3.TransactionManager.Account.Address,
					DefaultGas,
					null,
					GuidToBytes32(thirdPartyId),
					(byte)status);
			}
			finally { _txLock.Release(); }
		}

		public async Task<(bool Active, DateTimeOffset ValidUntil, int Status)>
			IsThirdPartyActiveAsync(Guid thirdPartyId, CancellationToken ct = default)
		{
			var fn = GetContract().GetFunction("isThirdPartyActive");
			var result = await fn.CallDecodingToDefaultAsync(
				GuidToBytes32(thirdPartyId));

			var active = (bool)result[0].Result;
			var unixTs = (BigInteger)result[1].Result;
			var statusVal = (BigInteger)result[2].Result;
			return (
				active,
				DateTimeOffset.FromUnixTimeSeconds((long)unixTs),
				(int)statusVal
			);
		}
	}
}