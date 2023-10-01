using System.Diagnostics.Metrics;

namespace GrowattShine2Mqtt;

public class GrowattMetrics
{
    private readonly Meter _scope;

    private long _connections;
    private readonly Counter<long> _connectedTotal;
    private readonly Counter<long> _messagesReceived;
    private readonly Counter<long> _bytesReceived;
    private readonly Counter<long> _messagesSent;
    private readonly Counter<long> _bytesSent;

    public GrowattMetrics(IMeterFactory meterFactory)
    {
        _scope = meterFactory.Create("GrowattShine2Mqtt");

        _messagesReceived = _scope.CreateCounter<long>("growatt_messages_received_total", description: "Amount of Growatt packages received");
        _messagesSent = _scope.CreateCounter<long>("growatt_messages_sent_total", description: "Amount of Growatt packages sent");
        _bytesReceived = _scope.CreateCounter<long>("growatt_received_bytes_total", description: "Amount of bytes received from Growatt");
        _bytesSent = _scope.CreateCounter<long>("growatt_sent_bytes_total", description: "Amount of bytes sent to Growatt");
        _scope.CreateObservableGauge("growatt_active_connections", () => _connections, description: "Amount of active Growatt connection");
        _connectedTotal = _scope.CreateCounter<long>("growatt_connected_total", description: "Amount of Growatt connections created");
    }

    public void MessageReceived(string messageType, int size)
    {
        _bytesReceived.Add(size);
        _messagesReceived.Add(1, new KeyValuePair<string, object?>("messageType", messageType));
    }


    public void MessageSent(string messageType, int size)
    {
        _bytesSent.Add(size);
        _messagesSent.Add(1, new KeyValuePair<string, object?>("messageType3", messageType));
    }

    public void ActiveConnections(long activeConnections)
    {
        _connections = activeConnections;
    }

    public void NewConnection()
    {
        _connectedTotal.Add(1);
    }
}

