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
using DiegoG.ToolSite.Shared;

namespace DiegoG.ToolSite.Server.Services;

[RegisterToolSiteService(ServiceLifetime.Scoped)]
public class UserManager
{
    private readonly static TimedCache<Id<User>, UserPermission> PermissionCache = new((k, v) => ServerProgram.Settings.PermissionCacheTimeout);
    private readonly static TimedCache<Id<User>, User> UserCache = new((k, v) => TimeSpan.FromSeconds(30));

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

    /// <summary>
    /// Fetches an user from cache, or finds it in the database if not available
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public async ValueTask<User> FetchOrFindUser(Id<User> userId)
        => await UserCache.GetOrAddAsync(
            userId,
            async k => await Db.Users.FindAsync(k) ?? throw new InvalidDataException($"The user Id '{k}' did not match any users")
        );

    public Task<User?> FindUser(Id<User> userId)
    {
        return Db.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }

    public Task<bool> CheckForUsernameConflict(string username, Id<User>? userId)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);

        return userId is not Id<User> uid
            ? Db.Users.AnyAsync(x => EF.Functions.Like(x.Username, username))
            : Db.Users.AnyAsync(x => EF.Functions.Like(x.Username, username) && x.Id != uid);
    }

    public Task<bool> CheckForEmailConflict(string email, Id<User>? userId)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);

        return userId is not Id<User> uid
            ? Db.Users.AnyAsync(x => x.Email != null && EF.Functions.Like(x.Email, email))
            : Db.Users.AnyAsync(x => x.Email != null && EF.Functions.Like(x.Email, email) && x.Id != uid);
    }

    //public Task<bool> CheckForConflict(string username, string email, Id<User>? userId)
    //{
    //    ArgumentException.ThrowIfNullOrEmpty(username);
    //    ArgumentException.ThrowIfNullOrEmpty(email);

    //    return userId is not Id<User> uid
    //        ? Db.Users.AnyAsync(x => EF.Functions.Like(x.Username, username) || (x.Email != null && EF.Functions.Like(x.Email, email)))
    //        : Db.Users.AnyAsync(x => (EF.Functions.Like(x.Username, username) || (x.Email != null && EF.Functions.Like(x.Email, email))) && x.Id != uid);
    //}

    public async Task<bool> AddUser(User user)
    {
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return true;
    }

    public Task<User?> CheckLogin(LoginRequest request)
    {
        var hash512 = HashHelpers.GetSHA512(request.PasswordSha256);
        return Db.Users.FirstOrDefaultAsync(
            x => x.Email != null && (EF.Functions.Like(x.Email, request.UsernameOrEmail) || EF.Functions.Like(x.Username, request.UsernameOrEmail))
                && x.PasswordSha512 != null
                && x.PasswordSha512 == hash512
        );
    }

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
