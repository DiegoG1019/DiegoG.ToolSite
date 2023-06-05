using System.Diagnostics.Tracing;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MailKit;
using MailKit.Net.Proxy;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DiegoG.ToolSite.Server.Services;

public static class SmtpProvider
{
    public enum AuthKind
    {
        None = -1,
        Basic = 0
    }

    private readonly record struct HostData(string Host, int Port, bool UseSsl = true);
    private readonly record struct FromData(string Name, string Address);

    public static async ValueTask<(ISmtpClient Client, InternetAddress ClientAddress)> GetSmtpClient(string mailUser, CancellationToken ct = default)
    {
        var client = new SmtpClient();
        var section = ServerProgram.App.Configuration.GetSection("Email").GetSection(mailUser)
            ?? throw new ArgumentException("Couldn't find a mail configuration for the given user", nameof(mailUser));

        var hd = section.GetRequiredSection("Host").Get<HostData>();
        await client.ConnectAsync(hd.Host, hd.Port, hd.UseSsl, ct);

        var kind = section.GetValue<AuthKind>("AuthKind");
        await (kind switch
        {
            AuthKind.Basic => client.AuthenticateAsync(section.GetRequiredSection("Credentials").Get<NetworkCredential>(), ct),
            _ => throw new InvalidDataException($"AuthKind for user {mailUser} is not set or is invalid")
        });

        var addr = section.GetRequiredSection("From").Get<FromData>();

        return (client, new MailboxAddress(addr.Name, addr.Address));
    }
}
