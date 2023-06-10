namespace DiegoG.ToolSite.Shared.Models;

[Flags]
public enum UserPermission : long
{
    AccessCalendar = 1 << 0,
    AccessLedger = 1 << 1
}
