using DiegoG.ToolSite.Server.Database;

namespace DiegoG.ToolSite.Server.Workers;

//[RegisterToolSiteWorker]
public class DatabaseCleanup : ApiServiceWorker
{
    public DatabaseCleanup(IServiceProvider rootProvider) : base(rootProvider)
    {
    }

    public override async Task Work(CancellationToken stoppingToken)
    {
        Log.Verbose("Obtaining Service Scope");
        using var s = GetNewScopedServices();
        using var db = s.GetRequiredService<ToolSiteContext>();

        try
        {
            await CleanupExpiredMailConfirms(db, stoppingToken);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Failed to clean up expired email confirmations");
        }

        try
        {
            await CleanupExpiredServers(db, stoppingToken);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Failed to clean up expired servers");
        }

        try
        {
            await CleanupExpiredUsers(db, stoppingToken);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Failed to clean up expired unconfirmed users");
        }

        var wait = ServerProgram.Settings.DatabaseCleanupInterval;
        Log.Verbose("Sleeping for {interval}", wait);
        await Task.Delay(wait, stoppingToken);
    }

    private async Task CleanupExpiredMailConfirms(ToolSiteContext db, CancellationToken stoppingToken)
    {
        var exp = ServerProgram.Settings.MailConfirmationRequestExpiration;
        var dtnow = DateTimeOffset.Now - exp;
        Log.Debug("Cleaning up Expired Mail Confirmations");

        var deleted = await db.Database.ExecuteSqlRawAsync($"delete from MailConfirmationRequests where TODATETIMEOFFSET('{dtnow:yyyy-MM-dd hh:mm:ss.fffffff}', '{dtnow:zzz}') > CreationDate", stoppingToken);

        if (deleted > 0)
            Log.Information("Removed entries for {deleted} expired mail confirmation requests", deleted);
        else
            Log.Debug("Found no expired mail confirmation requests to remove");
    }

    private async Task CleanupExpiredServers(ToolSiteContext db, CancellationToken stoppingToken)
    {
        var dtnow = DateTimeOffset.Now;
        Log.Debug("Cleaning up Expired Inactive Servers");

        int deleted = 0;
        await foreach (var server in db.Servers.AsAsyncEnumerable())
        {
            if (server.LastHeartbeat + server.HeartbeatInterval * 2 < dtnow)
            {
                db.Servers.Remove(server);
                deleted++;
            }
        }

        await db.SaveChangesAsync(stoppingToken);

        if (deleted > 0)
            Log.Information("Removed entries for {deleted} expired servers", deleted);
        else
            Log.Debug("Found no expired servers to remove");
    }

    private async Task CleanupExpiredUsers(ToolSiteContext db, CancellationToken stoppingToken)
    {
        var exp = ServerProgram.Settings.UnconfirmedUserExpiration;
        var dtnow = DateTimeOffset.Now - exp;
        Log.Debug("Cleaning up Expired (Unconfirmed) Users");
        var deleted = await db.Database
            .ExecuteSqlRawAsync($"delete from Users where IsMailConfirmed = 0 and CreationDate < TODATETIMEOFFSET('{dtnow:yyyy-MM-dd hh:mm:ss.fffffff}', '{dtnow:zzz}')", stoppingToken);

        if (deleted > 0)
            Log.Information("Removed entries for {deleted} expired users", deleted);
        else
            Log.Debug("Found no expired users to remove");
    }
}
