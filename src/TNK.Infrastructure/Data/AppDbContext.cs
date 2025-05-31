
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using TNK.Core.BusinessAggregate;
using TNK.Core.ContributorAggregate;
using TNK.Core.Identity;

namespace TNK.Infrastructure.Data;

public class AppDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
  private readonly IDomainEventDispatcher? _dispatcher;

  public AppDbContext(
      DbContextOptions<AppDbContext> options,
      IDomainEventDispatcher? dispatcher)
      : base(options)
  {
    _dispatcher = dispatcher;
  }

  public DbSet<Contributor> Contributors { get; set; }
  public DbSet<BusinessProfile> BusinessProfiles { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }

  public override async Task<int> SaveChangesAsync(
      CancellationToken cancellationToken = default)
  {
    var result = await base.SaveChangesAsync(cancellationToken)
                         .ConfigureAwait(false);

    if (_dispatcher is not null)
    {
      var entitiesWithEvents = ChangeTracker
          .Entries<HasDomainEventsBase>()
          .Select(e => e.Entity)
          .Where(e => e.DomainEvents.Any())
          .ToArray();

      await _dispatcher.DispatchAndClearEvents(entitiesWithEvents)
                       .ConfigureAwait(false);
    }

    return result;
  }

  public override int SaveChanges()
      => SaveChangesAsync(CancellationToken.None)
         .GetAwaiter()
         .GetResult();
}
