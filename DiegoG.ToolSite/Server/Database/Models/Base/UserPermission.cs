namespace DiegoG.ToolSite.Server.Database.Models.Base;

[Flags]
public enum UserPermission : long
{
    AccessCalendar = 1 << 0,
    AccessLedger = 1 << 1
}
