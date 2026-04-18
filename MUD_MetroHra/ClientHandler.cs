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

            await writer.WriteLineAsync("Vitej v MUD Metro Hra");
            await writer.WriteLineAsync("Zadej 'login' nebo 'register':");

            string? mode = await reader.ReadLineAsync();
            if (mode == null)
                return;

            mode = mode.Trim().ToLower();

            if (mode != "login" && mode != "register")
            {
                await writer.WriteLineAsync("Neplatna volba.");
                return;
            }

            await writer.WriteLineAsync("Zadej uzivatelske jmeno:");
            string? username = await reader.ReadLineAsync();

            await writer.WriteLineAsync("Zadej heslo:");
            string? password = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await writer.WriteLineAsync("Neplatne udaje.");
                return;
            }

            playerName = username.Trim();

            if (mode == "register")
            {
                if (!_accountService.Register(playerName, password))
                {
                    await writer.WriteLineAsync("Uzivatel uz existuje.");
                    return;
                }

                LoggerService.Info($"Registrace hrace {playerName}");
                await writer.WriteLineAsync("Ucet vytvoren.");
            }
            else
            {
                if (!_accountService.Login(playerName, password))
                {
                    await writer.WriteLineAsync("Neplatne prihlasovaci udaje.");
                    return;
                }

                LoggerService.Info($"Prihlaseni hrace {playerName}");
                await writer.WriteLineAsync("Prihlaseni uspesne.");
            }

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
            await writer.WriteLineAsync($"Ahoj {player.Name}");
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
                catch
                {
                }
            }

            _client.Close();
            LoggerService.Info($"{playerName} odpojen");
        }
    }
}