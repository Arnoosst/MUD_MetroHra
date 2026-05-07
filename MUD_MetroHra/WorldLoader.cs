namespace MUD_MetroHra;

using System.Text.Json;

public class WorldLoader
{
    public static GameWorld Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Soubor sveta nebyl nalezen: {path}");

        var json = File.ReadAllText(path);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<WorldData>(json, options);

        if (data?.Rooms == null || data.Rooms.Count == 0)
            throw new Exception("World JSON neobsahuje zadne mistnosti.");

        var world = new GameWorld();

        foreach (var room in data.Rooms)
        {
            world.Rooms[room.Id] = room;
        }

        if (data.Quests != null)
        {
            foreach (var quest in data.Quests)
            {
                world.Quests[quest.Id] = quest;
            }
        }

        return world;
    }
}

public class WorldData
{
    public List<Room> Rooms { get; set; } = new();
    public List<QuestDefinition> Quests { get; set; } = new();
}
