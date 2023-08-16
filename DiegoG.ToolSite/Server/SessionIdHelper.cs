using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

namespace DiegoG.ToolSite.Server;

public static partial class SessionIdHelper
{
    [GeneratedRegex(@"^Bearer (?<session>[A-z0-9+/]{43}=)$")]
    private static partial Regex AuthorizationRegex();

    public static SessionId ParseAuthorizationHeader(string bearerHeader)
        => SessionId.Parse(AuthorizationRegex().Match(bearerHeader).Groups["session"].Value);

    public static bool TryParseAuthorizationHeader(string bearerHeader, out SessionId sessionId)
        => SessionId.TryParse(AuthorizationRegex().Match(bearerHeader).Groups["session"].Value, null, out sessionId);
}
