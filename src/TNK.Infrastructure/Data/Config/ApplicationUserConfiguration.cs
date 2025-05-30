using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TNK.Core.Identity; 

namespace TNK.Infrastructure.Data.Config;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
  public void Configure(EntityTypeBuilder<ApplicationUser> builder)
  {
    // Identity tables are named by convention (e.g., AspNetUsers)
    // builder.ToTable("YourCustomUserTableName"); // If you want to override

    builder.Property(u => u.FirstName)
        .HasMaxLength(100);

    builder.Property(u => u.LastName)
        .HasMaxLength(100);

    // The relationship with BusinessProfile is already defined from BusinessProfileConfiguration.
    // If you wanted to define it here, it would look like:
    // builder.HasOne(u => u.BusinessProfile)
    //        .WithOne(bp => bp.Vendor)
    //        .HasForeignKey<BusinessProfile>(bp => bp.VendorId);
    // Note: Define the relationship only once to avoid conflicts.
  }
}
