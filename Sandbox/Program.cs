using ToolSite.Tests;

namespace Sandbox;

internal class Program
{
    static async Task Main(string[] args)
    {
        var x = new SessionManagerTest();
        await x.NewUserTest();
    }
}
