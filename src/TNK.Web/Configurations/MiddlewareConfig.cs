using Ardalis.ListStartupServices;
using Microsoft.Extensions.Options;
using TNK.Infrastructure.Data;

namespace TNK.Web.Configurations;

public static class MiddlewareConfig
{
  public static async Task<IApplicationBuilder> UseAppMiddlewareAndSeedDatabase(this WebApplication app)
  {
    if (app.Environment.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
      app.UseShowAllServicesMiddleware(); // see https://github.com/ardalis/AspNetCoreStartupServices
    }
    else
    {
      app.UseDefaultExceptionHandler(); 
      app.UseHsts();
    }

    //Localization Configuration
    app.UseRequestLocalization(
    app.Services
       .GetRequiredService<IOptions<RequestLocalizationOptions>>()
       .Value);

    // CORS Configuration
    app.UseCors("_localAngularOrigin");

    // Swagger Configuration
    app.UseFastEndpoints()
        .UseSwaggerGen();

    // Authentication and Authorization Configuration
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseHttpsRedirection(); 

    await SeedDatabase(app);

    return app;
  }

  static async Task SeedDatabase(WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
      await SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
      var logger = services.GetRequiredService<ILogger<Program>>();
      logger.LogError(ex, "An error occurred seeding the DB. {exceptionMessage}", ex.Message);
    }
  }
}
