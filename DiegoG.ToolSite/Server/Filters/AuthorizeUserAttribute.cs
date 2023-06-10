using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using DiegoG.REST;
using DiegoG.REST.ASPNET;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using DiegoG.ToolSite.Server.Attributes;
using DiegoG.ToolSite.Server.Database.Models.Base;
using DiegoG.ToolSite.Server.Services;
using DiegoG.ToolSite.Server.Types;
using DiegoG.ToolSite.Shared.Models.Responses;

namespace DiegoG.ToolSite.Server.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class AuthorizeUserAttribute : TypeFilterAttribute
{
    public AuthorizeUserAttribute(UserPermission userPermissions = 0) : base(typeof(UserAuthorizationFilter))
    {
        Arguments = new object[] { userPermissions };
    }
}

public class UserAuthorizationFilter : ToolSiteFilter, IAsyncAuthorizationFilter
{
    private readonly UserPermission UserPermissions;
    private readonly SessionAuthenticationFilter TokenAuthFilter;
    private readonly UserManager Users;

    public UserAuthorizationFilter(UserPermission userPermissions, SessionStore store, UserManager users, IRESTObjectSerializer<ResponseCode> serializer)
    {
        Users = users;
        TokenAuthFilter = new SessionAuthenticationFilter(users, store, serializer);
        UserPermissions = userPermissions;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        await TokenAuthFilter.OnAuthorizationAsync(context);
        if (context.Result is not null) return;

        var log = CreateLogger(context.HttpContext);
        var user = context.HttpContext.Features.Get<User>();
        Debug.Assert(user is not null); // Este valor no puede ser nulo si context.Result no lo es; para llegar a este punto el filtro de autenticacion tuvo que haber logrado algo
                                        // Es decir, tuvo que haber encontrado el usuario y por tanto tener un token, o context.Result NO es nulo y no deberia llegar hasta aca

        if (UserPermissions > 0)
        {
            log.Debug("Attempting to authorize user {user} ({userid}) for user permissions: {permissions}", user.Username, user.Id, UserPermissions);

            var perms = await Users.FetchRolePermissions(user.Id);
            if (perms.HasFlag(UserPermissions) is false)
            {
                log.Debug("The user {user} ({userid}) did not have the required user permissions of {permissions} and was not authorized", user.Username, user.Id, UserPermissions);
                context.Result = new ErrorResponse("The user does not have the required permissions to be authorized for this resource") 
                {
                    TraceId = context.HttpContext.TraceIdentifier 
                }
                .ToResult(HttpStatusCode.Forbidden);

                return;
            }

            log.Debug("The user {user} ({userid}) was succesfully authorized for permissions {permissions}", user.Username, user.Id, UserPermissions);
        }
    }
}
