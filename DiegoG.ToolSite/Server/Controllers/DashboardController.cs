using Microsoft.AspNetCore.Mvc;
using DiegoG.ToolSite.Server.Filters;

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
