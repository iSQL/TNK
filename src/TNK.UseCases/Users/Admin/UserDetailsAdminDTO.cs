namespace TNK.UseCases.Users.Admin;

public record UserDetailsAdminDTO(
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool EmailConfirmed,
    List<string> Roles
);
