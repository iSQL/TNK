using System.Globalization;
using System.Resources;
using System.Text;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using TNK.Web.Configurations;
using TNK.Web.Resources;

var builder = WebApplication.CreateBuilder(args);

// Localization Configuration
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
  var supportedCultures = new[]
  {
        new CultureInfo("en"),
        new CultureInfo("sr-Latn-RS")
    };

  options.DefaultRequestCulture = new RequestCulture("sr-Latn-RS");
  options.SupportedCultures = supportedCultures;
  options.SupportedUICultures = supportedCultures;
});

//Logger Configuration
var logger = Log.Logger = new LoggerConfiguration()
  .Enrich.FromLogContext()
  .WriteTo.Console()
  .CreateLogger();

logger.Information("Starting web host");

builder.AddLoggerConfigs();

var appLogger = new SerilogLoggerFactory(logger)
    .CreateLogger<Program>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);



builder.Services.AddOptionConfigs(builder.Configuration, appLogger, builder);
builder.Services.AddServiceConfigs(appLogger, builder);

//CORS Configuration

builder.Services.AddCors(options =>
{
  options.AddPolicy(name: "_localAngularOrigin", //TODO: reuse this name in the app from config or something
                    policy =>
                    {
                      policy.WithOrigins("http://localhost:4200") // Your Angular app's origin
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials(); 
                    });
});

//Auth and Authorization
builder.Services.AddAuthentication(options =>
{
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtSettings["Issuer"],
    ValidAudience = jwtSettings["Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
    ClockSkew = TimeSpan.Zero
  };
  options.Events = new JwtBearerEvents
  {
    OnAuthenticationFailed = context =>
    {
      var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
      logger.LogError(context.Exception, "JWT Authentication Failed");
      return Task.CompletedTask;
    },
    OnTokenValidated = context =>
    {
      var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
      logger.LogInformation("JWT Token Validated for: {User}", context.Principal?.Identity?.Name);
      return Task.CompletedTask;
    },
    OnMessageReceived = context =>
    {
      var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
      logger.LogInformation("JWT Message Received. Token found in header: {TokenExists}", !string.IsNullOrEmpty(context.Token));
      return Task.CompletedTask;
    }
  };
});


builder.Services.AddAuthorization();

// API and Swagger Configuration
builder.Services
  .AddFastEndpoints()
  .SwaggerDocument(o =>
  {
    o.ShortSchemaNames = true;
    o.DocumentSettings = s =>
    {
      s.Title = "TerminNaKlik API";
      s.Version = "v1.0";
    };
  });


var app = builder.Build();
var rm = new ResourceManager(
    "TNK.Web.Resources.SharedResources",   // your root namespace + folder + class
    typeof(SharedResources).Assembly);

// test invariant
var en = rm.GetString("InvalidLoginAttempt", CultureInfo.GetCultureInfo("en"));
app.Logger.LogInformation("en → {Message}", en);

// test Serbian-Latin
var sr = rm.GetString("InvalidLoginAttempt", CultureInfo.GetCultureInfo("sr-Latn-RS"));
app.Logger.LogInformation("sr-Latn-RS → {Message}", sr);



var resources = typeof(SharedResources).Assembly
                    .GetManifestResourceNames();
app.Logger.LogInformation("Embedded resources: {Names}",
    string.Join("; ", resources));

app.Logger.LogInformation("=== Localization Smoke-Test ===");
using (var scope = app.Services.CreateScope())
{
  var localizer = scope.ServiceProvider
                       .GetRequiredService<IStringLocalizer<SharedResources>>();

  // 1. Default culture (as configured)
  var defaultMsg = localizer["InvalidLoginAttempt"];
  app.Logger.LogInformation("Default (en) → {Message}", defaultMsg);

  // 2. Serbian (Latin)
  var srCulture = new CultureInfo("sr-Latn-RS");
  CultureInfo.CurrentCulture = srCulture;
  CultureInfo.CurrentUICulture = srCulture;

  var srMsg = localizer["InvalidLoginAttempt"];
  app.Logger.LogInformation("Serbian-Latin (sr-Latn-RS) → {Message}", srMsg);
}

app.Logger.LogInformation("=== End Smoke-Test ===");

// Configure RequestLocalizationOptions
app.UseRequestLocalization(
    app.Services
       .GetRequiredService<IOptions<RequestLocalizationOptions>>()
       .Value);

await app.UseAppMiddlewareAndSeedDatabase();

app.Run();

// Make the implicit Program.cs class public, so integration tests can reference the correct assembly for host building
public partial class Program { }
