// Path: isql/tnk/TNK-67e3eba6c7290257b6752e18259f2ed7a66c7bac/src/TNK.Infrastructure/Data/SeedData.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TNK.Core.BusinessAggregate;
using TNK.Core.Constants; // For Roles
using TNK.Core.ContributorAggregate;
using TNK.Core.Identity;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Enums;
using System; // For DayOfWeek, DateTime, DateOnly, TimeOnly, Guid
using System.Linq; // For Linq extension methods
using System.Threading.Tasks; // For Task

namespace TNK.Infrastructure.Data;

public static class SeedData
{
  // Contributor seed data (from original file)
  public static readonly Contributor Contributor1 = new("Ardalis");
  public static readonly Contributor Contributor2 = new("Snowfrog");

  // Role names (updated to use Roles class)
  // public static readonly string AdminRole = Roles.SUPERADMIN; // Assuming Roles.Admin is SUPERADMIN
  // public static readonly string VendorRole = Roles.VENDOR;
  // public static readonly string CustomerRole = Roles.CUSTOMER;

  public static async Task InitializeAsync(IServiceProvider serviceProvider)
  {
    using (var scope = serviceProvider.CreateScope())
    {
      var scopedProvider = scope.ServiceProvider;

      var roleManager = scopedProvider.GetRequiredService<RoleManager<IdentityRole>>();
      var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();
      var dbContext = scopedProvider.GetRequiredService<AppDbContext>();

      // Ensures database is created. If you are using "dotnet ef database update", this might be redundant.
      // await dbContext.Database.EnsureCreatedAsync(); 
      // For EF Core Migrations, "database update" is the preferred way to create/migrate the DB.

      await SeedRolesAsync(roleManager);
      var (vendorUser, customerUser) = await SeedUsersAsync(userManager); // Fetch or create users

      // Seed Contributors (as in original file)
      await SeedContributorsAsync(dbContext);

      if (vendorUser != null)
      {
        var businessProfile = await SeedBusinessProfileAsync(dbContext, vendorUser);
        if (businessProfile != null && customerUser != null)
        {
          await SeedServiceManagementDataAsync(dbContext, businessProfile, customerUser);
        }
      }
    }
  }

  private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
  {
    // Using Roles constants
    string[] roleNames = { Roles.Admin, Roles.Vendor, Roles.Customer };

    foreach (var roleName in roleNames)
    {
      if (!await roleManager.RoleExistsAsync(roleName))
      {
        await roleManager.CreateAsync(new IdentityRole(roleName));
      }
    }
  }

  private static async Task<(ApplicationUser? VendorUser, ApplicationUser? CustomerUser)> SeedUsersAsync(UserManager<ApplicationUser> userManager)
  {
    ApplicationUser? adminUser = await userManager.FindByNameAsync("superadmin@terminnaklik.com");
    if (adminUser == null)
    {
      adminUser = new ApplicationUser { UserName = "superadmin@terminnaklik.com", Email = "superadmin@terminnaklik.com", EmailConfirmed = true, FirstName = "Super", LastName = "Admin" };
      var adminResult = await userManager.CreateAsync(adminUser, "AdminP@$$wOrd1!"); // Use a strong password
      if (adminResult.Succeeded)
      {
        await userManager.AddToRoleAsync(adminUser, Roles.Admin);
      }
    }

    ApplicationUser? vendorUser = await userManager.FindByNameAsync("vendor@example.com");
    if (vendorUser == null)
    {
      vendorUser = new ApplicationUser { UserName = "vendor@example.com", Email = "vendor@example.com", EmailConfirmed = true, FirstName = "Demo", LastName = "Vendor" };
      var vendorResult = await userManager.CreateAsync(vendorUser, "VendorP@$$wOrd1!"); // Use a strong password
      if (vendorResult.Succeeded)
      {
        await userManager.AddToRoleAsync(vendorUser, Roles.Vendor);
      }
    }

    ApplicationUser? customerUser = await userManager.FindByNameAsync("customer@example.com");
    if (customerUser == null)
    {
      customerUser = new ApplicationUser { UserName = "customer@example.com", Email = "customer@example.com", EmailConfirmed = true, FirstName = "Demo", LastName = "Customer" };
      var customerResult = await userManager.CreateAsync(customerUser, "CustP@$$wOrd1!"); // Use a strong password
      if (customerResult.Succeeded)
      {
        await userManager.AddToRoleAsync(customerUser, Roles.Customer);
      }
    }
    return (vendorUser, customerUser);
  }

  private static async Task SeedContributorsAsync(AppDbContext dbContext)
  {
    if (!await dbContext.Contributors.AnyAsync())
    {
      dbContext.Contributors.AddRange(Contributor1, Contributor2);
      await dbContext.SaveChangesAsync();
    }
  }

  private static async Task<BusinessProfile?> SeedBusinessProfileAsync(AppDbContext dbContext, ApplicationUser vendorUser)
  {
    var businessProfile = await dbContext.BusinessProfiles.FirstOrDefaultAsync(bp => bp.VendorId == vendorUser.Id);
    if (businessProfile == null)
    {
      // Using constructor from BusinessProfile.cs
      businessProfile = new BusinessProfile(
          vendorId: vendorUser.Id,
          name: "Demo Salon & Spa",
          address: "123 Demo Street, Belgrade, Serbia",
          phoneNumber: "011-555-1234",
          description: "Offering the finest demo services for your testing needs."
      );
      dbContext.BusinessProfiles.Add(businessProfile);
      await dbContext.SaveChangesAsync();
    }
    return businessProfile;
  }

  private static async Task SeedServiceManagementDataAsync(
      AppDbContext dbContext,
      BusinessProfile businessProfile,
      ApplicationUser customerUser)
  {
    // 1. Seed Services
    Service? serviceHaircut = await dbContext.Services.FirstOrDefaultAsync(s => s.BusinessProfileId == businessProfile.Id && s.Name == "Demo Haircut");
    if (serviceHaircut == null)
    {
      // Using constructor from Service.cs
      serviceHaircut = new Service(businessProfile.Id, "Demo Haircut", 45, 2500.00m);
      serviceHaircut.SetDescription("A stylish haircut using demo techniques.");
      serviceHaircut.SetImageUrl("https://example.com/demohaircut.jpg");
      dbContext.Services.Add(serviceHaircut);
    }

    Service? serviceMassage = await dbContext.Services.FirstOrDefaultAsync(s => s.BusinessProfileId == businessProfile.Id && s.Name == "Demo Relax Massage");
    if (serviceMassage == null)
    {
      serviceMassage = new Service(businessProfile.Id, "Demo Relax Massage", 60, 3500.00m);
      serviceMassage.SetDescription("A relaxing massage to de-stress your demo day.");
      serviceMassage.Activate(); // Ensure it's active
      dbContext.Services.Add(serviceMassage);
    }
    await dbContext.SaveChangesAsync(); // Save services to get their IDs

    // 2. Seed Workers
    Worker? workerJohn = await dbContext.Workers.FirstOrDefaultAsync(w => w.BusinessProfileId == businessProfile.Id && w.Email == "john.stylist@example.com");
    if (workerJohn == null)
    {
      // Using constructor from Worker.cs
      workerJohn = new Worker(businessProfile.Id, "John", "Stylist");
      workerJohn.UpdateDetails("John", "Stylist", "john.stylist@example.com", "060111222", "https://example.com/john.jpg", "Haircuts, Styling");
      dbContext.Workers.Add(workerJohn);
    }

    Worker? workerJane = await dbContext.Workers.FirstOrDefaultAsync(w => w.BusinessProfileId == businessProfile.Id && w.Email == "jane.masseuse@example.com");
    if (workerJane == null)
    {
      workerJane = new Worker(businessProfile.Id, "Jane", "Masseuse");
      workerJane.UpdateDetails("Jane", "Masseuse", "jane.masseuse@example.com", "060333444", "https://example.com/jane.jpg", "Relaxation Massage");
      dbContext.Workers.Add(workerJane);
    }
    await dbContext.SaveChangesAsync(); // Save workers to get their IDs

    // 3. Seed Schedules
    Schedule? scheduleJohn = await dbContext.Schedules
    .Include(s => s.RuleItems) // Eager load RuleItems
    .ThenInclude(ri => ri.Breaks) // Eager load Breaks
    .FirstOrDefaultAsync(s => s.WorkerId == workerJohn.Id && s.IsDefault);

    if (workerJohn != null && scheduleJohn == null) // Check if scheduleJohn needs to be created
    {
      scheduleJohn = new Schedule(
          workerJohn.Id,
          businessProfile.Id,
          "John's Regular Hours",
          DateOnly.FromDateTime(DateTime.UtcNow.Date),
          "Europe/Belgrade", // Ensure this is a valid TimeZoneId
          true
      );
      dbContext.Schedules.Add(scheduleJohn);
      // Save the Schedule first to get its ID.
      // RuleItems will be added and saved in the next step.
      await dbContext.SaveChangesAsync();

      // Add rule items
      scheduleJohn.AddRuleItem(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), true);
      scheduleJohn.AddRuleItem(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0), true);
      scheduleJohn.AddRuleItem(DayOfWeek.Wednesday, new TimeOnly(9, 0), new TimeOnly(12, 30), true);
      scheduleJohn.AddRuleItem(DayOfWeek.Thursday, new TimeOnly(12, 0), new TimeOnly(20, 0), true);
      scheduleJohn.AddRuleItem(DayOfWeek.Friday, new TimeOnly(9, 0), new TimeOnly(17, 0), true);
      scheduleJohn.AddRuleItem(DayOfWeek.Saturday, TimeOnly.MinValue, TimeOnly.MinValue, false);
      scheduleJohn.AddRuleItem(DayOfWeek.Sunday, TimeOnly.MinValue, TimeOnly.MinValue, false);

      // Now save the Schedule again, this time with its newly added RuleItems.
      // This will generate IDs for the RuleItems.
      await dbContext.SaveChangesAsync();

      // Now that RuleItems are saved and have IDs, retrieve the one you want to add a break to.
      var mondayRule = await dbContext.ScheduleRuleItems
                                   .FirstOrDefaultAsync(r => r.ScheduleId == scheduleJohn.Id &&
                                                             r.DayOfWeek == DayOfWeek.Monday &&
                                                             r.StartTime == new TimeOnly(9, 0));
      if (mondayRule != null)
      {
        if (!mondayRule.Breaks.Any(b => b.Name == "Lunch Break")) // Check if break already exists
        {
          mondayRule.AddBreak("Lunch Break", new TimeOnly(13, 0), new TimeOnly(14, 0));
          // Save again to persist the BreakRule.
          await dbContext.SaveChangesAsync();
        }
      }
    }


    // 4. Seed Availability Slots (Manually for Demo)
    // For robust slot generation, you'd have a separate service.
    // These are examples. Adjust StartTime based on current date for testing.
    AvailabilitySlot? slotForBooking = null;
    if (workerJohn != null && serviceHaircut != null)
    {
      // Find a future working day for John (e.g., next Monday if today is weekend)
      var nextMonday = DateTime.UtcNow.Date;
      while (nextMonday.DayOfWeek != DayOfWeek.Monday) nextMonday = nextMonday.AddDays(1);
      if (nextMonday < DateTime.UtcNow.Date.AddDays(1)) nextMonday = nextMonday.AddDays(7); // Ensure it's in the future

      var slotTime = new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 10, 0, 0, DateTimeKind.Local); // Assuming Belgrade time for demo
      DateTimeOffset slotStartTimeLocal = new DateTimeOffset(slotTime); // Example: Tomorrow 10:00 AM Local Time
      DateTimeOffset slotEndTimeLocal = slotStartTimeLocal.AddMinutes(serviceHaircut.DurationInMinutes);

      // Convert to UTC for storage if your convention is UTC
      DateTime slotStartTimeUtc = slotStartTimeLocal.UtcDateTime;
      DateTime slotEndTimeUtc = slotEndTimeLocal.UtcDateTime;


      slotForBooking = await dbContext.AvailabilitySlots.FirstOrDefaultAsync(s => s.WorkerId == workerJohn.Id && s.StartTime == slotStartTimeUtc);
      if (slotForBooking == null)
      {
        var scheduleJohnDb = await dbContext.Schedules.FirstOrDefaultAsync(s => s.WorkerId == workerJohn.Id && s.IsDefault);
        // Using constructor from AvailabilitySlot.cs
        slotForBooking = new AvailabilitySlot(
            workerJohn.Id,
            businessProfile.Id,
            slotStartTimeUtc, // Store as UTC
            slotEndTimeUtc,   // Store as UTC
            AvailabilitySlotStatus.Available,
            scheduleJohnDb?.Id // Link to schedule if available
        );
        dbContext.AvailabilitySlots.Add(slotForBooking);
        await dbContext.SaveChangesAsync(); // Save slot to get its ID
      }
    }

    // 5. Seed Bookings
    if (customerUser != null && serviceHaircut != null && workerJohn != null && slotForBooking != null && slotForBooking.Status == AvailabilitySlotStatus.Available)
    {
      var existingBooking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.AvailabilitySlotId == slotForBooking.Id);
      if (existingBooking == null)
      {
        // Using constructor from Booking.cs
        var booking = new Booking(
            businessProfile.Id,
            customerUser.Id,
            serviceHaircut.Id,
            workerJohn.Id,
            slotForBooking.Id,
            slotForBooking.StartTime, // Use slot's time
            slotForBooking.EndTime,   // Use slot's time
            serviceHaircut.Price      // Price at time of booking
        );
        booking.UpdateNotes("First demo booking!", null);
        booking.ConfirmBooking(); // Set booking status, etc.

        dbContext.Bookings.Add(booking);
        // IMPORTANT: Save the booking entity here to generate its ID
        await dbContext.SaveChangesAsync();

        // Now booking.Id is populated and can be safely used.
        if (slotForBooking.Status == AvailabilitySlotStatus.Available) // Double check status before booking
        {
          slotForBooking.BookSlot(booking.Id);
          // EF Core will track the change to slotForBooking.
          // A final SaveChangesAsync at the end of the InitializeAsync scope, 
          // or an explicit one here, will persist this change.
          // For clarity in seeding and to ensure this linked change is saved:
          await dbContext.SaveChangesAsync();
        }
      }
    }
  }
}
