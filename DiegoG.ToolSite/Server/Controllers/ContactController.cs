using System.Net;
using DiegoG.ToolSite.Server.Filters;
using DiegoG.ToolSite.Shared.Types;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DiegoG.ToolSite.Server.Controllers;

/// <summary>
/// Manages messages meant to contact the administrators of ToolSite
/// </summary>
[ApiController]
[Route("api/contact")]
[AuthenticateSession]
public class ContactController : ToolSiteAuthenticatedController
{
    /// <summary>
    /// Posts a new contact message for the site's admins to receive
    /// </summary>
    /// <param name="request">The details regarding the message</param>
    [HttpPost]

    [SwaggerOperation("Posts a message to be received by the site administrators")]

    [SwaggerResponse(200, "The message was succesfully submitted", typeof(NoResultsResponse))]

    [SwaggerResponse(403, "The user is not authorized for this resource", typeof(ErrorResponse))]
    [SwaggerResponse(400, "The request is empty, wrongly formatted, or otherwise invalid", typeof(ErrorResponse))]
    [SwaggerResponse(500, "An internal error ocurred in the server", typeof(ErrorResponse))]
    [SwaggerResponse(503, "Too many requests have been made to the server in a short period of time", typeof(TooManyRequestsResponse))]
    [SwaggerResponse(401, "No valid session id was present in the Authorization header of the request", typeof(ErrorResponse))]
    public async Task<IActionResult> PostNewMessage([FromBody] ContactMessageRequest request)
    {
        ErrorList errors = new();
        Log.Debug("Validating a new ContactMessageRequest");

        if (string.IsNullOrWhiteSpace(request.Message))
            errors.AddError("The request's message property is not set, empty or contains only whitespace");
        else if (request.Message.Length > 500)
            errors.AddError("The request's message is too long");

        if (request.ResponseMedium?.Length is > 100)
            errors.AddError("The request's responsemedium property is too long");

        if (request.ResponseAddress?.Length is > 100)
            errors.AddError("The request's responseaddress property is too long");

        if (errors.HasErrors)
        {
            Log.Debug("The request did not pass validation and has been declined");
            return BadRequest(new ErrorResponse(errors.AsEnumerable()) { TraceId = HttpContext.TraceIdentifier });
        }

        Log.Debug("The request has been validated, adding to DB");
        await Db.ContactMessages.AddAsync(new PendingContactMessage()
        {
            Id = Id<PendingContactMessage>.New(),
            Message = request.Message,
            ResponseAddress = request.ResponseAddress,
            ResponseMedium = request.ResponseMedium
        });

        Log.Verbose("Added new pending contact message, committing changes");
        await Db.SaveChangesAsync();

        Log.Information("Succesfully added a new pending contact message from user {userid}", SiteUserId);
        return Ok(NoResultsResponse.Instance);
    }
}
