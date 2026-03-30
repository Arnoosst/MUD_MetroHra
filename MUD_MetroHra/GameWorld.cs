namespace MUD_MetroHra;

public class GameWorld
{
    public Dictionary<string, Room> Rooms { get; set; } = new();

    public Room GetRoom(string id)
    {
        return Rooms.ContainsKey(id) ? Rooms[id] : null;
    }
}