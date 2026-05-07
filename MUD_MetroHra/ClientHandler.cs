namespace MUD_MetroHra;

using System.Net.Sockets;
using System.Text;

public class ClientHandler
{
    private readonly TcpClient _client;
    private readonly GameWorld _world;
    private readonly GameServer _server;
    private readonly AccountService _accountService;
    private readonly PersistenceService _persistenceService;

    public ClientHandler(
        TcpClient client,
        GameWorld world,
        GameServer server,
        AccountService accountService,
        PersistenceService persistenceService)
    {
        _client = client;
        _world = world;
        _server = server;
        _accountService = accountService;
        _persistenceService = persistenceService;
    }

    public async Task HandleAsync()
    {
        string playerName = "Neznamy";

        try
        {
            using var stream = _client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            string? playerNameResult = await HandleLoginAsync(reader, writer);
            if (playerNameResult == null)
                return;

            playerName = playerNameResult;

            var player = _persistenceService.LoadPlayer(playerName) ?? new Player
            {
                Name = playerName,
                CurrentRoomId = _world.GetStartRoom().Id
            };

            var session = new PlayerSession
            {
                Player = player,
                Writer = writer
            };

            _server.Sessions[player.Name] = session;

            LoggerService.Info($"{player.Name} se pripojil");
            await writer.WriteLineAsync($"Ahoj {player.Name}!");
            await writer.WriteLineAsync("Napis 'pomoc' pro seznam prikazu.");
            await writer.WriteLineAsync("");
            await writer.WriteLineAsync(CommandProcessor.Look(player, _world, _server));

            await _server.BroadcastToRoomAsync(player.CurrentRoomId, $"{player.Name} vstoupil do mistnosti.", player.Name);

            while (true)
            {
                string? input = await reader.ReadLineAsync();
                if (input == null)
                    break;

                input = input.Trim();
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                LoggerService.Command(player.Name, input);

                string oldRoomId = player.CurrentRoomId;
                string response = CommandProcessor.Process(input, player, _world, _server);

                await writer.WriteLineAsync(response);

                if (oldRoomId != player.CurrentRoomId)
                {
                    await _server.BroadcastToRoomAsync(oldRoomId, $"{player.Name} odesel z mistnosti.", player.Name);
                    await _server.BroadcastToRoomAsync(player.CurrentRoomId, $"{player.Name} prisel do mistnosti.", player.Name);
                }

                _persistenceService.SavePlayer(player);

                if (input.Equals("konec", StringComparison.OrdinalIgnoreCase))
                    break;
            }
        }
        catch (Exception ex)
        {
            LoggerService.Error(ex.ToString());
        }
        finally
        {
            if (_server.Sessions.TryRemove(playerName, out var session))
            {
                try
                {
                    _persistenceService.SavePlayer(session.Player);
                    await _server.BroadcastToRoomAsync(session.Player.CurrentRoomId, $"{session.Player.Name} opustil mistnost.", session.Player.Name);
                }
                catch { }
            }

            _client.Close();
            LoggerService.Info($"{playerName} odpojen");
        }
    }

    private async Task<string?> HandleLoginAsync(StreamReader reader, StreamWriter writer)
    {
        await writer.WriteLineAsync("Vitej v MUD Metro Hra!");
        await writer.WriteLineAsync("==============================");

        // Max 5 pokusu
        for (int attempt = 0; attempt < 5; attempt++)
        {
            await writer.WriteLineAsync("Zadej 'login' nebo 'register':");

            string? mode = await reader.ReadLineAsync();
            if (mode == null) return null;
            mode = mode.Trim().ToLower();

            if (mode != "login" && mode != "register")
            {
                await writer.WriteLineAsync("Neplatna volba. Zadej 'login' nebo 'register'.");
                continue;
            }

            await writer.WriteLineAsync("Uzivatelske jmeno:");
            string? username = await reader.ReadLineAsync();
            if (username == null) return null;
            username = username.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                await writer.WriteLineAsync("Jmeno nemuze byt prazdne.");
                continue;
            }

            await writer.WriteLineAsync("Heslo:");
            string? password = await reader.ReadLineAsync();
            if (password == null) return null;

            if (string.IsNullOrWhiteSpace(password))
            {
                await writer.WriteLineAsync("Heslo nemuze byt prazdne.");
                continue;
            }

            if (mode == "register")
            {
                if (!_accountService.Register(username, password))
                {
                    await writer.WriteLineAsync("Uzivatel uz existuje. Zkus jine jmeno nebo pouzij 'login'.");
                    continue;
                }

                LoggerService.Info($"Registrace hrace {username}");
                await writer.WriteLineAsync("Ucet uspesne vytvoren!");
                return username;
            }
            else
            {
                if (!_accountService.Login(username, password))
                {
                    await writer.WriteLineAsync("Spatne jmeno nebo heslo. Zkus to znovu.");
                    continue;
                }

                LoggerService.Info($"Prihlaseni hrace {username}");
                await writer.WriteLineAsync("Prihlaseni uspesne!");
                return username;
            }
        }

        await writer.WriteLineAsync("Prilis mnoho neuspesnych pokusu. Odpojuji.");
        return null;
    }
}