using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using GrowattShine2Mqtt;

namespace GrowattShine2MqttTests.Integration;
public class GrowattWebApplicationFactory : WebApplicationFactory<Program>
{
    public int GrowattPort = NetworkUtils.GetFreeTcpPort();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.Configure<GrowattServerOptions>(x =>
            {
                x.Port = GrowattPort;
            });
        });

        base.ConfigureWebHost(builder);
    }
}

public static class NetworkUtils
{
    public static int GetFreeTcpPort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
