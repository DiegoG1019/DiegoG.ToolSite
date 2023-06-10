using System.Runtime.Loader;
using Microsoft.AspNetCore.Mvc;
using DiegoG.ToolSite.Server.Filters;
using DiegoG.ToolSite.Shared.Models.Responses;
using System.Linq;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace DiegoG.ToolSite.Server.Controllers;

[ApiController]
[Route("api/app/ledger")]
[AuthorizeUser(UserPermission.AccessLedger)]
public class LedgerController : ToolSiteAuthenticatedController
{
    private readonly LedgerManager Ledger;

    public LedgerController(LedgerManager ledger)
    {
        Ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
    }

    [HttpGet]

    [SwaggerOperation("Fetches a Page from the Current User's Ledger")]

    [SwaggerResponse(404, "No entries were found that matched the query", typeof(LedgerPageResponse))]
    [SwaggerResponse(200, "Found at one or more entries that matched the query", typeof(LedgerPageResponse))]

    [SwaggerResponse(403, "The user is not authorized for this resource", typeof(ErrorResponse))]
    [SwaggerResponse(400, "The request is empty, wrongly formatted, or otherwise invalid", typeof(ErrorResponse))]
    [SwaggerResponse(500, "An internal error ocurred in the server", typeof(ErrorResponse))]
    [SwaggerResponse(503, "Too many requests have been made to the server in a short period of time", typeof(TooManyRequestsResponse))]
    [SwaggerResponse(401, "No valid session id was present in the Authorization header of the request", typeof(ErrorResponse))]
    public async Task<IActionResult> GetLedgerPage([FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] DateTimeOffset? dateStart, [FromQuery] DateTimeOffset? dateEnd)
    {
        var sta = dateStart  ?? DateTimeOffset.Now;
        var end = dateEnd ?? DateTimeOffset.Now + TimeSpan.FromDays(-DateTimeOffset.Now.Day + 1);
        var pgo = page ?? 0;
        var pgs = pageSize ?? 100;

        Log.Information("Fetching a Page from Ledger of size {size} at offset {page}, from {start} to {end}");

        var query = Ledger.FetchItems(
            sta,
            end,
            pgo,
            pgs,
            SiteUserId
        );

        Log.Debug("Preparing found ledger entries");
        List<IdentifiedItem<LedgerEntryItem>> lei = new();

        await foreach (var entry in query.AsAsyncEnumerable())
        {
            Log.Verbose("Including ledger entry of Id {id}", entry.Id);
            lei.Add(new(entry.Id.Identification, new()
            {
                Money = entry.Money.ToMoneyAmount(),
                Message = entry.Message,
                Date = entry.Date,
                Category = entry.Category,
                Recipient = entry.Recipient,
                Tags = entry.Tags.Select(x => x.Label).ToList()
            }));
        }

        Log.Information("Returning Ledger Page with {total} entries", lei.Count);

        return lei.Count is 0
            ? NotFound(new LedgerPageResponse(lei))
            : Ok(new LedgerPageResponse(lei));
    }

    [HttpPut]

    [SwaggerOperation("Puts changes made by the client in the server")]

    [SwaggerResponse(200, "Succesfully submitted and saved the changes", typeof(LedgerInsertionResponse))]

    [SwaggerResponse(403, "The user is not authorized for this resource", typeof(ErrorResponse))]
    [SwaggerResponse(400, "The request is empty, wrongly formatted, or otherwise invalid", typeof(ErrorResponse))]
    [SwaggerResponse(500, "An internal error ocurred in the server", typeof(ErrorResponse))]
    [SwaggerResponse(503, "Too many requests have been made to the server in a short period of time", typeof(TooManyRequestsResponse))]
    [SwaggerResponse(401, "No valid session id was present in the Authorization header of the request", typeof(ErrorResponse))]
    public async Task<IActionResult> SubmitLedgerChanges([FromBody] LedgerChangesRequest request)
    {
        Log.Debug("Submitting ledger changes");
        if (request.Changes?.Length is not > 0 && request.NewEntries?.Length is not > 0)
        {
            Log.Debug("Ledger changes were empty and were not processed");
            return BadRequest(new ErrorResponse("The body of the request must contain at least one Entry") { TraceId = HttpContext.TraceIdentifier });
        }

        PutResult[]? modresults = null;
        PutResult[]? addresults = null;
        int modsuccess = 0;
        int addsuccess = 0;

        if (request.Changes?.Length is > 0)
        {
            Log.Debug("Verifying {total} changes to ledger", request.Changes.Length);
            modresults = new PutResult[request.Changes.Length];

            Log.Verbose("Querying relevant items from database");
            var dict = await Ledger.FetchItems(request.Changes.Select(x => new Id<LedgerEntry>(x.TargetId)), SiteUserId)
                .ToDictionaryAsync(x => x.Id.Identification, x => x);

            for (int i = 0; i < request.Changes.Length; i++)
            {
                IdentifiedItem<LedgerEntryItem> change = request.Changes[i];
                Log.Verbose("Reviewing change #{i}, for entry {entryid}", i, change.TargetId);

                if (dict.TryGetValue(change.TargetId, out var entry) is false)
                {
                    Log.Debug("Could not find a LedgerEntry under Id '{TargetId}' that was accesible by the user in change #{i}", change.TargetId, i);
                    modresults[i] = new(i, false, $"Could not find a LedgerEntry under Id '{change.TargetId}' that was accesible by the user");
                    continue;
                }

                if (change.Item.Tags is not null)
                {
                    entry.Tags.Clear();
                    foreach (var tag in change.Item.Tags)
                        entry.Tags.Add(new Tag<LedgerEntry>() { OwnerId = entry.Id, Label = tag });
                }

                entry.Money = change.Item.Money?.ToNodaMoney() ?? entry.Money;
                entry.Message = change.Item.Message ?? entry.Message;
                entry.Category = change.Item.Category ?? entry.Category;
                entry.Recipient = change.Item.Recipient ?? entry.Recipient;
                entry.Date = change.Item.Date ?? entry.Date;

                modresults[i] = new(i, true);

                modsuccess++;
                Log.Verbose("Changed entry {entryid} by change #{i}", entry.Id, i);
            }

            Log.Debug("Verified {success} out of {total} changes to ledger", modsuccess, request.Changes.Length);
        }

        if (request.NewEntries?.Length is > 0)
        {
            Log.Debug("Making {total} additions to ledger", request.NewEntries.Length);
            addresults = new PutResult[request.NewEntries.Length];

            for (int i = 0; i < request.NewEntries.Length; i++)
            {
                var item = request.NewEntries[i];

                Log.Verbose("Verifying addition #{i}", i);

                if (item.Date is null || item.Money is null)
                {
                    addresults[i] = new(i, false, "Money and Date properties cannot be null");
                    Log.Debug("Could not make change #{i} because either the Date property, Money property or both were null", i);
                }

                var ne = Ledger.AddItem(item, SiteUserId);
                addresults[i] = new(i, true);
                addsuccess++;
                Log.Verbose("Made addition #{i} under LedgerEntry of Id {id}", i, ne.Id);
            }

            Log.Debug("Made {success} out of {total} additions to ledger", addsuccess, request.NewEntries.Length);
        }

        Log.Information("Committing {suchanges} out of {tochanges} changes and {suadd} out of {toadd} additions made to ledger", modsuccess, modresults?.Length ?? 0, addsuccess, addresults?.Length ?? 0);
        await Ledger.SaveChangesAsync();

        return Ok(new LedgerInsertionResponse()
        {
            Additions = addresults ?? Array.Empty<PutResult>(),
            Modifications = modresults ?? Array.Empty<PutResult>(),

            SuccesfulAdditions = addsuccess,
            SuccesfulModifications = modsuccess,

            RequestedAdditions = addresults?.Length ?? 0,
            RequestedModifications = modresults?.Length ?? 0,
        });
    }
}
