namespace MUD_MetroHra;


using System.Net;
using System.Net.Sockets;

public class GameServer
{
    private TcpListener _listener;

    public GameServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine("[INFO] Server běží");

        while (true)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            Console.WriteLine("[INFO] Klient připojen");

            ClientHandler handler = new ClientHandler(client);
            _ = Task.Run(() => handler.HandleAsync());
        }
    }
}