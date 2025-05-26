using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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




    logger.LogInformation("{Project} services registered", "Mediatr, TokenService and Email Sender");

    return services;
  }


}
