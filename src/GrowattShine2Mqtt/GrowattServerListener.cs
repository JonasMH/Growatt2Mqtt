using System.Net.Sockets;

namespace GrowattShine2Mqtt;

public interface IGrowattServerListener
{
    List<GrowattSocketHandler> Sockets { get; }
}

public class GrowattServerListener : IHostedService, IGrowattServerListener
{
    private readonly ILogger<GrowattServerListener> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IGrowattMetrics _metrics;
    public List<GrowattSocketHandler> Sockets { get; } = new List<GrowattSocketHandler>();

    public GrowattServerListener(
        ILogger<GrowattServerListener> logger,
        IServiceProvider serviceProvider,
        IGrowattMetrics metrics)
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
                    var growattSocket = ActivatorUtilities.CreateInstance<GrowattSocketHandler>(_serviceProvider, (IGrowattSocket)(new GrowattSocket(socket)));
                    Sockets.Add(growattSocket);
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
