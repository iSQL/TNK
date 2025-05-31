using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Infrastructure.Data.Config;

namespace TNK.Infrastructure.Data.Config.ServiceManagementConfig;

public class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
  public void Configure(EntityTypeBuilder<Worker> builder)
  {
    builder.ToTable("Workers", DataSchemaConstants.DefaultSchema);

    builder.HasKey(w => w.Id);

    builder.Property(w => w.FirstName)
        .HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH)
        .IsRequired();

    builder.Property(w => w.LastName)
        .HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH)
        .IsRequired();

    builder.Ignore(w => w.FullName); // Calculated property

    builder.Property(w => w.Email)
        .HasMaxLength(DataSchemaConstants.DEFAULT_EMAIL_LENGTH);

    builder.Property(w => w.PhoneNumber)
        .HasMaxLength(DataSchemaConstants.DEFAULT_PHONE_LENGTH);

    builder.Property(w => w.Specialization)
        .HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH);

    builder.Property(w => w.IsActive)
        .IsRequired();

    builder.Property(w => w.ImageUrl)
        .HasMaxLength(DataSchemaConstants.DEFAULT_URL_LENGTH);

    // Foreign Key to BusinessProfile
    builder.HasOne(w => w.BusinessProfile)
        .WithMany() // Assuming BusinessProfile doesn't have a direct collection of Workers
        .HasForeignKey(w => w.BusinessProfileId)
        .OnDelete(DeleteBehavior.Restrict); // Or Cascade

    // Optional Foreign Key to ApplicationUser
    builder.HasOne(w => w.ApplicationUser)
        .WithMany() // Assuming ApplicationUser doesn't have a direct collection of Worker profiles
        .HasForeignKey(w => w.ApplicationUserId)
        .IsRequired(false) // Worker might not be a system user
        .OnDelete(DeleteBehavior.SetNull); // If user is deleted, set Worker.ApplicationUserId to null

    builder.HasIndex(w => w.BusinessProfileId);
    builder.HasIndex(w => w.ApplicationUserId).IsUnique(false); // Can be null, so not strictly unique unless filtered for non-null
  }
}
