using MediatR;

namespace TNK.UseCases.Users.Admin;

public record ListUsersAdminQuery(int? PageNumber, int PageSize, string? SearchTerm)
    : IRequest<Result<Common.Models.PagedResult<UserDetailsAdminDTO>>>;
