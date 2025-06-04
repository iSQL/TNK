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
using System;
using System.Linq;
using System.Threading.Tasks;
// using TNK.Infrastructure.Data; // AppDbContext should be in this namespace or the correct one specified.

namespace TNK.Infrastructure.Data; // Assuming AppDbContext is in this namespace. If not, adjust the using statement above or here.

public static class SeedData
{
  public static readonly Contributor Contributor1 = new("Ardalis");
  public static readonly Contributor Contributor2 = new("Snowfrog");

  public static async Task InitializeAsync(IServiceProvider serviceProvider)
  {
    using (var scope = serviceProvider.CreateScope())
    {
      var scopedProvider = scope.ServiceProvider;

      var roleManager = scopedProvider.GetRequiredService<RoleManager<IdentityRole>>();
      var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();
      var dbContext = scopedProvider.GetRequiredService<AppDbContext>();

      Console.WriteLine("Starting database seeding process...");

      await SeedRolesAsync(roleManager);
      var (vendorUser, customerUser) = await SeedUsersAsync(userManager);

      await SeedContributorsAsync(dbContext);

      if (vendorUser != null)
      {
        Console.WriteLine($"Vendor user {vendorUser.UserName} obtained. Proceeding to seed/link BusinessProfile ID 1.");
        // SeedBusinessProfileAsync now only needs dbContext and vendorUser
        var businessProfileForVendor = await SeedBusinessProfileAsync(dbContext, vendorUser);

        if (businessProfileForVendor != null)
        {
          if (businessProfileForVendor.Id == 1)
          {
            Console.WriteLine($"Successfully ensured BusinessProfile ID 1 ('{businessProfileForVendor.Name}') is linked to {vendorUser.UserName} via BusinessProfile.VendorId.");
          }
          else
          {
            Console.WriteLine($"BusinessProfile linked to {vendorUser.UserName} is '{businessProfileForVendor.Name}' with ID {businessProfileForVendor.Id} (not the targeted ID 1). This is expected if ID 1 was taken or if this is a new profile in a non-empty table.");
          }

          // Verify the link by fetching the user again and including the profile
          var freshVendorUser = await userManager.Users.Include(u => u.BusinessProfile).FirstOrDefaultAsync(u => u.Id == vendorUser.Id);
          if (freshVendorUser?.BusinessProfile != null)
          {
            Console.WriteLine($"Verification: User {freshVendorUser.UserName} has BusinessProfile '{freshVendorUser.BusinessProfile.Name}' with ID {freshVendorUser.BusinessProfile.Id} loaded via navigation property.");
          }


          if (customerUser != null)
          {
            Console.WriteLine($"Proceeding to seed service management data for BusinessProfile ID {businessProfileForVendor.Id} ('{businessProfileForVendor.Name}') and Customer {customerUser.UserName}.");
            await SeedServiceManagementDataAsync(dbContext, businessProfileForVendor, customerUser);
          }
          else
          {
            Console.WriteLine("Customer user not found, skipping service management data seeding that requires a customer.");
          }
        }
        else
        {
          Console.WriteLine($"Error: Could not obtain or create BusinessProfile for {vendorUser.UserName}.");
        }
      }
      else
      {
        Console.WriteLine("Vendor user (vendor@example.com) not found. Cannot seed BusinessProfile or related data.");
      }
      Console.WriteLine("Database seeding process finished.");
    }
  }

  private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
  {
    Console.WriteLine("Seeding roles...");
    string[] roleNames = { Roles.Admin, Roles.Vendor, Roles.Customer, Roles.Worker };

    foreach (var roleName in roleNames)
    {
      if (!await roleManager.RoleExistsAsync(roleName))
      {
        await roleManager.CreateAsync(new IdentityRole(roleName));
        Console.WriteLine($"Role '{roleName}' created.");
      }
      else
      {
        Console.WriteLine($"Role '{roleName}' already exists.");
      }
    }
  }

  private static async Task<(ApplicationUser? VendorUser, ApplicationUser? CustomerUser)> SeedUsersAsync(UserManager<ApplicationUser> userManager)
  {
    Console.WriteLine("Seeding users...");
    ApplicationUser? adminUser = await userManager.FindByNameAsync("superadmin@terminnaklik.com");
    if (adminUser == null)
    {
      adminUser = new ApplicationUser { UserName = "superadmin@terminnaklik.com", Email = "superadmin@terminnaklik.com", EmailConfirmed = true, FirstName = "Super", LastName = "Admin" };
      var adminResult = await userManager.CreateAsync(adminUser, "AdminP@$$wOrd1!");
      if (adminResult.Succeeded)
      {
        await userManager.AddToRoleAsync(adminUser, Roles.Admin);
        Console.WriteLine($"User '{adminUser.UserName}' created and assigned to Admin role.");
      }
      else { Console.WriteLine($"Error creating admin user: {string.Join(", ", adminResult.Errors.Select(e => e.Description))}"); }
    }
    else { Console.WriteLine($"User 'superadmin@terminnaklik.com' already exists."); }

    ApplicationUser? vendorUser = await userManager.FindByNameAsync("vendor@example.com");
    if (vendorUser == null)
    {
      vendorUser = new ApplicationUser { UserName = "vendor@example.com", Email = "vendor@example.com", EmailConfirmed = true, FirstName = "Demo", LastName = "Vendor" };
      var vendorResult = await userManager.CreateAsync(vendorUser, "VendorP@$$wOrd1!");
      if (vendorResult.Succeeded)
      {
        await userManager.AddToRoleAsync(vendorUser, Roles.Vendor);
        Console.WriteLine($"User '{vendorUser.UserName}' created and assigned to Vendor role.");
      }
      else { Console.WriteLine($"Error creating vendor user: {string.Join(", ", vendorResult.Errors.Select(e => e.Description))}"); }
    }
    else { Console.WriteLine($"User 'vendor@example.com' already exists."); }


    ApplicationUser? customerUser = await userManager.FindByNameAsync("customer@example.com");
    if (customerUser == null)
    {
      customerUser = new ApplicationUser { UserName = "customer@example.com", Email = "customer@example.com", EmailConfirmed = true, FirstName = "Demo", LastName = "Customer" };
      var customerResult = await userManager.CreateAsync(customerUser, "CustP@$$wOrd1!");
      if (customerResult.Succeeded)
      {
        await userManager.AddToRoleAsync(customerUser, Roles.Customer);
        Console.WriteLine($"User '{customerUser.UserName}' created and assigned to Customer role.");
      }
      else { Console.WriteLine($"Error creating customer user: {string.Join(", ", customerResult.Errors.Select(e => e.Description))}"); }
    }
    else { Console.WriteLine($"User 'customer@example.com' already exists."); }

    return (vendorUser, customerUser);
  }

  private static async Task SeedContributorsAsync(AppDbContext dbContext)
  {
    Console.WriteLine("Seeding contributors...");
    if (!await dbContext.Contributors.AnyAsync())
    {
      dbContext.Contributors.AddRange(Contributor1, Contributor2);
      await dbContext.SaveChangesAsync();
      Console.WriteLine("Contributors seeded.");
    }
    else
    {
      Console.WriteLine("Contributors already exist.");
    }
  }

  // Removed UserManager from parameters as it's not needed here anymore for setting a non-existent scalar FK.
  private static async Task<BusinessProfile?> SeedBusinessProfileAsync(AppDbContext dbContext, ApplicationUser vendorUser)
  {
    const int targetBusinessProfileId = 1;
    Console.WriteLine($"Attempting to ensure BusinessProfile for vendor {vendorUser.UserName}, targeting ID {targetBusinessProfileId} if possible.");

    BusinessProfile? businessProfileToLink = null;

    BusinessProfile? existingProfileForThisVendor = await dbContext.BusinessProfiles
        .FirstOrDefaultAsync(bp => bp.VendorId == vendorUser.Id);

    if (existingProfileForThisVendor != null)
    {
      Console.WriteLine($"Vendor {vendorUser.UserName} is already linked to BusinessProfile ID {existingProfileForThisVendor.Id} ('{existingProfileForThisVendor.Name}').");
      businessProfileToLink = existingProfileForThisVendor;

      if (businessProfileToLink.Id != targetBusinessProfileId)
      {
        BusinessProfile? profileById1 = await dbContext.BusinessProfiles.FindAsync(targetBusinessProfileId);
        if (profileById1 != null && profileById1.VendorId != vendorUser.Id)
        {
          Console.WriteLine($"Note: BusinessProfile ID {targetBusinessProfileId} exists but is assigned to a different vendor ({profileById1.VendorId}). {vendorUser.UserName} remains linked to their current profile (ID {businessProfileToLink.Id}).");
        }
        else if (profileById1 == null)
        {
          Console.WriteLine($"Note: BusinessProfile ID {targetBusinessProfileId} does not exist. {vendorUser.UserName} remains linked to their current profile (ID {businessProfileToLink.Id}).");
        }
      }
    }
    else
    {
      BusinessProfile? profileById1 = await dbContext.BusinessProfiles.FindAsync(targetBusinessProfileId);
      if (profileById1 == null)
      {
        Console.WriteLine($"BusinessProfile with ID {targetBusinessProfileId} not found. Creating new one for {vendorUser.UserName}.");
        businessProfileToLink = new BusinessProfile(
            vendorId: vendorUser.Id,
            name: $"Demo Salon & Spa (ID {targetBusinessProfileId} Candidate)",
            address: "123 Demo Street, Belgrade, Serbia",
            phoneNumber: "011-555-1234",
            description: "Offering the finest demo services for your testing needs."
        );
        dbContext.BusinessProfiles.Add(businessProfileToLink);
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"New BusinessProfile created for {vendorUser.UserName} with actual ID {businessProfileToLink.Id}.");
        if (businessProfileToLink.Id != targetBusinessProfileId)
        {
          Console.WriteLine($"Critical Warning: Created new BusinessProfile for {vendorUser.UserName}, it received ID {businessProfileToLink.Id} instead of the targeted {targetBusinessProfileId}. This is expected if the table was not empty or ID is auto-generated.");
        }
      }
      else
      {
        if (profileById1.VendorId != vendorUser.Id)
        {
          Console.WriteLine($"BusinessProfile ID {targetBusinessProfileId} exists but is assigned to a different vendor ({profileById1.VendorId}). Creating a new, separate profile for {vendorUser.UserName}.");
          businessProfileToLink = new BusinessProfile(
              vendorId: vendorUser.Id,
              name: $"Demo Salon & Spa for {vendorUser.UserName} (New Profile)",
              address: "456 New Ave, Belgrade, Serbia",
              phoneNumber: "011-555-6789",
              description: "A distinct demo business for this vendor."
          );
          dbContext.BusinessProfiles.Add(businessProfileToLink);
          await dbContext.SaveChangesAsync();
          Console.WriteLine($"New BusinessProfile created for {vendorUser.UserName} with actual ID {businessProfileToLink.Id} because ID {targetBusinessProfileId} was taken.");
        }
        else
        {
          Console.WriteLine($"BusinessProfile ID {targetBusinessProfileId} already exists and is correctly assigned to vendor {vendorUser.UserName}.");
          businessProfileToLink = profileById1;
        }
      }
    }

    // No need to update ApplicationUser.BusinessProfileId directly here,
    // as that property doesn't exist on ApplicationUser.
    // The link is established by BusinessProfile.VendorId = vendorUser.Id.
    // The ApplicationUser.BusinessProfile navigation property will be populated by EF Core when queried with .Include().

    return businessProfileToLink;
  }

  private static async Task SeedServiceManagementDataAsync(
      AppDbContext dbContext,
      BusinessProfile businessProfile,
      ApplicationUser customerUser)
  {
    Console.WriteLine($"Starting to seed service management data for BusinessProfile: {businessProfile.Name} (ID: {businessProfile.Id})");

    Service? serviceHaircut = await dbContext.Services.FirstOrDefaultAsync(s => s.BusinessProfileId == businessProfile.Id && s.Name == "Demo Haircut");
    if (serviceHaircut == null)
    {
      serviceHaircut = new Service(businessProfile.Id, "Demo Haircut", 45, 2500.00m);
      serviceHaircut.SetDescription("A stylish haircut using demo techniques.");
      serviceHaircut.SetImageUrl("https://example.com/demohaircut.jpg");
      dbContext.Services.Add(serviceHaircut);
      Console.WriteLine($"Service 'Demo Haircut' added for BP ID {businessProfile.Id}.");
    }

    Service? serviceMassage = await dbContext.Services.FirstOrDefaultAsync(s => s.BusinessProfileId == businessProfile.Id && s.Name == "Demo Relax Massage");
    if (serviceMassage == null)
    {
      serviceMassage = new Service(businessProfile.Id, "Demo Relax Massage", 60, 3500.00m);
      serviceMassage.SetDescription("A relaxing massage to de-stress your demo day.");
      serviceMassage.Activate();
      dbContext.Services.Add(serviceMassage);
      Console.WriteLine($"Service 'Demo Relax Massage' added for BP ID {businessProfile.Id}.");
    }
    await dbContext.SaveChangesAsync();

    Worker? workerJohn = await dbContext.Workers.FirstOrDefaultAsync(w => w.BusinessProfileId == businessProfile.Id && w.Email == "john.stylist@example.com");
    if (workerJohn == null)
    {
      workerJohn = new Worker(businessProfile.Id, "John", "Stylist");
      workerJohn.UpdateDetails("John", "Stylist", "john.stylist@example.com", "060111222", "https://example.com/john.jpg", "Haircuts, Styling");
      dbContext.Workers.Add(workerJohn);
      Console.WriteLine($"Worker 'John Stylist' added for BP ID {businessProfile.Id}.");
    }

    Worker? workerJane = await dbContext.Workers.FirstOrDefaultAsync(w => w.BusinessProfileId == businessProfile.Id && w.Email == "jane.masseuse@example.com");
    if (workerJane == null)
    {
      workerJane = new Worker(businessProfile.Id, "Jane", "Masseuse");
      workerJane.UpdateDetails("Jane", "Masseuse", "jane.masseuse@example.com", "060333444", "https://example.com/jane.jpg", "Relaxation Massage");
      dbContext.Workers.Add(workerJane);
      Console.WriteLine($"Worker 'Jane Masseuse' added for BP ID {businessProfile.Id}.");
    }
    await dbContext.SaveChangesAsync();

    if (workerJohn == null) { Console.WriteLine("Worker John not found, cannot seed schedule for him."); return; }

    Schedule? scheduleJohn = await dbContext.Schedules
      .Include(s => s.RuleItems)
      .ThenInclude(ri => ri.Breaks)
      .FirstOrDefaultAsync(s => s.WorkerId == workerJohn.Id && s.IsDefault);

    if (scheduleJohn == null)
    {
      Console.WriteLine($"Default schedule for John Stylist not found. Creating...");
      scheduleJohn = new Schedule(
          workerJohn.Id,
          businessProfile.Id,
          "John's Regular Hours",
          DateOnly.FromDateTime(DateTime.UtcNow.Date),
          "Europe/Belgrade",
          true
      );
      dbContext.Schedules.Add(scheduleJohn);
      await dbContext.SaveChangesAsync();

      scheduleJohn.AddRuleItem(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), true);
      scheduleJohn.AddRuleItem(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0), true);
      scheduleJohn.AddRuleItem(DayOfWeek.Wednesday, new TimeOnly(9, 0), new TimeOnly(12, 30), true);
      scheduleJohn.AddRuleItem(DayOfWeek.Thursday, new TimeOnly(12, 0), new TimeOnly(20, 0), true);
      scheduleJohn.AddRuleItem(DayOfWeek.Friday, new TimeOnly(9, 0), new TimeOnly(17, 0), true);
      scheduleJohn.AddRuleItem(DayOfWeek.Saturday, new TimeOnly(0, 0), new TimeOnly(0, 0), false);
      scheduleJohn.AddRuleItem(DayOfWeek.Sunday, new TimeOnly(0, 0), new TimeOnly(0, 0), false);
      await dbContext.SaveChangesAsync();

      var mondayRule = await dbContext.ScheduleRuleItems
          .FirstOrDefaultAsync(r => r.ScheduleId == scheduleJohn.Id && r.DayOfWeek == DayOfWeek.Monday);
      if (mondayRule != null && (mondayRule.Breaks == null || !mondayRule.Breaks.Any(b => b.Name == "Lunch Break")))
      {
        mondayRule.AddBreak("Lunch Break", new TimeOnly(13, 0), new TimeOnly(14, 0));
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"Added lunch break to Monday rule for John's schedule.");
      }
      Console.WriteLine($"Default schedule created for John Stylist with rules.");
    }
    else { Console.WriteLine($"Default schedule for John Stylist already exists."); }

    AvailabilitySlot? slotForBooking = null;
    if (serviceHaircut != null && workerJohn != null)
    {
      var nextMonday = DateTime.UtcNow.Date;
      while (nextMonday.DayOfWeek != DayOfWeek.Monday) nextMonday = nextMonday.AddDays(1);
      if (nextMonday < DateTime.UtcNow.Date.AddDays(1)) nextMonday = nextMonday.AddDays(7);

      var slotTime = new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 10, 0, 0, DateTimeKind.Local);
      DateTime slotStartTimeUtc = TimeZoneInfo.ConvertTimeToUtc(slotTime, TimeZoneInfo.Local);
      DateTime slotEndTimeUtc = slotStartTimeUtc.AddMinutes(serviceHaircut.DurationInMinutes);

      slotForBooking = await dbContext.AvailabilitySlots.FirstOrDefaultAsync(s => s.WorkerId == workerJohn.Id && s.StartTime == slotStartTimeUtc);
      if (slotForBooking == null)
      {
        var scheduleJohnDb = await dbContext.Schedules.FirstOrDefaultAsync(s => s.WorkerId == workerJohn.Id && s.IsDefault);
        slotForBooking = new AvailabilitySlot(
            workerJohn.Id,
            businessProfile.Id,
            slotStartTimeUtc,
            slotEndTimeUtc,
            AvailabilitySlotStatus.Available,
            scheduleJohnDb?.Id
        );
        dbContext.AvailabilitySlots.Add(slotForBooking);
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"Availability slot created for John Stylist on {slotStartTimeUtc} UTC ({slotTime} Local).");
      }
      else { Console.WriteLine($"Availability slot for John Stylist on {slotStartTimeUtc} UTC ({slotTime} Local) already exists."); }
    }
    else { Console.WriteLine("Haircut service or Worker John not found, cannot seed availability slot for it."); }

    if (customerUser != null && serviceHaircut != null && workerJohn != null && slotForBooking != null && slotForBooking.Status == AvailabilitySlotStatus.Available)
    {
      var existingBooking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.AvailabilitySlotId == slotForBooking.Id);
      if (existingBooking == null)
      {
        var booking = new Booking(
            businessProfile.Id,
            customerUser.Id,
            serviceHaircut.Id,
            workerJohn.Id,
            slotForBooking.Id,
            slotForBooking.StartTime,
            slotForBooking.EndTime,
            serviceHaircut.Price
        );
        booking.UpdateNotes("First demo booking!", null);
        booking.ConfirmBooking();

        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();

        slotForBooking.BookSlot(booking.Id);
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"Booking created for customer {customerUser.UserName} with John Stylist.");
      }
      else { Console.WriteLine($"Booking for slot ID {slotForBooking.Id} already exists."); }
    }
    else { Console.WriteLine("Could not seed booking due to missing prerequisites (customer, service, worker, or available slot)."); }
    Console.WriteLine($"Service management data seeding finished for BusinessProfile: {businessProfile.Name}.");
  }
}
