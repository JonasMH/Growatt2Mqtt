using System.Net.Sockets;

namespace GrowattShine2Mqtt;

public class GrowattServerListener : IHostedService
{
    private readonly ILogger<GrowattServerListener> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly GrowattMetrics _metrics;
    private List<GrowattSocketHandler> _sockets = new List<GrowattSocketHandler>();

    public GrowattServerListener(
    ILogger<GrowattServerListener> logger,
    IServiceProvider serviceProvider,
    GrowattMetrics metrics)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _metrics = metrics;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var tcpListener = new TcpListener(System.Net.IPAddress.Parse("0.0.0.0"), 5279);
        tcpListener.Start();


        var listenTask = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    var socket = await tcpListener.AcceptSocketAsync();
                    _logger.LogInformation("Got connection from {endpoint}", socket.RemoteEndPoint);
                    _metrics?.NewConnection();
                    var growattSocket = ActivatorUtilities.CreateInstance<GrowattSocketHandler>(_serviceProvider, socket);
                    _sockets.Add(growattSocket);
                    var _ = growattSocket.RunAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to setup socket");
                }
            }
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
