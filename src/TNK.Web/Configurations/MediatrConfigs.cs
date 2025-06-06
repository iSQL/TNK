﻿using Ardalis.SharedKernel;
using TNK.Core.ContributorAggregate;
using TNK.UseCases.Contributors.Create;
using MediatR;
using System.Reflection;
using FluentValidation;

namespace TNK.Web.Configurations;

public static class MediatrConfigs
{
  public static IServiceCollection AddMediatrConfigs(this IServiceCollection services)
  {
    var mediatRAssemblies = new[]
      {
        Assembly.GetAssembly(typeof(Contributor)), // Core
        Assembly.GetAssembly(typeof(CreateContributorCommand)) // UseCases
      };

    services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(mediatRAssemblies!))
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
            .AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

    if (mediatRAssemblies.Length > 0)
    {
      services.AddValidatorsFromAssemblies(mediatRAssemblies!);
    }

    return services;
  }
}
