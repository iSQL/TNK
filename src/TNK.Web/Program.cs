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
using YamlDotNet.Serialization;

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

builder.Services.AddOptionConfigs(builder.Configuration, appLogger, builder);
builder.Services.AddServiceConfigs(appLogger, builder);

//CORS Configuration
builder.Services.AddCors(options =>
{
  options.AddPolicy(name: "_localAngularOrigin", //TODO: reuse this name in the app from config or something
                    policy =>
                    {
                      policy.WithOrigins("http://localhost:4200", "https://localhost:57679") // Your Angular app's origin
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials(); 
                    });
});

//Auth and Authorization
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);
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
      s.AddAuth(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme // Using "Bearer" as the scheme name
      {
        Name = "Authorization", // This is the header name
        In = OpenApiSecurityApiKeyLocation.Header,
        Type = OpenApiSecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme, // Specifies the scheme is "Bearer"
        BearerFormat = "JWT", // Indicates the format of the bearer token
        Description = "Input your Bearer token in this format - {token}"
      });
    };

  });


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
app.UseRequestLocalization(
    app.Services
       .GetRequiredService<IOptions<RequestLocalizationOptions>>()
       .Value);

await app.UseAppMiddlewareAndSeedDatabase();

app.Run();

// Make the implicit Program.cs class public, so integration tests can reference the correct assembly for host building
public partial class Program { }
