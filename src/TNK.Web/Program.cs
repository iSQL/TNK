using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TNK.Web.Configurations;
using FastEndpoints.Swagger;

var builder = WebApplication.CreateBuilder(args);
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
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
});

builder.Services.AddAuthorization();


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

builder.Services.AddFastEndpoints()
                .SwaggerDocument(o =>
                {
                  o.ShortSchemaNames = true;
                });
// Configure FastEndpoints.Swagger - NSwag
builder.Services.AddSwaggerDocument(settings =>
{
  settings.DocumentName = "v1.0";
  settings.Title = "TerminNaKlik API";
  settings.Version = "v1.0";
  // Add JWT Bearer authentication to Swagger UI
  settings.AddAuth("BearerAuth", new NSwag.OpenApiSecurityScheme
  {
    Name = "Authorization",
    In = NSwag.OpenApiSecurityApiKeyLocation.Header,
    Type = NSwag.OpenApiSecuritySchemeType.Http, 
    Scheme = "bearer", 
    BearerFormat = "JWT",
    Description = "Input your Bearer token in this format: Bearer {token}"
  });
}); // If using System.Text.Json source generation


var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
await app.UseAppMiddlewareAndSeedDatabase();

app.Run();

// Make the implicit Program.cs class public, so integration tests can reference the correct assembly for host building
public partial class Program { }
