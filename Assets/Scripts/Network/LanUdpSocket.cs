using System.Net;
using System.Net.Sockets;
using UnityEngine;

public sealed class LanUdpSocket
{
    private Socket _socket;
    private EndPoint _anyEndpoint;

    public Socket Socket => _socket;

    public void Open(int port)
    {
        if (_socket != null)
            return;

        _socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp
        );

        // 🔥 КЛЮЧЕВОЕ МЕСТО
        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        // Не блокируем поток Unity
        _socket.Blocking = false;

        // Разрешаем broadcast
        _socket.EnableBroadcast = true;

        _anyEndpoint = new IPEndPoint(IPAddress.Any, port);

        _socket.Bind(_anyEndpoint);

        Debug.Log($"[LAN][UDP] Bound with REUSEADDR on port {port}");
    }

    public void Close()
    {
        if (_socket == null)
            return;

        try
        {
            _socket.Close();
        }
        catch { }

        _socket = null;
        Debug.Log("[LAN][UDP] Closed");
    }

    public bool TryReceive(out byte[] data, out IPEndPoint sender)
    {
        data = null;
        sender = null;

        if (_socket == null || !_socket.Poll(0, SelectMode.SelectRead))
            return false;

        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        byte[] buffer = new byte[1024];

        int size = _socket.ReceiveFrom(buffer, ref remote);
        if (size <= 0)
            return false;

        data = buffer;
        sender = (IPEndPoint)remote;
        return true;
    }

    public void Send(byte[] data, IPEndPoint target)
    {
        if (_socket == null)
            return;

        _socket.SendTo(data, target);
    }
}