using DiegoG.ToolSite.Shared.Models;

namespace DiegoG.ToolSite.Server.Services;

public static class ServiceItemDescriptionStore
{
    public static ServiceItemDescription CalendarService { get; } = new()
    {
        BigIcon = "",
        SmallIcon = "",
        Description = "A Calendar you can attach stickers to to conmemorate important events or goals!",
        Title = "Sticker Calendar",
        Uri = "/calendar"
    };

    public static ServiceItemDescription LedgerService { get; } = new()
    {
        BigIcon = "",
        SmallIcon = "",
        Description = "Keep track of your finances using this handy tool!",
        Title = "Ledger",
        Uri = "/ledger"
    };
}
