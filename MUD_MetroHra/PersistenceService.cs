namespace MUD_MetroHra;

using System.Text.Json;

public class PersistenceService
{
    private readonly string _playersFolder = "players";

    public PersistenceService()
    {
        Directory.CreateDirectory(_playersFolder);
    }

    public void SavePlayer(Player player)
    {
        var path = GetPlayerPath(player.Name);
        var json = JsonSerializer.Serialize(player, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }

    public Player? LoadPlayer(string playerName)
    {
        var path = GetPlayerPath(playerName);

        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Player>(json);
    }

    public List<Player> GetAllPlayers()
    {
        var players = new List<Player>();

        if (!Directory.Exists(_playersFolder))
            return players;

        foreach (var file in Directory.GetFiles(_playersFolder, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var player = JsonSerializer.Deserialize<Player>(json);
                if (player != null)
                    players.Add(player);
            }
            catch { }
        }

        return players;
    }

    private string GetPlayerPath(string playerName)
    {
        return Path.Combine(_playersFolder, $"{playerName}.json");
    }
}
