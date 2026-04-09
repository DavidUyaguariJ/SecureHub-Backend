using Microsoft.EntityFrameworkCore;

namespace SecureHub_Backend.context
{
	public class SecureHubDbContext: DbContext
	{
		public SecureHubDbContext(DbContextOptions<SecureHubDbContext> options): base(options)
		{

		}
	}
}
