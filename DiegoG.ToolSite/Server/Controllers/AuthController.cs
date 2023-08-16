using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using DiegoG.ToolSite.Server.Database.Models.Base;
using DiegoG.ToolSite.Server.Filters;
using Swashbuckle.AspNetCore.Annotations;
using DiegoG.ToolSite.Shared;
using System.Text.RegularExpressions;
using System.Net.Mail;
using DiegoG.ToolSite.Shared.Types;

namespace DiegoG.ToolSite.Server.Controllers;

/// <summary>
/// Manages and controls authentication and user sessions
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ToolSiteController
{
    private readonly UserManager Users;
    private readonly SessionStore SessionStore;

    /// <summary>
    /// Creates a new instance of this controller
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public AuthController(UserManager users, SessionStore sessionStore)
    {
        Users = users ?? throw new ArgumentNullException(nameof(users));
        SessionStore = sessionStore;
    }

    /// <summary>
    /// Fetches session information about the currently logged in user
    /// </summary>
    /// <returns></returns>
    [AuthenticateSession]
    [HttpGet]

    [SwaggerOperation("Fetches the current user's session information")]

    [SwaggerResponse(200, "Succesfully requested the current user's session information", typeof(SessionInformationResponse))]

    [SwaggerResponse(403, "The user is not authorized for this resource", typeof(ErrorResponse))]
    [SwaggerResponse(400, "The request is empty, wrongly formatted, or otherwise invalid", typeof(ErrorResponse))]
    [SwaggerResponse(500, "An internal error ocurred in the server", typeof(ErrorResponse))]
    [SwaggerResponse(503, "Too many requests have been made to the server in a short period of time", typeof(TooManyRequestsResponse))]
    [SwaggerResponse(401, "No valid session id was present in the Authorization header of the request", typeof(ErrorResponse))]
    public async Task<IActionResult> FetchSessionInfo()
    {
        var user = await Users.FetchOrFindUser(HttpContext.Features.Get<Id<User>>());
        var session = HttpContext.Features.Get<Session>()!;
        Log.Debug("Fetched Session information");
        return Ok(new SessionInformationResponse()
        {
            LoggedInAs = user.Username,
            LoggedInSince = session.Created,
            IsAnonymous = user.PasswordSha512 is null,
            SessionId = session.Id,
            Permissions = await Users.FetchRolePermissions(user.Id),
            Settings = ToResult(user.UserSettings)
        });
    }

    private static UserSettings? ToResult(UserSettingsDbModel? model)
        => new();

    /// <summary>
    /// Destroys the session used by the current user in this request, effectively logging out the user of the session
    /// </summary>
    /// <param name="store"></param>
    /// <returns></returns>
    [AuthenticateSession]
    [HttpDelete]

    [SwaggerOperation("Destroys the session of the current user, so it cannot be used thereafter")]

    [SwaggerResponse(200, "Succesfully destroyed the session", typeof(NoResultsResponse))]

    [SwaggerResponse(403, "The user is not authorized for this resource", typeof(ErrorResponse))]
    [SwaggerResponse(400, "The request is empty, wrongly formatted, or otherwise invalid", typeof(ErrorResponse))]
    [SwaggerResponse(500, "An internal error ocurred in the server", typeof(ErrorResponse))]
    [SwaggerResponse(503, "Too many requests have been made to the server in a short period of time", typeof(TooManyRequestsResponse))]
    [SwaggerResponse(401, "No valid session id was present in the Authorization header of the request", typeof(ErrorResponse))]
    public IActionResult Logout([FromServices] SessionStore store)
    {
        var session = HttpContext.Features.Get<Session>()!;
        Log.Information("Logging out user");
        Log.Debug("Destroying Session of id {sessionid}", session.Id);
        store.DestroySession(session.Id);
        return Ok(NoResultsResponse.Instance);
    }

    /// <summary>
    /// Validates the provided login information and creates a new session for the user
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]

    [SwaggerOperation("Logins as an user and creates a new session for them")]

    [SwaggerResponse(200, "Succesfully logged in as the requested user and created a new session", typeof(SuccesfulLoginResponse))]
    [SwaggerResponse(403, "The request was made using unverifiable credentials", typeof(ErrorResponse))]

    [SwaggerResponse(400, "The request is empty, wrongly formatted, or otherwise invalid", typeof(ErrorResponse))]
    [SwaggerResponse(500, "An internal error ocurred in the server", typeof(ErrorResponse))]
    [SwaggerResponse(503, "Too many requests have been made to the server in a short period of time", typeof(TooManyRequestsResponse))]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        ErrorList errors = new();

        if (ModelState.IsValid is false || request is null)
        {
            errors.AddError("The request body is invalid or is null");
            Log.Debug("Bad request for new login");
            return BadRequest(new ErrorResponse(errors.AsEnumerable()) { TraceId = HttpContext.TraceIdentifier });
        }

        Log.Verbose("Verifying if login request for {username} is valid", request.UsernameOrEmail);
        var attemptingUser = await Users.CheckLogin(request);

        if (attemptingUser is null)
        {
            errors.AddError("Could not find an user with the given credentials");
            Log.Debug("Could not verify user credentials for {username}", request.UsernameOrEmail);
            return new ObjectResult(new ErrorResponse(errors.AsEnumerable()) { TraceId = HttpContext.TraceIdentifier })
            {
                StatusCode = (int)HttpStatusCode.Forbidden
            };
        }
        
        Debug.Assert(attemptingUser is not null);

        Log.Debug("Creating new session for user {user} ({id})", attemptingUser.Username, attemptingUser.Id);
        var session = Session.New(attemptingUser, HttpContext);
        SessionStore.AddSession(session);

        Log.Information("Succesfully processed a login request for user {user} ({id})", attemptingUser.Username, attemptingUser.Id);
        return Ok(new SuccesfulLoginResponse()
        {
            LoggedInAs = attemptingUser.Username,
            SessionId = session.Id
        });
    }

    /// <summary>
    /// Verifies and validates the request to create a new user, and creates a new session to automatically log them in
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("new")]

    [SwaggerOperation("Creates a brand new user")]

    [SwaggerResponse(200, "The new user was succesfully created and logged in", typeof(SuccesfulLoginResponse))]

    [SwaggerResponse(400, "The request is empty, wrongly formatted, or otherwise invalid", typeof(ErrorResponse))]
    [SwaggerResponse(500, "An internal error ocurred in the server", typeof(ErrorResponse))]
    [SwaggerResponse(503, "Too many requests have been made to the server in a short period of time", typeof(TooManyRequestsResponse))]
    public async Task<IActionResult> CreateNewUser([FromBody] NewUserRequest request)
    {
        ErrorList errors = new();

        if (ModelState.IsValid is false || request is null)
        {
            errors.AddError("The request body is invalid or is null");
            Log.Debug("Bad request for new user");
            return BadRequest(new ErrorResponse(errors.AsEnumerable()) { TraceId = HttpContext.TraceIdentifier });
        }

        Log.Debug("Verifying the details of the new user request");
        if (string.IsNullOrWhiteSpace(request.PasswordSha256) || request.PasswordSha256.Length != HashHelpers.SHA256HexStringLength)
        {
            Log.Verbose("New User Request's password is invalid");
            errors.AddError($"The request's password property was not present, was null or empty, or did not have a length of {HashHelpers.SHA256HexStringLength}");
        }
        
        if (RegexHelpers.VerifyHexStringRegex(RegexHelpers.HexStringVerificationOptions.Uppercase).IsMatch(request.PasswordSha256) is false)
            errors.AddError("The request's password property is not a purely uppercase hexadecimal string. Note that a trailing 0x is not allowed.");

        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length > 30)
        {
            Log.Verbose("New User Request's username is too long");
            errors.AddError("The request's username property is not set, empty, or is longer than 30 characters");
        }
        else if (RegexHelpers.VerifyAlphaNumericRegex().IsMatch(request.Username) is false)
        {
            Log.Verbose("New User Request's username is invalid: {username}", request.Username);
            errors.AddError("An username may only contain alphanumeric characters and a '_' character. That is, A-Z lowercase or uppercase, 0-9 and '_'");
        }
        else if (await Users.CheckForUsernameConflict(request.Username, null))
        {
            Log.Verbose("New User Request's is already being used");
            errors.AddError($"The username '{request.Username}' is already being used");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || MailAddress.TryCreate(request.Email, out _) is false)
        {
            Log.Verbose("New User Request's email is invalid");
            errors.AddError("The request's email property is not set, or does not represent a valid email address");
        }
        else if (await Users.CheckForEmailConflict(request.Email, null))
        {
            Log.Verbose("New User Request's email is already being used");
            errors.AddError($"The email address '{request.Email}' is already being used");
        }

        if (errors.Errors?.Count is > 0)
        {
            Log.Debug("Rejected NewUser request due to validation errors");
            return BadRequest(new ErrorResponse(errors.AsEnumerable()) { TraceId = HttpContext.TraceIdentifier });
        }

        var newuser = new User()
        {
            Id = Id<User>.New(),
            CreationDate = DateTimeOffset.Now,
            Email = request.Email,
            PasswordSha512 = HashHelpers.GetSHA512(request.PasswordSha256),
            Username = request.Username
        };

        Log.Debug("Creating new user {user} with {id}", newuser.Username, newuser.Id);
        if (await Users.AddUser(newuser))
            Log.Information("Succesfully created new user {user} ({userid}) with an email of {email}", newuser.Username, newuser.Id, newuser.Email);

        Log.Debug("Creating a new session for user {user} ({userid})", newuser.Username, newuser.Id);
        var session = Session.New(newuser, HttpContext);
        SessionStore.AddSession(session);

        Log.Information(
            "Succesfully processed a new user request for user {user} ({id}), and created session {sessionid} for them", 
            newuser.Username, 
            newuser.Id,
            session.Id
        );

        return Ok(new SuccesfulLoginResponse()
        {
            LoggedInAs = newuser.Username,
            SessionId = session.Id
        });
    }
}
