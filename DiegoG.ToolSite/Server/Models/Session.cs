using Microsoft.AspNetCore.Http;
using DiegoG.ToolSite.Server.Database.Models.Base;

namespace DiegoG.ToolSite.Server.Models;

public class Session
{
    public required SessionId Id { get; init; }
    public required Id<User> UserId { get; init; }
    public required DateTime Created { get; init; }
    public required TimeSpan Expiration { get; init; }
    public required string? IPAddress { get; init; }
    public required DateTime LastUsed { get; set; }

    public static Session New(User user, HttpContext context)
        => new()
        {
            Created = DateTime.Now,
            Expiration = string.IsNullOrWhiteSpace(user.PasswordSha512) ? ServerProgram.Settings.AnonymousSessionTimeout : ServerProgram.Settings.UserSessionTimeout,
            Id = SessionId.NewId(),
            IPAddress = context.FindIPAddress(),
            LastUsed = DateTime.Now,
            UserId = user.Id,
        };
}
