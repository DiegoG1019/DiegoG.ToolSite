using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Serilog;
using DiegoG.ToolSite.Server.Attributes;
using DiegoG.ToolSite.Server.Database;
using DiegoG.ToolSite.Server.Database.Models.Base;
using DiegoG.ToolSite.Server.Types;
using DiegoG.ToolSite.Shared.Models;
using DiegoG.ToolSite.Shared.Models.Requests;

namespace DiegoG.ToolSite.Server.Services;

[RegisterToolSiteService(ServiceLifetime.Scoped)]
public class UserManager
{
    private readonly static TimedCache<Id<User>, UserPermission> PermissionCache = new((k, v) => ServerProgram.Settings.PermissionCacheTimeout);
    private readonly ToolSiteContext Db;

    public UserManager(ToolSiteContext context)
    {
        Db = context;
    }

    public async ValueTask<IEnumerable<ServiceItemDescription>> FetchUserServices(Id<User> userId)
    {
        List<ServiceItemDescription>? services = null;

        var p = await FetchRolePermissions(userId);

        if (p.HasFlag(UserPermission.AccessCalendar))
            Add(ServiceItemDescriptionStore.CalendarService);

        if (p.HasFlag(UserPermission.AccessLedger))
            Add(ServiceItemDescriptionStore.LedgerService);

        return services ?? Enumerable.Empty<ServiceItemDescription>();

        void Add(ServiceItemDescription sid)
            => (services ??= new()).Add(sid);
    }

    public Task<User?> FindUser(Id<User> userId)
    {
        return Db.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }

    public Task AddUser(User user)
    {
        Db.Users.Add(user);
        return Db.SaveChangesAsync();
    }

    public Task<User?> CheckLogin(LoginRequest request)
    {
        var hash512 = Helper.GetHash512(request.Password);
        return Db.Users.FirstOrDefaultAsync(
            x => x.Email != null && (EF.Functions.Like(x.Email, request.Username) || EF.Functions.Like(x.Username, request.Username))
                && x.PasswordHash != null
                && x.PasswordHash == hash512
        );
    }

    public Task<bool> CheckIfEmailExists(MailAddress address)
        => Db.Users.AnyAsync(x => x.Email != null && EF.Functions.Like(x.Email, address.Address));

    public Task<bool> CheckIfUsernameExists(string username)
        => Db.Users.AnyAsync(x => EF.Functions.Like(x.Username, username));

    public async Task<(bool Found, Id<MailConfirmationRequest>? RequestId)> ChangeUserEmail(Id<User> userId, MailAddress newEmail)
    {
        ArgumentNullException.ThrowIfNull(newEmail);

        var user = await Db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null)
            return (false, null);

        if (user.Email is not null && string.Equals(user.Email, newEmail.Address, StringComparison.InvariantCultureIgnoreCase))
            return (true, null);

        user.IsMailConfirmed = false;
        user.EmailAddress = newEmail;
        return (true, await CreateNewConfirmationRequest(user));
    }

    public async Task<bool?> ConfirmRequest(Id<MailConfirmationRequest> requestId)
    {
        var mcr = await Db.MailConfirmationRequests.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == requestId);
        if (mcr is null)
            return null;

        if (mcr.User.IsMailConfirmed)
            return false;

        mcr.User.IsMailConfirmed = true;
        await Db.SaveChangesAsync();

        return true;
    }

    public ValueTask<UserPermission> FetchRolePermissions(Id<User> userId) 
        => PermissionCache.PeekOrAddAsync(userId, async k => await AggregatePermissionsFromDb(k));

    private async Task<UserPermission> AggregatePermissionsFromDb(Id<User> userId)
    {
        UserPermission upp = 0;

        await foreach (var up in Db.Users
            .Where(x => x.Id == userId)
            .SelectMany(x => x.Roles)
            .Select(x => x.UserPermissions)
            .AsAsyncEnumerable())
        {
            upp |= up;
        }

        return upp;
    }

    public async Task<Id<MailConfirmationRequest>> CreateNewConfirmationRequest(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (user.Email is null)
            throw new ArgumentException("The user's email cannot be null", nameof(user));

        await Db.MailConfirmationRequests.Where(x => x.User.Id == user.Id).ExecuteDeleteAsync();
        var conf = new MailConfirmationRequest()
        {
            CreationDate = DateTimeOffset.Now,
            Email = user.Email,
            Id = Id<MailConfirmationRequest>.New(),
            UserId = user.Id
        };

        Db.MailConfirmationRequests.Add(conf);
        await Db.SaveChangesAsync();

        return conf.Id;
    }
}
