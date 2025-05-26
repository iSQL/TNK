using Microsoft.AspNetCore.Identity;

using TNK.Core.ContributorAggregate;


namespace TNK.Infrastructure.Data;

public static class SeedData
{
  // Contributor Data
  public static readonly Contributor Contributor1 = new("Ardalis");
  public static readonly Contributor Contributor2 = new("Snowfrog");

  // Role names - these can be used elsewhere in your application if needed
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

      // 1. Seed Roles
      var roleManager = scopedProvider.GetRequiredService<RoleManager<IdentityRole>>();
      await SeedRolesAsync(roleManager);

      // 2. Seed Contributors (and other application-specific data)
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
      // Check if the role already exists
      if (!await roleManager.RoleExistsAsync(roleName))
      {
        // Create the role
        await roleManager.CreateAsync(new IdentityRole(roleName));
      }
    }
  }

  private static async Task SeedContributorsAsync(AppDbContext dbContext)
  {
    // Check if contributors already exist to prevent duplicating data
    if (!await dbContext.Contributors.AnyAsync())
    {
      // Using C# 12 collection expression as in your original code
      dbContext.Contributors.AddRange([Contributor1, Contributor2]);
      await dbContext.SaveChangesAsync();
    }
  }
}
