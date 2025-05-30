using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using TNK.Core.Interfaces;
using TNK.Infrastructure;
using TNK.Infrastructure.Email;

namespace TNK.Web.Configurations;

public static class ServiceConfigs
{
  public static IServiceCollection AddServiceConfigs(this IServiceCollection services, Microsoft.Extensions.Logging.ILogger logger, WebApplicationBuilder builder)
  {
    services.AddInfrastructureServices(builder.Configuration, logger)
            .AddMediatrConfigs();

    
    if (builder.Environment.IsDevelopment())
    {
      // Use a local test email server
      // See: https://ardalis.com/configuring-a-local-test-email-server/
      services.AddScoped<IEmailSender, MimeKitEmailSender>();

      // Otherwise use this:
      //builder.Services.AddScoped<IEmailSender, FakeEmailSender>();

    }
    else
    {
      services.AddScoped<IEmailSender, MimeKitEmailSender>();
    }
    builder.Services.AddScoped<Auth.TokenService>();

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


    logger.LogInformation("{Project} services registered", "Mediatr, TokenService and Email Sender");

    return services;
  }


}
