using Prometheus;

namespace GrowattShine2Mqtt;

public class GrowattMetrics
{
    private readonly MetricFactory _metricsFactory;

    private readonly Gauge _connections;
    private readonly Counter _connectedTotal;
    private readonly Counter _messagesReceived;
    private readonly Counter _bytesReceived;
    private readonly Counter _messagesSent;
    private readonly Counter _byesSent;

    public GrowattMetrics(MetricFactory metricsFactory)
    {
        _metricsFactory = metricsFactory;

        _messagesReceived = _metricsFactory.CreateCounter("growatt_messages_received_total", "Amount of Growatt packages received", "messageType");
        _messagesSent = _metricsFactory.CreateCounter("growatt_messages_sent_total", "Amount of Growatt packages sent", "messageType");
        _bytesReceived = _metricsFactory.CreateCounter("growatt_received_bytes_total", "Amount of bytes received from Growatt");
        _byesSent = _metricsFactory.CreateCounter("growatt_sent_bytes_total", "Amount of bytes sent to Growatt");
        _connections = _metricsFactory.CreateGauge("growatt_active_connections", "Amount of active Growatt connection");
        _connectedTotal = _metricsFactory.CreateCounter("growatt_connected_total", "Amount of Growatt connections created");
    }

    public void MessageReceived(string messageType, int size)
    {
        _bytesReceived.Inc(size);
        _messagesReceived.WithLabels(messageType).Inc();
    }


    public void MessageSent(string messageType, int size)
    {
        _byesSent.Inc(size);
        _messagesSent.WithLabels(messageType).Inc();
    }

    public void ActiveConnections(int activeConnections)
    {
        _connections.Set(activeConnections);

    }

    public void NewConnection()
    {
        _connectedTotal.Inc();
    }
}

