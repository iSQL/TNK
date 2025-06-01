// File: TNK.UseCases/Services/ListByBusiness/ListServicesByBusinessQuery.cs
using Ardalis.Result;
using MediatR;
using TNK.UseCases.Services; // For ServiceDTO

namespace TNK.UseCases.Services.ListByBusiness;

public record ListServicesByBusinessQuery(
    int BusinessProfileId
// int PageNumber = 1, // Example for pagination
// int PageSize = 10   // Example for pagination
) : IRequest<Result<List<ServiceDTO>>>; // Or Result<PagedResult<ServiceDTO>> if using pagination
