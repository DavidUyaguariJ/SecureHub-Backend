using Microsoft.EntityFrameworkCore;

namespace SecureHub.Infrastructure.Persistence
{
	public class SecureHubDbContext: DbContext
	{
		public SecureHubDbContext(DbContextOptions<SecureHubDbContext> options): base(options)
		{

		}
	}
}
