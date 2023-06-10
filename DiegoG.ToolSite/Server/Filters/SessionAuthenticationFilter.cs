using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using DiegoG.REST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Serilog;
using DiegoG.ToolSite.Server;
using DiegoG.ToolSite.Server.Attributes;
using DiegoG.ToolSite.Server.Database.Models.Base;
using DiegoG.ToolSite.Server.Models;
using DiegoG.ToolSite.Server.Services;
using DiegoG.ToolSite.Server.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DiegoG.ToolSite.Server.Filters;

public class AuthenticateSessionAttribute : ServiceFilterAttribute
{
    public AuthenticateSessionAttribute() : base(typeof(SessionAuthenticationFilter)) { }
}

public class SessionAuthenticationFilter : ToolSiteFilter, IAsyncAuthorizationFilter
{
    public const string SessionIdCookie = "session-id";
    private readonly UserManager Users;
    private readonly SessionStore SessionStore;
    private readonly IRESTObjectSerializer<ResponseCode> RESTSerializer;

    public SessionAuthenticationFilter(UserManager users, SessionStore store, IRESTObjectSerializer<ResponseCode> serializer)
    {
        Users = users;
        RESTSerializer = serializer;
        SessionStore = store;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        var log = CreateLogger(httpContext);

        if (httpContext.Features.Get<User>() is not null || context.Result is not null)
            return;

        User? user;

        log.Verbose("Reading user headers to authenticate");
        if (httpContext.Request.Headers.Authorization.Count > 1)
        {
            log.Verbose("The request contains more than one authorization header, declining");
            context.Result = new ObjectResult(
                new ErrorResponse("The request contains more than a single authorization header, which is not allowed when verifying headers")
                { TraceId = httpContext.TraceIdentifier }
            )
            { StatusCode = (int)HttpStatusCode.Unauthorized };
            return;
        }

        var x = httpContext.Request.Headers.Authorization;
        if (SessionId.TryParse("", out var sid))
            log.Verbose("Found session id in authorization header");

        if (sid != default && SessionStore.TryGetSession(sid, out var session)) 
        {
            log.Verbose("Succesfully parsed session id {sessionid}", sid);

            user = await Users.FindUser(session.UserId);
            if (user is null)
            {
                log.Verbose("No user was found associated to session id {sessionid}", sid);
                context.Result = new ObjectResult(
                    new ErrorResponse("No session was found that matched the session id in the request")
                    { TraceId = httpContext.TraceIdentifier }
                )
                { StatusCode = (int)HttpStatusCode.Unauthorized };
                return;
            }
            else
                log.Debug("Succesfully authenticated user {user} ({userid}) under session {sessionid}", user.Username, user.Id, sid);
        }
        else
        {
            log.Verbose("No session id was found in the request");
            context.Result = new ObjectResult(
                new ErrorResponse("No valid session id was found in the request")
                { TraceId = httpContext.TraceIdentifier }
            )
            { StatusCode = (int)HttpStatusCode.Unauthorized };
            return;
        }

        Debug.Assert(user is not null);
        Debug.Assert(session is not null);
        httpContext.Features.Set(user);
        httpContext.Features.Set(session);
    }
}
