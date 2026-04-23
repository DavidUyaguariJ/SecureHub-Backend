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
				e.Property(s => s.Identification).HasColumnName("identification").HasColumnType("TEXT").IsRequired();
				e.Property(s => s.FullName).HasColumnName("full_name").HasColumnType("TEXT").IsRequired();
				e.Property(s => s.Phone).HasColumnName("phone").HasColumnType("TEXT");
				e.Property(s => s.Address).HasColumnName("address").HasColumnType("TEXT");
				e.Property(s => s.Email).HasColumnName("email").HasColumnType("TEXT").IsRequired();
				e.Property(s => s.SubjectType).HasColumnName("subject_type").HasMaxLength(20);
				e.Property(s => s.ContactPerson).HasColumnName("contact_person").HasColumnType("TEXT");
				e.Property(s => s.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
				e.Property(s => s.DeletedAt).HasColumnName("deleted_at");
				e.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
				e.HasQueryFilter(s => !s.IsDeleted);

				e.HasMany(s => s.Devices).WithOne(d => d.Subject).HasForeignKey(d => d.SubjectId).OnDelete(DeleteBehavior.Cascade);
				e.HasMany(s => s.BiometricAuths).WithOne(b => b.Subject).HasForeignKey(b => b.SubjectId);
			});

			modelBuilder.Entity<Device>(e =>
			{
				e.ToTable("devices");
				e.HasKey(d => d.Id);
				e.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
				e.Property(d => d.SubjectId).HasColumnName("subject_id").IsRequired();
				e.Property(d => d.DeviceType).HasColumnName("device_type").HasMaxLength(50).IsRequired();
				e.Property(d => d.Brand).HasColumnName("brand").HasColumnType("TEXT");
				e.Property(d => d.Model).HasColumnName("model").HasColumnType("TEXT");
				e.Property(d => d.SerialNumber).HasColumnName("serial_number").HasColumnType("TEXT");
				e.Property(d => d.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
				e.Property(d => d.DeletedAt).HasColumnName("deleted_at");
				e.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
				e.HasQueryFilter(d => !d.IsDeleted);
				e.HasOne(d => d.Credential).WithOne(c => c.Device).HasForeignKey<DeviceCredential>(c => c.DeviceId).OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<DeviceCredential>(e =>
			{
				e.ToTable("device_credentials");
				e.HasKey(c => c.Id);
				e.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
				e.Property(c => c.DeviceId).HasColumnName("device_id").IsRequired();
				e.Property(c => c.SystemUser).HasColumnName("system_user_name").HasColumnType("TEXT");
				e.Property(c => c.EncryptedPassword).HasColumnName("encrypted_password").HasColumnType("TEXT").IsRequired();
				e.Property(c => c.EncryptionIV).HasColumnName("encryption_iv").HasColumnType("TEXT").IsRequired(false);
				e.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
			});

			modelBuilder.Entity<BiometricAuth>(e =>
			{
				e.ToTable("biometric_auth");
				e.HasKey(b => b.Id);
				e.Property(b => b.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
				e.Property(b => b.SubjectId).HasColumnName("subject_id").IsRequired();
				e.Property(b => b.TemplateType).HasColumnName("template_type").HasMaxLength(50);
				e.Property(b => b.BiometricVector).HasColumnName("biometric_vector").IsRequired();
				e.Property(b => b.ConsentText).HasColumnName("consent_text").HasColumnType("TEXT").IsRequired();
				e.Property(b => b.DigitalSignature).HasColumnName("digital_signature").HasColumnType("TEXT");
				e.Property(b => b.EmbeddingModel).HasColumnName("embedding_model").HasMaxLength(50);
				e.Property(b => b.EmbeddingDims).HasColumnName("embedding_dims");
				e.Property(b => b.ConfidenceScore).HasColumnName("confidence_score");
				e.Property(b => b.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
				e.Property(b => b.DeletedAt).HasColumnName("deleted_at");
				e.Property(b => b.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
				e.HasQueryFilter(b => !b.IsDeleted);
			});

		}
	}
}