using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using TNK.Core.Identity;
using TNK.Core.Interfaces;
using TNK.Web.Configurations;
using TNK.Web.Resources;
using TNK.Web.Services;

var builder = WebApplication.CreateBuilder(args);

//Logger Configuration
var logger = Log.Logger = new LoggerConfiguration()
  .Enrich.FromLogContext()
  .WriteTo.Console()
  .CreateLogger();
logger.Information("Starting web host");
builder.AddLoggerConfigs();
var appLogger = new SerilogLoggerFactory(logger)
    .CreateLogger<Program>();

builder.Services.AddOptionConfigs(builder.Configuration, appLogger, builder);
builder.Services.AddServiceConfigs(appLogger, builder);

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>(); // Or AddTransient/AddSingleton depending on lifetime needs, Scoped is common

var app = builder.Build();

app.Logger.LogInformation("=== Localization Smoke-Test ===");
using (var scope = app.Services.CreateScope())
{
  var localizer = scope.ServiceProvider
                       .GetRequiredService<IStringLocalizer<SharedResources>>();

  var defaultMsg = localizer["InvalidLoginAttempt"];
  app.Logger.LogInformation("Default (en) → {Message}", defaultMsg);

  var srCulture = new CultureInfo("sr-Latn-RS");
  CultureInfo.CurrentCulture = srCulture;
  CultureInfo.CurrentUICulture = srCulture;

  var srMsg = localizer["InvalidLoginAttempt"];
  app.Logger.LogInformation("Serbian-Latin (sr-Latn-RS) → {Message}", srMsg);
}
app.Logger.LogInformation("=== End Smoke-Test ===");


await app.UseAppMiddlewareAndSeedDatabase();

app.Run();

public partial class Program { }
