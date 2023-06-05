using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using DiegoG.ToolSite.Server.Database.Models.Base;
using DiegoG.ToolSite.Server.Filters;

namespace DiegoG.ToolSite.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ToolSiteAuthenticatedController
{
    private readonly UserManager Users;

    public AuthController(UserManager users)
    {
        Users = users ?? throw new ArgumentNullException(nameof(users));
    }

    [AuthenticateSession]
    [HttpGet("fetch")]
    public async Task<IActionResult> FetchSessionInfo()
    {
        var user = await FetchSiteUser();
        return Ok(new SessionInformationResponse()
        {
            LoggedInAs = user.DisplayName,
            LoggedInSince = Session.Created,
            IsAnonymous = user.PasswordHash is null,
            SessionId = Session.Id
        });
    }

    [AuthenticateSession]
    [HttpPost("logout")]
    public IActionResult Logout([FromServices] SessionStore store)
    {
        Log.Information("Logging out user");
        Log.Debug("Destroying Session of id {sessionid}", Session.Id);
        store.DestroySession(Session.Id);
        return Ok(NoResultsResponse.Instance);
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        ErrorList errors = new();

        if (ModelState.IsValid is false || request is null)
        {
            errors.AddError("The request body is invalid or is null");
            Log.Debug("Bad request for new login");
            return BadRequest(new ErrorResponse(errors.AsEnumerable()));
        }

        Log.Verbose("Verifying if login request for {username} is valid", request.Username);
        var attemptingUser = await Users.CheckLogin(request);

        if (attemptingUser is null)
        {
            errors.AddError("Could not find an user with the given credentials");
            Log.Debug("Could not verify user credentials for {username}", request.Username);
            return new ObjectResult(new ErrorResponse(errors.AsEnumerable()))
            {
                StatusCode = (int)HttpStatusCode.Forbidden
            };
        }

        Debug.Assert(attemptingUser is not null);

        Log.Debug("Creating new session for user {user} ({id})", attemptingUser.DisplayName, attemptingUser.Id);
        var session = Session.New(attemptingUser, HttpContext);

        Log.Information("Succesfully processed a login request for user {user} ({id})", attemptingUser.DisplayName, attemptingUser.Id);
        return Ok(new SuccesfulLoginResponse()
        {
            LoggedInAs = attemptingUser.DisplayName,
            SessionId = session.Id
        });
    }
}
