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
            var client = new HttpClient(new HttpCachingHandler())
            {
                BaseAddress = new Uri("http://localhost:5099")
            };

            var sessions = sp.GetRequiredService<SessionManager>();
            client.DefaultRequestHeaders.Authorization = sessions.CurrentUser?.Header;

            return client;
        });

        sc.AddScoped<IStorageManager, StubStorageManager>();

        sc.AddSingleton<IRESTObjectSerializer<ResponseCode>>(new JsonRESTSerializer<ResponseCode>());
        sc.AddSingleton<RESTObjectTypeTable<ResponseCode>>(new APIResponseTypeTable());

        sc.AddSingleton(typeof(SessionManager));

        Services = sc.BuildServiceProvider();
        ApiHelper.Services = Services;
    }

    [TestMethod]
    public async Task LogoutTest()
    {
        await Manager.Logout();
    }

    [TestMethod]
    public async Task NewUserTest()
    {
        var username = $"TestUser-{Guid.NewGuid()}";
        await Manager.CreateNewUserAndLogin(new NewUserRequest(username, $"{Guid.NewGuid()}@gmail.com", HashHelpers.GetSHA256("123456789")));
        Debug.Assert(Manager.CurrentUser?.Username == username);
    }
}
