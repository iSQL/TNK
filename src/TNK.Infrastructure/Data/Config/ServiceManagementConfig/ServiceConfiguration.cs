using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Infrastructure.Data.Config; // For DataSchemaConstants

namespace TNK.Infrastructure.Data.Config.ServiceManagementConfig;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
  public void Configure(EntityTypeBuilder<Service> builder)
  {
    builder.ToTable("Services", DataSchemaConstants.DefaultSchema); // Optional: Define schema if not default

    builder.HasKey(s => s.Id);

    builder.Property(s => s.Name)
        .HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH)
        .IsRequired();

    builder.Property(s => s.Description)
        .HasMaxLength(DataSchemaConstants.DEFAULT_DESCRIPTION_LENGTH);

    builder.Property(s => s.DurationInMinutes)
        .IsRequired();

    builder.Property(s => s.Price)
        .HasColumnType("decimal(18,2)") // Specify precision and scale for currency
        .IsRequired();

    builder.Property(s => s.IsActive)
        .IsRequired();

    builder.Property(s => s.ImageUrl)
        .HasMaxLength(DataSchemaConstants.DEFAULT_URL_LENGTH);

    // Foreign Key to BusinessProfile
    builder.HasOne(s => s.BusinessProfile)
        .WithMany() // Assuming BusinessProfile doesn't have a direct collection of Services
        .HasForeignKey(s => s.BusinessProfileId)
        .OnDelete(DeleteBehavior.Restrict); // Or Cascade, depending on your rules

    builder.HasIndex(s => s.BusinessProfileId);
  }
}
