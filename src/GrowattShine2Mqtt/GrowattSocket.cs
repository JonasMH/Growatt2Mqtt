using System.Net.Sockets;

namespace GrowattShine2Mqtt;

public class GrowattSocket(Socket socket, int socketId) : IGrowattSocket {
    private readonly Socket _socket = socket;
    private readonly int _socketId = socketId;

    public int Available => _socket.Available;
    public int SocketId => _socketId;
    public bool Connected => _socket.Connected;

    public async Task SendAsync(ArraySegment<byte> buffer)
    {
        await _socket.SendAsync(buffer, SocketFlags.None);
    }

    public async Task<int> ReceiveAsync(ArraySegment<byte> buffer)
    {
        return await _socket.ReceiveAsync(buffer, SocketFlags.None);
    }
}
