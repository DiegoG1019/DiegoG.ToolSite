using DiegoG.ToolSite.Shared.Logging.Enrichers;

namespace DiegoG.ToolSite.Server;

public record class AppSettings(
    TimeSpan WorkerDelayOnError,
    TimeSpan WorkerMaxDelayOnError,
    TimeSpan AnonymousSessionTimeout,
    TimeSpan UserSessionTimeout,
    TimeSpan UnconfirmedUserExpiration,
    TimeSpan MailConfirmationRequestExpiration,
    TimeSpan DispatchModelAfterClaimDelay,
    TimeSpan DispatchModelClaimExpiration,
    TimeSpan HeartbeatInterval,
    TimeSpan DatabaseCleanupInterval,
    TimeSpan PermissionCacheTimeout,
    string FrontFacingBaseAddress,
    string ClientName,
    bool EnableDatabaseVerification
)
{
    public void Validate()
    {
        List<Exception> exceptions = new();

        if (DispatchModelAfterClaimDelay.TotalSeconds < 1)
            exceptions.Add(new ArgumentException("DispatchModelAfterClaimDelay must be longer than one second"));
        else if (DispatchModelAfterClaimDelay > DispatchModelClaimExpiration + TimeSpan.FromSeconds(30))
            exceptions.Add(new ArgumentException("DispatchModelAfterClaimDelay must be longer than DispatchModelClaimExpiration by at least 30 seconds"));

        if (DatabaseCleanupInterval.TotalSeconds < 1)
            exceptions.Add(new ArgumentException("DatabaseCleanupInterval must be longer than one second"));

        if (DispatchModelClaimExpiration.TotalSeconds < 1)
            exceptions.Add(new ArgumentException("DispatchModelClaimExpiration must be longer than one second"));

        if (HeartbeatInterval.TotalSeconds < 1)
            exceptions.Add(new ArgumentException("HeartbeatInterval must be longer than one second"));

        if (UnconfirmedUserExpiration.TotalSeconds < 1)
            exceptions.Add(new ArgumentException("UnconfirmedUserExpiration must be longer than one second"));

        if (PermissionCacheTimeout.TotalSeconds < 1)
            exceptions.Add(new ArgumentException("PermissionCacheTimeout must be longer than one second"));

        if (MailConfirmationRequestExpiration.TotalSeconds < 1)
            exceptions.Add(new ArgumentException("MailConfirmationRequestExpiration must be longer than one second"));

        if (AnonymousSessionTimeout.TotalSeconds < 1)
            exceptions.Add(new ArgumentException("AnonymousSessionTimeout must be longer than one second"));

        if (UserSessionTimeout.TotalSeconds < 1)
            exceptions.Add(new ArgumentException("UserSessionTimeout must be longer than one second"));

        if (WorkerDelayOnError.TotalSeconds < 1)
            exceptions.Add(new ArgumentException("WorkerDelayOnError must be longer than one second"));

        if (string.IsNullOrWhiteSpace(FrontFacingBaseAddress))
            exceptions.Add(new ArgumentException("FrontFacingBaseAddress must not be null or only whitespace"));

        if (string.IsNullOrWhiteSpace(ClientName))
            exceptions.Add(new ArgumentException("ClientName must not be null or only whitespace"));

        if (WorkerMaxDelayOnError.TotalSeconds < 1)
            exceptions.Add(new ArgumentException("WorkerMaxDelayOnError must be longer than one second"));
        if (WorkerMaxDelayOnError < WorkerDelayOnError)
            exceptions.Add(new ArgumentException("WorkerMaxDelayOnError must not be less than WorkerDelayOnError"));

        if (exceptions.Count > 0)
            if (exceptions.Count is 1)
                throw exceptions[0];
            else
                throw new AggregateException("One or more errors ocurred while validating settings", exceptions);
    }
}
