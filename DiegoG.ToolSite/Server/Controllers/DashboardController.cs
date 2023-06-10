using Microsoft.AspNetCore.Mvc;
using DiegoG.ToolSite.Server.Filters;
using Swashbuckle.AspNetCore.Annotations;

namespace DiegoG.ToolSite.Server.Controllers;

[ApiController]
[AuthenticateSession]
[Route("api/dashboard")]
public class DashboardController : ToolSiteAuthenticatedController
{
    private readonly UserManager Users;

    public DashboardController(UserManager users)
    {
        Users = users;
    }

    [HttpGet()]
    [ResponseCache(Duration = 5 * 60)]

    [SwaggerOperation("Fetches the current user's dashboard information")]

    [SwaggerResponse(200, "Succesfully requested the user's dashboard information", typeof(DashboardItemsResponse))]

    [SwaggerResponse(403, "The user is not authorized for this resource", typeof(ErrorResponse))]
    [SwaggerResponse(400, "The request is empty, wrongly formatted, or otherwise invalid", typeof(ErrorResponse))]
    [SwaggerResponse(500, "An internal error ocurred in the server", typeof(ErrorResponse))]
    [SwaggerResponse(503, "Too many requests have been made to the server in a short period of time", typeof(TooManyRequestsResponse))]
    [SwaggerResponse(401, "No valid session id was present in the Authorization header of the request", typeof(ErrorResponse))]
    public async Task<IActionResult> GetDashboardItems()
    {
        return Ok(
            new DashboardItemsResponse()
            {
                ServiceItems = await Users.FetchUserServices(SiteUserId)
            }
        );
    }
}
