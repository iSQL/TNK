using Microsoft.AspNetCore.Identity;
using TNK.Core.Identity;
using TNK.Core.Interfaces;
using TNK.Core.Services;
using TNK.Infrastructure.Data;
using TNK.Infrastructure.Data.Queries;
using TNK.UseCases.Contributors.List;
using Microsoft.Extensions.DependencyInjection;

namespace TNK.Infrastructure;
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
      this IServiceCollection services,
      ConfigurationManager config,
      ILogger logger)
    {
        string? connectionString = config.GetConnectionString("PostgreSqlConnection");
        Guard.Against.Null(connectionString);
        services.AddDbContext<AppDbContext>(options =>
         options.UseNpgsql(connectionString));

        // Ensure the required package is installed: Microsoft.AspNetCore.Identity.EntityFrameworkCore
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
             options.Password.RequireDigit = false;
             options.Password.RequiredLength = 3;
             options.User.RequireUniqueEmail = true;
            options.Password.RequireUppercase = false;
        })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders(); 


        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
               .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>))
               .AddScoped<IListContributorsQueryService, ListContributorsQueryService>()
               .AddScoped<IDeleteContributorService, DeleteContributorService>();


        logger.LogInformation("{Project} services registered", "Infrastructure");

        return services;
    }
}
