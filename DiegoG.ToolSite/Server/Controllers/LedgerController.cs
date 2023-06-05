using System.Runtime.Loader;
using Microsoft.AspNetCore.Mvc;
using DiegoG.ToolSite.Server.Filters;
using DiegoG.ToolSite.Shared.Models.Responses;

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
    public async Task<IActionResult> GetLedgerPage([FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] DateTimeOffset? start, [FromQuery] DateTimeOffset? end)
    {
        var query = Ledger.FetchItems(
            start ?? DateTimeOffset.Now,
            end ?? DateTimeOffset.Now + TimeSpan.FromDays(-DateTimeOffset.Now.Day + 1),
            page ?? 0,
            pageSize ?? 100,
            SiteUserId
        );

        List<IdentifiedItem<LedgerEntryItem>> lei = new();

        await foreach (var entry in query.AsAsyncEnumerable())
            lei.Add(new(entry.Id.Identification, new()
            {
                Money = entry.Money,
                Message = entry.Message,
                Date = entry.Date,
                Category = entry.Category,
                Recipient = entry.Recipient,
                Tags = entry.Tags.Select(x => x.Label).ToList()
            }));

        return lei.Count is 0
            ? NotFound(new LedgerPageResponse(lei))
            : Ok(new LedgerPageResponse(lei));
    }

    [HttpPut]
    public async Task<IActionResult> SubmitLedgerChanges([FromBody] LedgerChangesRequest request)
    {
        if (request.Changes?.Length is not > 0 && request.NewEntries?.Length is not > 0)
            return BadRequest(new ErrorResponse("The body of the request must contain at least one Entry"));

        PutResult[]? modresults = null;
        PutResult[]? addresults = null;

        if (request.Changes?.Length is > 0)
        {
            modresults = new PutResult[request.Changes.Length];

            var dict = await Ledger.FetchItems(request.Changes.Select(x => new Id<LedgerEntry>(x.TargetId)), SiteUserId)
                .ToDictionaryAsync(x => x.Id.Identification, x => x);

            for (int i = 0; i < request.Changes.Length; i++)
            {
                IdentifiedItem<LedgerEntryItem> change = request.Changes[i];
                if (dict.TryGetValue(change.TargetId, out var entry) is false)
                {
                    modresults[i] = new(i, false, $"Could not find a LedgerEntry under Id '{change.TargetId}' that was accesible by the user");
                    continue;
                }

                foreach (var tag in change.Item.Tags)
                    entry.Tags.Add(new Tag<LedgerEntry>() { OwnerId = entry.Id, Label = tag });

                entry.Money = change.Item.Money;
                entry.Message = change.Item.Message;
                entry.Category = change.Item.Category;
                entry.Recipient = change.Item.Recipient;
                entry.Date = change.Item.Date;

                modresults[i] = new(i, true);
            }
        }

        if (request.NewEntries?.Length is > 0)
        {
            addresults = new PutResult[request.NewEntries.Length];

            for (int i = 0; i < request.NewEntries.Length; i++)
            {
                LedgerEntryItem? items = request.NewEntries[i];
                Ledger.AddItem(items, SiteUserId);
                addresults[i] = new(i, true);
            }
        }

        return Ok(new LedgerInsertionResponse()
        {
            Additions = addresults ?? Array.Empty<PutResult>(),
            Modifications = modresults ?? Array.Empty<PutResult>()
        });
    }
}
