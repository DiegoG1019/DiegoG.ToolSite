using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiegoG.REST.Json;
using DiegoG.REST;
using DiegoG.ToolSite.Client;
using DiegoG.ToolSite.Client.Services;
using DiegoG.ToolSite.Client.Types;
using DiegoG.ToolSite.Shared;
using DiegoG.ToolSite.Shared.Models.Requests;
using DiegoG.ToolSite.Shared.Models.Responses.Base;
using DiegoG.ToolSite.Shared.Models.Responses;
using DiegoG.ToolSite.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using DiegoG.ToolSite.Shared.Models;

namespace ToolSite.Tests;

[TestClass]
public class SessionManagerTest
{
    private IServiceProvider Services { get; }
    private SessionManager? _manager;
    public SessionManager Manager => _manager ??= Services.GetRequiredService<SessionManager>();

    public SessionManagerTest()
    {
        var sc = new ServiceCollection();

        sc.AddScoped(sp =>
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri("https://localhost:7273")
            };

            var sessions = sp.GetRequiredService<SessionManager>();

            if (sessions.CurrentUser?.Session is SessionId sid)
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {sid}");

            return client;
        });

        sc.AddScoped<IStorageManager, StubStorageManager>();

        sc.AddSingleton<IRESTObjectSerializer<ResponseCode>>(new JsonRESTSerializer<ResponseCode>(SharedStatic.JsonOptions));
        sc.AddSingleton<RESTObjectTypeTable<ResponseCode>>(new APIResponseTypeTable());

        sc.AddSingleton(typeof(SessionManager));

        Services = sc.BuildServiceProvider();
        ApiHelper.Services = Services;
    }

    [TestMethod]
    public async Task NewUserTest()
    {
        const string charpool = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        StringBuilder sb = new StringBuilder(30).Append("TestUser_");
        for (int i = 0; i < 30 - 9; i++)
            sb.Append(charpool[Random.Shared.Next(0, charpool.Length - 1)]);

        var username = sb.ToString();

        var (s, e) = await Manager.CreateNewUserAndLogin(new NewUserRequest(username, $"{Guid.NewGuid()}@gmail.com", HashHelpers.GetSHA256("123456789")));

        if (s is false)
        {
            foreach (var error in e!)
                Console.WriteLine($"- {error}");
            Debug.Fail("The request completed with errors");
        }
        Debug.Assert(Manager.CurrentUser?.Username == username, "Logged in User's Username is different from what was expected");

        Debug.Assert(await Manager.Logout(), "The user was unexpectedly not logged in");
    }
}
