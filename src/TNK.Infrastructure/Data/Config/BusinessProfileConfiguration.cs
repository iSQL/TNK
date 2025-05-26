using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TNK.Core.BusinessAggregate; 
using TNK.Core.Identity; 

namespace TNK.Infrastructure.Data.Config;

public class BusinessProfileConfiguration : IEntityTypeConfiguration<BusinessProfile>
{
  public void Configure(EntityTypeBuilder<BusinessProfile> builder)
  {


    builder.ToTable("BusinessProfiles");

    builder.Property(bp => bp.Name)
        .IsRequired()
        .HasMaxLength(200);

    builder.Property(bp => bp.Address)
        .HasMaxLength(500);

    builder.Property(bp => bp.PhoneNumber)
        .HasMaxLength(30); 

    builder.Property(bp => bp.Description)
        .HasMaxLength(2000);

    builder.Property(bp => bp.VendorId)
        .IsRequired();

    // Configure the one-to-one relationship between BusinessProfile and ApplicationUser (Vendor)
    // A BusinessProfile MUST have one Vendor (ApplicationUser).
    // An ApplicationUser (Vendor) CAN have one BusinessProfile.
    builder.HasOne(bp => bp.Vendor)                   // BusinessProfile has one Vendor
           .WithOne(u => u.BusinessProfile)           // ApplicationUser has one BusinessProfile
           .HasForeignKey<BusinessProfile>(bp => bp.VendorId) // FK is in BusinessProfile table
           .IsRequired()                              // BusinessProfile must have a VendorId
           .OnDelete(DeleteBehavior.Cascade);          // Or DeleteBehavior.Restrict depending on your rules

    // Consider adding an index for VendorId, especially if you query by it.
    // A unique index ensures one-to-one mapping at the database level.
    builder.HasIndex(bp => bp.VendorId)
           .IsUnique();
  }
}
