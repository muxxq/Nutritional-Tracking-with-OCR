using NutriScan.Domain.Entities;

namespace NutriScan.Application.Auth.Dtos;

public sealed record UserDto(Guid Id, string Email, string FirstName, string LastName, bool IsProfileComplete)
{
    public static UserDto From(User user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.IsProfileComplete);
}
