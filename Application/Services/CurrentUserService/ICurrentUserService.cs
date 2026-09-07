namespace Application.Services.CurrentUserService
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? Name { get; }
        string? Email { get; }
        string? PhoneNumber { get; }
        bool IsAuthenticated { get; }
        IReadOnlyList<string> Roles { get; }
        IReadOnlyList<string> Permissions { get; }
        string? IpAddress { get; }
    }
}
