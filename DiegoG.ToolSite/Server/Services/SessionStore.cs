using System.Diagnostics.CodeAnalysis;
using DiegoG.ToolSite.Shared.Models;

namespace DiegoG.ToolSite.Server.Services;

[RegisterToolSiteService(ServiceLifetime.Singleton)]
public class SessionStore
{
    private readonly static TimedCache<SessionId, Session> SessionCache = new((k, v) => v.Expiration);

    public bool TryGetSession(SessionId sessionId, [NotNullWhen(true)] out Session? session) 
        => sessionId == default
            ? throw new ArgumentException("value cannot be default", nameof(sessionId))
            : SessionCache.TryGetValue(sessionId, out session);

    public void AddSession(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);
        
        if (session.Id == default)
            throw new ArgumentException("The Id of the value cannot be default", nameof(session));

        if (SessionCache.TryAdd(session.Id, session) is false)
            throw new InvalidOperationException("A session by the same Id already exists in this store");
    }

    public void DestroySession(SessionId sessionId)
    {
        if (sessionId == default)
            throw new ArgumentException("value cannot be default", nameof(sessionId));
        SessionCache.TryRemove(sessionId, out _);
    }
}
