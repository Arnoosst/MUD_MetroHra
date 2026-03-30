namespace MUD_MetroHra;

class Program
{
    static async Task Main(string[] args)
    {
        GameServer server = new GameServer(5000);
        await server.StartAsync();
    }
}