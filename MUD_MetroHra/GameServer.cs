namespace MUD_MetroHra;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

public class GameServer
{
    private readonly TcpListener _listener;
    private readonly GameWorld _world;
    private readonly AccountService _accountService;
    private readonly PersistenceService _persistenceService;

    public ConcurrentDictionary<string, PlayerSession> Sessions { get; } = new();

    public GameServer(int port, GameWorld world, AccountService accountService, PersistenceService persistenceService)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _world = world;
        _accountService = accountService;
        _persistenceService = persistenceService;
    }

    public async Task StartAsync()
    {
        _listener.Start();
        LoggerService.Info("Server bezi");

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            LoggerService.Info("Klient pripojen");

            var handler = new ClientHandler(client, _world, this, _accountService, _persistenceService);
            _ = Task.Run(handler.HandleAsync);
        }
    }

    public List<string> GetOtherPlayersInRoom(string roomId, string exceptPlayerName)
    {
        return Sessions.Values
            .Where(s => s.Player.CurrentRoomId == roomId && !s.Player.Name.Equals(exceptPlayerName, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Player.Name)
            .OrderBy(x => x)
            .ToList();
    }

    public async Task BroadcastToRoomAsync(string roomId, string message, string? exceptPlayerName = null)
    {
        var targets = Sessions.Values
            .Where(s => s.Player.CurrentRoomId == roomId &&
                        (exceptPlayerName == null || !s.Player.Name.Equals(exceptPlayerName, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var session in targets)
        {
            try { await session.Writer.WriteLineAsync(message); }
            catch { }
        }
    }

    public async Task BroadcastToAllAsync(string message, string? exceptPlayerName = null)
    {
        var targets = Sessions.Values
            .Where(s => exceptPlayerName == null || !s.Player.Name.Equals(exceptPlayerName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var session in targets)
        {
            try { await session.Writer.WriteLineAsync(message); }
            catch { }
        }
    }

    public List<Player> GetLeaderboard()
    {
        return _persistenceService.GetAllPlayers()
            .Where(p => p.HasFinishedGame && p.FinishedAt.HasValue)
            .OrderBy(p => p.FinishedAt)
            .ToList();
    }
}
