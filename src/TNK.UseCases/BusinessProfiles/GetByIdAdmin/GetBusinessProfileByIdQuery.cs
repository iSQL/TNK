using MediatR;

namespace TNK.UseCases.BusinessProfiles.GetByIdAdmin;

/// <summary>
/// Represents a query to fetch a specific Business Profile by its ID for a SuperAdmin.
/// </summary>
/// <param name="BusinessProfileId">The ID of the Business Profile to retrieve.</param>
public record GetBusinessProfileByIdAdminQuery(int BusinessProfileId) : IRequest<Result<BusinessProfileDTO>>;
