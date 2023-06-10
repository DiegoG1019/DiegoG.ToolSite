using System.Collections.Concurrent;
using System.Net;
using DiegoG.REST.ASPNET;
using DiegoG.REST.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using MimeKit.Cryptography;
using DiegoG.ToolSite.Server.Database.Models.Base;
using DiegoG.ToolSite.Server.Filters;
using DiegoG.ToolSite.Server.Models;
using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Server;

public static class HttpHelpers
{
    static HttpHelpers()
    {
        JsonMediaTypes = new();
        foreach (var mediaType in JsonRESTSerializer<ResponseCode>.JsonMIME)
            JsonMediaTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(mediaType, 1));
    }

    public static string? FindIPAddress(this HttpContext httpContext)
    {
        return httpContext.Request.Host.Value;
    }

    private readonly static Func<TimeSpan, CookieOptions> CookieOptionGen = ts => new CookieOptions()
    {
        Expires = DateTimeOffset.Now + ts,
        SameSite = SameSiteMode.Strict,
        Secure = true
    };

    public static bool CheckIfAlreadyLoggedIn(User user, ILogger? log = null)
    {
        ArgumentNullException.ThrowIfNull(user);
        log?.Debug("Checking if user is already logged in");
        if (user.PasswordSha512 is not null)
        {
            log?.Information("User {user} ({userid}) is already logged in");
            return true;
        }
        return false;
    }

    public static RESTObjectResult<ResponseCode> ToResult(this APIResponse response, HttpStatusCode code)
        => response.ToResult((int)code);

    private static readonly MediaTypeCollection JsonMediaTypes;
    public static RESTObjectResult<ResponseCode> ToResult(this APIResponse response, int httpStatusCode)
        => new RESTObjectResult<ResponseCode>(response)
        {
            ContentTypes = JsonMediaTypes,
            StatusCode = httpStatusCode
        };
}
