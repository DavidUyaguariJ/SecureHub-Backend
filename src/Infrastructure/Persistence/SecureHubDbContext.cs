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
				e.ToTable("Subjects");
				e.HasKey(s => s.Id);
				e.Property(s => s.Id).HasColumnName("Id").HasDefaultValueSql("gen_random_uuid()");
				e.Property(s => s.Identification).HasColumnName("Identification").HasMaxLength(20).IsRequired();
				e.Property(s => s.FullName).HasColumnName("FullName").HasMaxLength(255).IsRequired();
				e.Property(s => s.Phone).HasColumnName("Phone").HasMaxLength(20);
				e.Property(s => s.Address).HasColumnName("Address");
				e.Property(s => s.Email).HasColumnName("Email").HasMaxLength(150).IsRequired();
				e.Property(s => s.SubjectType).HasColumnName("SubjectType").HasMaxLength(20);
				e.Property(s => s.ContactPerson).HasColumnName("ContactPerson").HasMaxLength(255);
				e.Property(s => s.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
				e.HasIndex(s => s.Identification).IsUnique();
				e.HasIndex(s => s.Email).IsUnique();
				e.HasMany(s => s.Devices).WithOne(d => d.Subject).HasForeignKey(d => d.SubjectId).OnDelete(DeleteBehavior.Cascade);
				e.HasMany(s => s.BiometricAuths).WithOne(b => b.Subject).HasForeignKey(b => b.SubjectId);
			});

			modelBuilder.Entity<Device>(e =>
			{
				e.ToTable("Devices");
				e.HasKey(d => d.Id);
				e.Property(d => d.Id).HasColumnName("Id").HasDefaultValueSql("gen_random_uuid()");
				e.Property(d => d.SubjectId).HasColumnName("SubjectId").IsRequired();
				e.Property(d => d.DeviceType).HasColumnName("DeviceType").HasMaxLength(50).IsRequired();
				e.Property(d => d.Brand).HasColumnName("Brand").HasMaxLength(50);
				e.Property(d => d.Model).HasColumnName("Model").HasMaxLength(50);
				e.Property(d => d.SerialNumber).HasColumnName("SerialNumber").HasMaxLength(100);
				e.Property(d => d.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
				e.HasIndex(d => d.SerialNumber).IsUnique();
				e.HasOne(d => d.Credential).WithOne(c => c.Device).HasForeignKey<DeviceCredential>(c => c.DeviceId).OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<DeviceCredential>(e =>
			{
				e.ToTable("DeviceCredentials");
				e.HasKey(c => c.Id);
				e.Property(c => c.Id).HasColumnName("Id").HasDefaultValueSql("gen_random_uuid()");
				e.Property(c => c.DeviceId).HasColumnName("DeviceId").IsRequired();
				e.Property(c => c.SystemUser).HasColumnName("SystemUser").HasMaxLength(100);
				e.Property(c => c.EncryptedPassword).HasColumnName("EncryptedPassword").IsRequired();
				e.Property(c => c.EncryptionIV).HasColumnName("EncryptionIV").IsRequired();
				e.Property(c => c.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
			});

			modelBuilder.Entity<BiometricAuth>(e =>
			{
				e.ToTable("BiometricAuth");
				e.HasKey(b => b.Id);
				e.Property(b => b.Id).HasColumnName("Id").HasDefaultValueSql("gen_random_uuid()");
				e.Property(b => b.SubjectId).HasColumnName("SubjectId").IsRequired();
				e.Property(b => b.TemplateType).HasColumnName("TemplateType").HasMaxLength(20);
				e.Property(b => b.BiometricVector).HasColumnName("BiometricVector").IsRequired();
				e.Property(b => b.ConsentText).HasColumnName("ConsentText").IsRequired();
				e.Property(b => b.DigitalSignature).HasColumnName("DigitalSignature");
				e.Property(b => b.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
			});
		}
	}
}