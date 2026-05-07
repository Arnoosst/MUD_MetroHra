namespace MUD_MetroHra;

public class GameWorld
{
    public Dictionary<string, Room> Rooms { get; set; } = new();
    public Dictionary<string, QuestDefinition> Quests { get; set; } = new();

    public Room? GetRoom(string id)
    {
        return Rooms.TryGetValue(id, out var room) ? room : null;
    }

    public Room GetStartRoom()
    {
        return GetRoom("zakladna") ?? Rooms.Values.First();
    }

    public QuestDefinition? GetQuest(string id)
    {
        return Quests.TryGetValue(id, out var q) ? q : null;
    }
}

public class QuestDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}
