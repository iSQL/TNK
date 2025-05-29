using Ardalis.Result;
using MediatR;

namespace TNK.UseCases.BusinessProfiles.DeleteAdmin;

/// <summary>
/// Represents a command to delete a specific Business Profile by its ID, intended for SuperAdmin use.
/// </summary>
/// <param name="BusinessProfileId">The ID of the Business Profile to delete.</param>
public record AdminDeleteBusinessProfileCommand(int BusinessProfileId) : IRequest<Result>;
