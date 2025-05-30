using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using TNK.Core.Identity;
using TNK.Core.Interfaces;
using TNK.Core.Services;
using TNK.Infrastructure.Data;
using TNK.Infrastructure.Data.Queries;
using TNK.UseCases.Contributors.List;

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

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
          options.Password.RequireDigit = false;
          options.Password.RequiredLength = 3;
          options.User.RequireUniqueEmail = true;
          options.Password.RequireUppercase = false;
          options.Password.RequireNonAlphanumeric = false;
          options.Password.RequireLowercase = false;
          options.Lockout.MaxFailedAccessAttempts = int.MaxValue;

        }).AddEntityFrameworkStores<AppDbContext>()
          .AddDefaultTokenProviders();

    services.ConfigureApplicationCookie(options =>
    {
      options.Events = new CookieAuthenticationEvents 
      {
        OnRedirectToLogin = context =>
        {
          if (context.Request.Path.StartsWithSegments("/api"))
          {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
          }
          else
          {
            context.Response.Redirect(context.RedirectUri);
          }
          return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
          if (context.Request.Path.StartsWithSegments("/api"))
          {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
          }
          else
          {
            context.Response.Redirect(context.RedirectUri);
          }
          return Task.CompletedTask;
        }
      };
    });

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
               .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>))
               .AddScoped<IListContributorsQueryService, ListContributorsQueryService>()
               .AddScoped<IDeleteContributorService, DeleteContributorService>();
    services.AddScoped<IBusinessProfileRepository, BusinessProfileRepository>();


    logger.LogInformation("{Project} services registered", "Infrastructure");

        return services;
    }
}
