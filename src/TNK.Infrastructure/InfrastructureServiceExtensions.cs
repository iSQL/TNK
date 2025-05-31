using TNK.Core.Interfaces;
using TNK.Core.ServiceManagementAggregate.Interfaces;
using TNK.Core.Services;
using TNK.Infrastructure.Data;
using TNK.Infrastructure.Data.Queries;
using TNK.Infrastructure.Data.ServiceManagementRepositories;
using TNK.UseCases.Contributors.List;


namespace TNK.Infrastructure;
public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    ConfigurationManager config,
    ILogger logger)
  {
    string? connectionString = config.GetConnectionString("SqliteConnection");
    Guard.Against.Null(connectionString);
    services.AddDbContext<AppDbContext>(options =>
     options.UseSqlite(connectionString));

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
               .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>))
               .AddScoped<IListContributorsQueryService, ListContributorsQueryService>()
               .AddScoped<IDeleteContributorService, DeleteContributorService>();
    services.AddScoped<IBusinessProfileRepository, BusinessProfileRepository>();

    // Register Service Management repositories
    services.AddScoped<IServiceRepository, ServiceRepository>();
    services.AddScoped<IWorkerRepository, WorkerRepository>();
    services.AddScoped<IScheduleRepository, ScheduleRepository>();
    services.AddScoped<IAvailabilitySlotRepository, AvailabilitySlotRepository>();
    services.AddScoped<IBookingRepository, BookingRepository>();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }
}
