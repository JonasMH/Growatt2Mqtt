using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace GrowattShine2Mqtt;

public interface IGrowattServerListener
{
    IReadOnlyList<GrowattSocketHandler> Sockets { get; }
}

public class GrowattServerOptions
{
    public int Port { get; set; } = 5279;
    public string HostingAddress { get; set; } = "0.0.0.0";
}

public class GrowattSocketReference
{
    public int Id { get; set; }
    public Socket Socket { get; set; }
    public GrowattSocketHandler Handler { get; set; }
    public CancellationTokenSource HandlerCancellationToken { get; set; }
    public Task HandlerRunTask { get; set; }
}

public class GrowattServerListener : IHostedService, IGrowattServerListener
{
    private readonly ILogger<GrowattServerListener> _logger;
    private readonly GrowattServerOptions _growattServerOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IGrowattMetrics _metrics;
    private readonly CancellationTokenSource _serviceRunningToken = new CancellationTokenSource();
    private TcpListener _listener;
    private Task? _cleanupTask;
    private Task? _listenTask;
    private int _socketIdCounter = 1;

    private List<GrowattSocketReference> _socketRefs = new();
    public IReadOnlyList<GrowattSocketHandler> Sockets => _socketRefs.Select(x => x.Handler).ToList().AsReadOnly();

    public GrowattServerListener(
        ILogger<GrowattServerListener> logger,
        IOptions<GrowattServerOptions> growattServerOptions,
        IServiceProvider serviceProvider,
        IGrowattMetrics metrics)
    {
        _logger = logger;
        _growattServerOptions = growattServerOptions.Value;
        _serviceProvider = serviceProvider;
        _metrics = metrics;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {hostedService}", GetType().Name);
        _listener = new TcpListener(System.Net.IPAddress.Parse(_growattServerOptions.HostingAddress), _growattServerOptions.Port);
        _listener.Start();
        _logger.LogInformation("Listening on {endpoint}", _listener.LocalEndpoint);

        _listenTask = ListenAsync();
        _cleanupTask = StartCleanupSocketsAsync();

        _logger.LogInformation("Started {hostedService}", GetType().Name);
        return Task.CompletedTask;
    }

    private async Task TryCleanupSocketsAsync()
    {
        _logger.LogInformation("Cleaning up sockets...");
        try
        {
            foreach (var disconnectedSocket in _socketRefs.Where(x => !x.Socket.Connected).ToList())
            {
                _logger.LogInformation("Cleaning up socket {socketId} due to disconnect", disconnectedSocket.Id);
                _socketRefs.Remove(disconnectedSocket);
                disconnectedSocket.HandlerCancellationToken.Cancel();
                disconnectedSocket.Socket.Dispose();
                disconnectedSocket.HandlerRunTask.Dispose();
            }

            foreach (var stoppedHandlerSocket in _socketRefs.Where(x => x.HandlerRunTask.IsCompleted).ToList())
            {
                _logger.LogInformation(stoppedHandlerSocket.HandlerRunTask.Exception, "Cleaning up socket {socketId} due to handler being stopped, disconnecting if needed", stoppedHandlerSocket.Id);
                _socketRefs.Remove(stoppedHandlerSocket);
                if (stoppedHandlerSocket.Socket.Connected)
                {
                    await stoppedHandlerSocket.Socket.DisconnectAsync(false);
                }

                stoppedHandlerSocket.HandlerCancellationToken.Cancel();
                stoppedHandlerSocket.Socket.Dispose();
                stoppedHandlerSocket.HandlerRunTask.Dispose();

            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to clean up sockets");
        }
        _metrics.ActiveConnections(_socketRefs.Count);
    }

    private async Task ListenAsync()
    {
        while (!_serviceRunningToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Waiting for socket");
                var socket = await _listener.AcceptSocketAsync();
                var socketId = _socketIdCounter++;
                _logger.LogInformation("Got connection from {endpoint} (SocketId {socketId})", socket.RemoteEndPoint, socketId);
                _metrics.NewConnection();

                var handler = ActivatorUtilities.CreateInstance<GrowattSocketHandler>(_serviceProvider, (IGrowattSocket)(new GrowattSocket(socket, socketId)));
                var socketRef = new GrowattSocketReference
                {
                    Handler = handler,
                    Socket = socket,
                    Id = socketId,
                    HandlerCancellationToken = new CancellationTokenSource(),
                };
                socketRef.HandlerRunTask = handler.RunAsync(socketRef.HandlerCancellationToken.Token);
                _socketRefs.Add(socketRef);
                _metrics.ActiveConnections(_socketRefs.Count);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to setup socket");
            }
        }
    }

    private async Task StartCleanupSocketsAsync()
    {
        while(!_serviceRunningToken.IsCancellationRequested)
        {
            try
            {
                await TryCleanupSocketsAsync();
            }catch (Exception e)
            {
                _logger.LogError(e, "Socket cleanup failed");
            }
            await Task.Delay(TimeSpan.FromSeconds(15));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {hostedService}", GetType().Name);
        _serviceRunningToken.Cancel();
        _listener.Stop();
        _logger.LogInformation("Stopped {hostedService}", GetType().Name);
        return Task.CompletedTask;
    }
}
