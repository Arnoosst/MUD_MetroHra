namespace MUD_MetroHra;

public class GameWorld
{
    public Dictionary<string, Room> Rooms { get; set; } = new();

    public Room? GetRoom(string id)
    {
        return Rooms.TryGetValue(id, out var room) ? room : null;
    }

    public Room GetStartRoom()
    {
        return GetRoom("zakladna") ?? Rooms.Values.First();
    }
}