using System.Collections.ObjectModel;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using NodaTime;

namespace GrowattShine2Mqtt;


public class GrowattServerOptions
{
    public int Port { get; set; } = 5279;
    public string HostingAddress { get; set; } = "0.0.0.0";
}

public class GrowattServerListener(
    ILogger<GrowattServerListener> logger,
    IOptions<GrowattServerOptions> growattServerOptions,
    IServiceProvider serviceProvider,
    GrowattMetrics? metrics) : IHostedService
{
    private readonly ILogger<GrowattServerListener> _logger = logger;
    private readonly GrowattServerOptions _growattServerOptions = growattServerOptions.Value;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly GrowattMetrics? _metrics = metrics;
    private readonly CancellationTokenSource _serviceRunningToken = new();
    private TcpListener _listener = null!;
    private Task? _listenTask;
    private int _socketIdCounter = 1;

    private readonly Dictionary<int, GrowattSocketHandler> _socketRefs = [];

    public ReadOnlyDictionary<int, GrowattSocketHandler> Sockets => new(_socketRefs);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {hostedService}", GetType().Name);
        _listener = new TcpListener(System.Net.IPAddress.Parse(_growattServerOptions.HostingAddress), _growattServerOptions.Port);
        _listener.Start();
        _logger.LogInformation("Listening on {endpoint}", _listener.LocalEndpoint);

        _listenTask = ListenAsync();

        _logger.LogInformation("Started {hostedService}", GetType().Name);
        return Task.CompletedTask;
    }

    private async Task ListenAsync()
    {
        while (!_serviceRunningToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Waiting for new socket");
                var socket = await _listener.AcceptSocketAsync();
                var socketId = _socketIdCounter++;
                _logger.LogInformation("Got connection from {endpoint} (SocketId {socketId})", socket.RemoteEndPoint, socketId);
                _metrics?.NewConnection();
                var _ = HandleSocketAsync(socketId, socket);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to setup socket");
            }
        }
    }

    private async Task HandleSocketAsync(int socketId, Socket socket)
    {
        _metrics?.ActiveConnections(_socketRefs.Count);
        try
        {
            var socketInfo = new GrowattSocket(socket, socketId);
            var handler = new GrowattSocketHandler(
                _serviceProvider.GetRequiredService<ILogger<GrowattSocketHandler>>(),
                _serviceProvider.GetRequiredService<IGrowattToMqttHandler>(),
                _serviceProvider.GetRequiredService<IGrowattTelegramParser>(),
                _serviceProvider.GetRequiredService<GrowattMetrics>(),
                _serviceProvider.GetRequiredService<IClock>(),
                _serviceProvider.GetRequiredService<IDateTimeZoneProvider>(),
                socketInfo);
            _socketRefs.Add(socketId, handler);
            _metrics?.ActiveConnections(_socketRefs.Count);
            await handler.RunAsync(_serviceRunningToken.Token);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Socket {socketId} failed", socketId);
            return;
        }
        finally
        {
            socket.Close();
            socket.Dispose();
            _socketRefs.Remove(socketId);
            _metrics?.ActiveConnections(_socketRefs.Count);
        }
        _logger.LogInformation("Socket {socketId} disconnected/stopped", socketId);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {hostedService}", GetType().Name);
        _serviceRunningToken.Cancel();
        _listener.Stop();
        _listenTask?.Dispose();
        _logger.LogInformation("Stopped {hostedService}", GetType().Name);
        return Task.CompletedTask;
    }
}
