using Microsoft.EntityFrameworkCore;
using SecureHub.Domain.Entities;

namespace SecureHub.Infrastructure.Persistence
{
	public class SecureHubDbContext : DbContext
	{
		public SecureHubDbContext(DbContextOptions<SecureHubDbContext> options) : base(options) { }

		public DbSet<Subject> Subjects => Set<Subject>();
		public DbSet<Device> Devices => Set<Device>();
		public DbSet<DeviceCredential> DeviceCredentials => Set<DeviceCredential>();
		public DbSet<BiometricAuth> BiometricAuths => Set<BiometricAuth>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.HasDefaultSchema("data_protection");
			modelBuilder.Entity<Subject>(e =>
			{
				e.ToTable("subjects");
				e.HasKey(s => s.Id);
				e.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
				e.Property(s => s.Identification).HasColumnName("identification").HasMaxLength(20).IsRequired();
				e.Property(s => s.FullName).HasColumnName("fullname").HasMaxLength(255).IsRequired();
				e.Property(s => s.Phone).HasColumnName("phone").HasMaxLength(20);
				e.Property(s => s.Address).HasColumnName("address");
				e.Property(s => s.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
				e.Property(s => s.SubjectType).HasColumnName("subjecttype").HasMaxLength(20);
				e.Property(s => s.ContactPerson).HasColumnName("contactperson").HasMaxLength(255);
				e.Property(s => s.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("CURRENT_TIMESTAMP");
				e.HasIndex(s => s.Identification).IsUnique();
				e.HasIndex(s => s.Email).IsUnique();
				e.HasMany(s => s.Devices).WithOne(d => d.Subject).HasForeignKey(d => d.SubjectId).OnDelete(DeleteBehavior.Cascade);
				e.HasMany(s => s.BiometricAuths).WithOne(b => b.Subject).HasForeignKey(b => b.SubjectId);
			});

			modelBuilder.Entity<Device>(e =>
			{
				e.ToTable("devices");
				e.HasKey(d => d.Id);
				e.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
				e.Property(d => d.SubjectId).HasColumnName("subjectid").IsRequired();
				e.Property(d => d.DeviceType).HasColumnName("devicetype").HasMaxLength(50).IsRequired();
				e.Property(d => d.Brand).HasColumnName("brand").HasMaxLength(50);
				e.Property(d => d.Model).HasColumnName("model").HasMaxLength(50);
				e.Property(d => d.SerialNumber).HasColumnName("serialnumber").HasMaxLength(100);
				e.Property(d => d.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("CURRENT_TIMESTAMP");
				e.HasIndex(d => d.SerialNumber).IsUnique();
				e.HasOne(d => d.Credential).WithOne(c => c.Device).HasForeignKey<DeviceCredential>(c => c.DeviceId).OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<DeviceCredential>(e =>
			{
				e.ToTable("devicecredentials");
				e.HasKey(c => c.Id);
				e.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
				e.Property(c => c.DeviceId).HasColumnName("deviceid").IsRequired();
				e.Property(c => c.SystemUser).HasColumnName("systemuser").HasMaxLength(100);
				e.Property(c => c.EncryptedPassword).HasColumnName("encryptedpassword").IsRequired();
				e.Property(c => c.EncryptionIV).HasColumnName("encryptioniv").IsRequired();
				e.Property(c => c.UpdatedAt).HasColumnName("updatedat").HasDefaultValueSql("CURRENT_TIMESTAMP");
			});

			modelBuilder.Entity<BiometricAuth>(e =>
			{
				e.ToTable("biometricauth");
				e.HasKey(b => b.Id);
				e.Property(b => b.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
				e.Property(b => b.SubjectId).HasColumnName("subjectid").IsRequired();
				e.Property(b => b.TemplateType).HasColumnName("templatetype").HasMaxLength(20);
				e.Property(b => b.BiometricVector).HasColumnName("biometricvector").IsRequired();
				e.Property(b => b.ConsentText).HasColumnName("consenttext").IsRequired();
				e.Property(b => b.DigitalSignature).HasColumnName("digitalsignature");
				e.Property(b => b.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("CURRENT_TIMESTAMP");
			});
		}
	}
}