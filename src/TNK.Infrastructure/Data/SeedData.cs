using Microsoft.AspNetCore.Identity;

using TNK.Core.ContributorAggregate;


namespace TNK.Infrastructure.Data;

public static class SeedData
{
  public static readonly Contributor Contributor1 = new("Ardalis");
  public static readonly Contributor Contributor2 = new("Snowfrog");

  public static readonly string AdminRole = "Admin";
  public static readonly string VendorRole = "Vendor";
  public static readonly string CustomerRole = "Customer";

  /// <summary>
  /// Initializes the database with seed data.
  /// This method should be called from Program.cs during application startup.
  /// </summary>
  /// <param name="serviceProvider">The service provider to resolve services like DbContext and RoleManager.</param>
  public static async Task InitializeAsync(IServiceProvider serviceProvider)
  {
    using (var scope = serviceProvider.CreateScope())
    {
      var scopedProvider = scope.ServiceProvider;

      var roleManager = scopedProvider.GetRequiredService<RoleManager<IdentityRole>>();
      await SeedRolesAsync(roleManager);

      var dbContext = scopedProvider.GetRequiredService<AppDbContext>();
      await SeedContributorsAsync(dbContext);

      // Add calls to other seeding methods here if necessary
      // e.g., await SeedProductsAsync(dbContext);
    }
  }

  private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
  {
    string[] roleNames = { AdminRole, VendorRole, CustomerRole };

    foreach (var roleName in roleNames)
    {
      if (!await roleManager.RoleExistsAsync(roleName))
      {
        await roleManager.CreateAsync(new IdentityRole(roleName));
      }
    }
  }

  private static async Task SeedContributorsAsync(AppDbContext dbContext)
  {
    if (!await dbContext.Contributors.AnyAsync())
    {
      dbContext.Contributors.AddRange([Contributor1, Contributor2]);
      await dbContext.SaveChangesAsync();
    }
  }
}
