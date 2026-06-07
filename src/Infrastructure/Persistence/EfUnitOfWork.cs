using Microsoft.EntityFrameworkCore.Storage;
using SecureHub.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Persistence
{
	public class EfUnitOfWork : IUnitOfWork
	{
		private readonly SecureHubDbContext _context;
		private IDbContextTransaction? _transaction;

		public EfUnitOfWork(SecureHubDbContext context)
		{
			_context = context;
		}

		public async Task BeginTransactionAsync()
		{
			_transaction = await _context.Database.BeginTransactionAsync();
		}

		public async Task CommitAsync()
		{
			await _context.SaveChangesAsync();
			await _transaction!.CommitAsync();
		}

		public async Task RollbackAsync()
		{
			if (_transaction != null)
				await _transaction.RollbackAsync();
		}

		public Task SaveChangesAsync()
			=> _context.SaveChangesAsync();

		public async Task SaveAsync(CancellationToken ct = default)
	=> await _context.SaveChangesAsync(ct);
	}
}
