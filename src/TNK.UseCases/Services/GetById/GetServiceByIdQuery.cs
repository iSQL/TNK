// File: TNK.UseCases/Services/GetById/GetServiceByIdQuery.cs
using Ardalis.Result;
using MediatR;
using TNK.UseCases.Services; // For ServiceDTO

namespace TNK.UseCases.Services.GetById;

public record GetServiceByIdQuery(
    Guid ServiceId,
    int BusinessProfileId // For authorization purposes
) : IRequest<Result<ServiceDTO>>;
