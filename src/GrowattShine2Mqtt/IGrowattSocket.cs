namespace GrowattShine2Mqtt;

public interface IGrowattSocket
{
    int Available { get; }
    int SocketId { get; }
    bool Connected { get; }
    Task SendAsync(ArraySegment<byte> buffer);
    Task<int> ReceiveAsync(ArraySegment<byte> buffer);
}
