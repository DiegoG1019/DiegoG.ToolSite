namespace DiegoG.ToolSite.Shared.Models.Requests;

public record class LoginRequest(string UsernameOrEmail, string PasswordSha256);
